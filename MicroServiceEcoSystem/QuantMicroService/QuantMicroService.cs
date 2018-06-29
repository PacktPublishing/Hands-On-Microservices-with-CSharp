namespace QuantMicroService
{
    using System;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using QLNet;
    using Topshelf;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A quant micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{QuantMicroService.QuantMicroService, CommonMessages.CreditDefaultSwapRequestMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class QuantMicroService : BaseMicroService<QuantMicroService, CreditDefaultSwapRequestMessage>
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the QuantMicroService.QuantMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuantMicroService()
        {
            Name = "Quant Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="host"> The host. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool OnStart([CanBeNull] HostControl host)
        {
            Start(host);
            Subscribe();

            CreditDefaultSwapRequestMessage msgT = new CreditDefaultSwapRequestMessage();
            msgT.fixedRate = 0.001;
            msgT.notional = 10000.0;
            msgT.recoveryRate = 0.4;
            PublishRequestMessage(msgT, "CDSRequest");

            BondsRequestMessage r = new BondsRequestMessage();
            PublishBondRequestMessage(r, "BondRequest");
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
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
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the cds response described by cds. </summary>
        ///
        /// <param name="cds">  The cds. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool ProcessCDSResponse(CreditDefaultSwapResponseMessage cds)
        {
            Console.WriteLine("calculated spread: " + cds.fairRate);
            Console.WriteLine("calculated NPV: " + cds.fairNPV);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the cds message described by cds. </summary>
        ///
        /// <param name="cds">  The cds. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool ProcessCDSMessage(CreditDefaultSwapRequestMessage cds)
        {
            CDS c = new CDS();
            bool result = c.CalcCDS(ref cds, cds.fixedRate, cds.notional, cds.recoveryRate);
            // the message is populated with the fair rate and NPV now.
            CreditDefaultSwapResponseMessage cd = new CreditDefaultSwapResponseMessage();
            cd.fixedRate = cds.fixedRate;
            cd.fairNPV = cds.fairNPV;
            cd.fairRate = cds.fairRate;
            cd.notional = cds.notional;
            cd.recoveryRate = cds.recoveryRate;
            PublishResponseMessage(cd, "CDSResponse");
            return result;

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the bonds message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool ProcessBondsMessage(BondsRequestMessage msg)
        {
            Bonds b = new Bonds();
            return b.testYield(this);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the bonds response described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool ProcessBondsResponse(BondsResponseMessage msg)
        {
            Console.WriteLine(msg.message + ":\n"
                              + "    issue:     " + msg.issue + "\n"
                              + "    maturity:  " + msg.maturity + "\n"
                              + "    coupon:    " + msg.coupon + "\n"
                              + "    frequency: " + msg.frequency + "\n\n"
                              + "    yield:  " + msg.yield + " "
                              + msg.compounding + "\n"
                              + "    price:  " + msg.price + "\n"
                              + "    yield': " + msg.calcYield + "\n"
                              + "    price': " + msg.price2);
            return true;
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
            IQueue queue = Bus.Advanced.QueueDeclare("Financial");
            Bus.Advanced.Bind(exchange, queue, "");

            Bus.Subscribe<CreditDefaultSwapRequestMessage>("CDSRequest", msg => { ProcessCDSMessage(msg); }, 
                config => config.WithTopic("CDSRequest"));
            Bus.Subscribe<CreditDefaultSwapResponseMessage>("CDSResponse", msgR => { ProcessCDSResponse(msgR); }, 
                config => config.WithTopic("CDSResponse"));

            Bus.Subscribe<BondsRequestMessage>("BondRequest", msg => { ProcessBondsMessage(msg); },
                config => config.WithTopic("BondRequest"));
            Bus.Subscribe<BondsResponseMessage>("BondResponse", msgR => { ProcessBondsResponse(msgR); },
                config => config.WithTopic("BondResponse"));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish request message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="topic">        The topic. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishRequestMessage(CreditDefaultSwapRequestMessage msg, string topic, string routingID = "")
        {
            Bus.Publish(msg, topic);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish response message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="topic">        The topic. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishResponseMessage(CreditDefaultSwapResponseMessage msg, string topic, string routingID = "")
        {
            Bus.Publish(msg, topic);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish bond request message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="topic">        The topic. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishBondRequestMessage(BondsRequestMessage msg, string topic, string routingID = "")
        {
            Bus.Publish(msg, topic);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish bond response message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="topic">        The topic. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishBondResponseMessage(BondsResponseMessage msg, string topic, string routingID = "")
        {
            Bus.Publish(msg, topic);
        }
    }
}
