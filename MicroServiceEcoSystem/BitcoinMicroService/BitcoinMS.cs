using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitcoinMicroService
{
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Timers;
    using Autofac;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.Logging;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using NBitcoin;
    using NodaTime;
    using QBitNinja.Client;
    using QBitNinja.Client.Models;
    using ReflectSoftware.Insight;
    using Topshelf;
    using Topshelf.Leader;
    using Timer = System.Timers.Timer;
    using Console = Colorful.Console;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A bitcoin milliseconds. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{BitcoinMicroService.BitcoinMS, CommonMessages.HealthStatusMessage}"/>
    /// <seealso cref="T:Topshelf.Leader.ILeaseManager"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class BitcoinMS : BaseMicroService<BitcoinMS, HealthStatusMessage>, ILeaseManager
    {
        /// <summary>   The container. </summary>
        private IContainer _container;
        /// <summary>   The timer. </summary>
        private Timer _timer = null;
        /// <summary>   Identifier for the owning node. </summary>
        private string _owningNodeId;


        /// <summary>   Initializes a new instance of the BitcoinMicroService.BitcoinMS class. </summary>
        public BitcoinMS()
        {
            Name = "Bitcoin Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes a new instance of the BitcoinMicroService.BitcoinMS class. </summary>
        ///
        /// <param name="c">            An IContainer to process. </param>
        /// <param name="owningNode">   The owning node. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BitcoinMS(IContainer c, string owningNode)
        {
            _container = c;
            _owningNodeId = owningNode;
            Name = "Bitcoin Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Assign leader. </summary>
        ///
        /// <param name="newLeaderId">  Identifier for the new leader. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void AssignLeader(string newLeaderId)
        {
            this._owningNodeId = newLeaderId;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Acquires the lease. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>
        /// The asynchronous result that yields true if it succeeds, false if it fails.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task<bool> AcquireLease(LeaseOptions options, CancellationToken token)
        {
            Console.WriteLine("Acquire Lease", Color.Yellow);
            return Task.FromResult(options.NodeId == _owningNodeId);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Renew lease. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>
        /// The asynchronous result that yields true if it succeeds, false if it fails.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task<bool> RenewLease(LeaseOptions options, CancellationToken token)
        {
            Console.WriteLine("Renew Lease", Color.Yellow);
            return Task.FromResult(options.NodeId == _owningNodeId);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Release. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task ReleaseLease(LeaseReleaseOptions options)
        {
            Console.WriteLine("Release Lease", Color.Yellow);
            _owningNodeId = string.Empty;
            return Task.FromResult(true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Starts the given stop token. </summary>
        ///
        /// <param name="stopToken">    The stop token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task Start(CancellationToken stopToken)
        {
            while (!stopToken.IsCancellationRequested)
            {
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="host"> The host. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStart([CanBeNull] HostControl host)
        {
            base.Start(host);
            Host = host;
            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _timer.Start();
            Subscribe();

            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Starting");
                Console.WriteLine(Name + " Microservice Starting", Color.Yellow);
            }

            BitcoinSpendMessage m = new BitcoinSpendMessage();
            m.amount = .50M;
            Bus.Publish(m, "Bitcoin.Spend");
            return (true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            _timer.Stop();
            
            base.Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            base.Continue();

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            base.Pause();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            base.Resume();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            base.Shutdown();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Raises the elapsed event. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void OnTick(object sender, ElapsedEventArgs e)
        {
            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Heartbeat");
                Console.WriteLine(Name + " Microservice Heartbeat", Color.Yellow);
            }
        }

        /// <summary>   Subscribes this object. </summary>
        public void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });

            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare("Bitcoin");
            Bus.Advanced.Bind(exchange, queue, Environment.MachineName);

            Bus.Subscribe<BitcoinSpendMessage>(Environment.MachineName, msg => { ProcessBitcoinSpendMessage(msg); },
                config => config?.WithTopic("Bitcoin.Spend"));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the bitcoin spend message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessBitcoinSpendMessage(BitcoinSpendMessage msg)
        {
            Console.WriteLine("Received Bitcoin Spend Message", Color.Aqua);
            RILogManager.Default?.SendInformation("Received Bitcoin Spend Message");
            SpendMoney();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Spend money. </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SpendMoney()
        {
            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Bitcoin SpendMoney");
                Console.WriteLine("Spending Money", Color.Aqua);

                #region IMPORT PRIVKEY

                var bitcoinPrivateKey = new BitcoinSecret("cSZjE4aJNPpBtU6xvJ6J4iBzDgTmzTjbq8w2kqnYvAprBCyTsG4x");
                var network = bitcoinPrivateKey.Network;

                #endregion

                var address = bitcoinPrivateKey.GetAddress();
                logger?.LogInformation(bitcoinPrivateKey.ToString());
                logger?.LogInformation(address?.ToString());


                var client = new QBitNinjaClient(network);
                var transactionId = uint256.Parse("e44587cf08b4f03b0e8b4ae7562217796ec47b8c91666681d71329b764add2e3");
                var transactionResponse = client.GetTransaction(transactionId)?.Result;

                logger?.LogInformation(transactionResponse?.TransactionId.ToString());
                logger?.LogInformation(transactionResponse?.Block.Confirmations.ToString());


                var receivedCoins = transactionResponse?.ReceivedCoins;
                OutPoint outPointToSpend = null;
                foreach (var coin in receivedCoins)
                {
                    if (coin.TxOut?.ScriptPubKey == bitcoinPrivateKey.ScriptPubKey)
                    {
                        outPointToSpend = coin.Outpoint;
                    }
                }

                if (outPointToSpend == null)
                    throw new Exception("TxOut doesn't contain our ScriptPubKey");
                logger?.LogInformation("We want to spend " + outPointToSpend.N + 1 + ". outpoint:");
                Console.WriteLine("We want to spend " + outPointToSpend.N + 1 + ". outpoint:", Color.Aqua);

                var transaction = new Transaction();
                transaction.Inputs?.Add(new TxIn()
                {
                    PrevOut = outPointToSpend
                });

                var hallOfTheMakersAddress = new BitcoinPubKeyAddress("mzp4No5cmCXjZUpf112B1XWsvWBfws5bbB");

                // How much you want to TO
                var hallOfTheMakersAmount = new Money((decimal) 0.5, MoneyUnit.BTC);
                var minerFee = new Money((decimal) 0.0001, MoneyUnit.BTC);
                // How much you want to spend FROM
                var txInAmount = (Money) receivedCoins[(int) outPointToSpend.N]?.Amount;
                Money changeBackAmount = txInAmount - hallOfTheMakersAmount - minerFee;

                TxOut hallOfTheMakersTxOut = new TxOut()
                {
                    Value = hallOfTheMakersAmount,
                    ScriptPubKey = hallOfTheMakersAddress.ScriptPubKey
                };

                TxOut changeBackTxOut = new TxOut()
                {
                    Value = changeBackAmount,
                    ScriptPubKey = bitcoinPrivateKey.ScriptPubKey
                };

                transaction.Outputs?.Add(hallOfTheMakersTxOut);
                transaction.Outputs?.Add(changeBackTxOut);

                var message = "Our first bitcoin transaction together!";
                var bytes = Encoding.UTF8.GetBytes(message);
                transaction.Outputs?.Add(new TxOut()
                {
                    Value = Money.Zero,
                    ScriptPubKey = TxNullDataTemplate.Instance?.GenerateScriptPubKey(bytes)
                });

                // It is also OK:
                transaction.Inputs[0].ScriptSig = bitcoinPrivateKey.ScriptPubKey;
                transaction.Sign(bitcoinPrivateKey, false);

                BroadcastResponse broadcastResponse = client.Broadcast(transaction)?.Result;

                BitcoinSpendReceipt r = new BitcoinSpendReceipt();
                if (!broadcastResponse.Success)
                {
                    logger?.LogError($"ErrorCode: {broadcastResponse.Error.ErrorCode}");
                    Console.WriteLine($"ErrorCode: {broadcastResponse.Error.ErrorCode}", Color.Red);
                    logger?.LogError("Error message: " + broadcastResponse.Error.Reason);
                    Console.WriteLine("Error message: " + broadcastResponse.Error.Reason, Color.Red);
                    r.success = false;
                }
                else
                {
                    logger?.LogInformation("Success! You can check out the hash of the transaction in any block explorer:");
                    Console.WriteLine("Success! You can check out the hash of the transaction in any block explorer:", Color.Green);
                    logger?.LogInformation(transaction.GetHash()?.ToString());
                    Console.WriteLine(transaction.GetHash()?.ToString(), Color.Green);
                    r.success = true;
                }

                r.time = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc();
                r.amount = txInAmount.ToDecimal(MoneyUnit.BTC);
                Bus.Publish(r, "Bitcoin");
            }
        }
    }
}
