using System;
using MicroServiceEcoSystem;

namespace MachineLearningMicroService
{
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CommonMessages;
    using ConvNetSharp.Core;
    using ConvNetSharp.Core.Layers.Double;
    using ConvNetSharp.Core.Training.Double;
    using ConvNetSharp.Volume;
    using ConvNetSharp.Volume.Double;
    using EasyNetQ;
    using EasyNetQ.Topology;
    using JetBrains.Annotations;
    using RabbitMQ.Client;
    using ReflectSoftware.Insight;
    using Topshelf;
    using Topshelf.Leader;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   The miles micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{MachineLearningMicroService.MLMicroService, CommonMessages.MLMessage}"/>
    /// <seealso cref="T:Topshelf.Leader.ILeaseManager"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class MLMicroService : BaseMicroService<MLMicroService, MLMessage>, ILeaseManager
    {
        /// <summary>   The net. </summary>
        private static Net<double> _net = null;
        /// <summary>   The probability. </summary>
        private static Volume<double> _probability;
        /// <summary>   The train result. </summary>
        private static Volume<double> _trainResult;
        /// <summary>   The container. </summary>
        private Autofac.IContainer _container;
        /// <summary>   Identifier for the owning node. </summary>
        private string _owningNodeId;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MachineLearningMicroService.MLMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MLMicroService()
        {
            Name = "Machine Learning Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MachineLearningMicroService.MLMicroService class.
        /// </summary>
        ///
        /// <param name="container">    The container. </param>
        /// <param name="owningNode">   The owning node. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MLMicroService(Autofac.IContainer container, string owningNode)
        {
            _container = container;
            _owningNodeId = owningNode;
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
            Host = host;
            base.Start(host);

            CreateNetwork();

            Subscribe("MachineLearning", "", (msg) => { ProcessMLMessage(msg, null); });
            Console.WriteLine(string.Intern("Machine Learning MicroService Started."));

            // input layer declares size of input. here: 2-D data
            // ConvNetJS works on 3-Dimensional volumes (width, height, depth), but if you're not dealing with images
            // then the first two dimensions (width, height) will always be kept at size 1
            PublishMessage("EvolvedAI", "", MLMessageType.AddLayer, LayerType.InputLayer, 1,1,2,"test1","test2", string.Empty);

            // declare 20 neurons
            PublishMessage("EvolvedAI", "", MLMessageType.AddLayer, LayerType.FullyConnLayer, 20, 0, 0, string.Empty, string.Empty, string.Empty);

            // declare a ReLU (rectified linear unit non-linearity)
            PublishMessage("EvolvedAI", "", MLMessageType.AddLayer, LayerType.ReluLayer, 0, 0, 0, string.Empty, string.Empty, string.Empty);

            // declare a fully connected layer that will be used by the softmax layer
            PublishMessage("EvolvedAI", "", MLMessageType.AddLayer, LayerType.FullyConnLayer, 10, 0, 0, string.Empty, string.Empty, string.Empty);

            // declare the linear classifier on top of the previous hidden layer
            PublishMessage("EvolvedAI", "", MLMessageType.AddLayer, LayerType.SoftmaxLayer, 10, 0, 0, string.Empty, string.Empty, string.Empty);

            // forward a random data point through the network
            PublishMessage("EvolvedAI", "", MLMessageType.Forward, LayerType.None, 0.3, -0.5, 2, string.Empty, string.Empty, string.Empty);

            // prob is a Volume (tensor). Volumes have a property Weights that stores the raw data, and WeightGradients that stores gradients
            // get the result of all we did. 0.50101

            PublishMessage("EvolvedAI", "", MLMessageType.GetResult, LayerType.None, 0.3, -0.5, 2, string.Empty, string.Empty, string.Empty);

            return (true);
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
        /// <summary>   Continues this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool Continue()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Pauses this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool Pause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Resumes this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool Resume()
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
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
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
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            return true;
        }

        /// <summary>   Subscribes this object. </summary>
        public void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost");
            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", RabbitMQ.Client.ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare("MachineLearning");
            Bus.Advanced.Bind(exchange, queue, "");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the miles message. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        /// <param name="mri">  The mri. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessMLMessage([NotNull] MLMessage msg, [NotNull] MessageReceivedInfo mri)
        {
            Console.WriteLine("Received Machine Learning Message");

            RILogManager.Default?.SendInformation("Received Machine Learning Message");
            switch ((MLMessageType)msg.MessageType)
            {
                case MLMessageType.AddLayer:
                    RILogManager.Default?.SendInformation("Adding a layer command");
                    CreateLayer((LayerType)msg.LayerType, msg.param1, msg.param2, msg.param3);
                    PublishMessage("EvolvedAI", "", MLMessageType.Reply, LayerType.None, 0, 0,0,"Success", "", "");
                    break;
                case MLMessageType.Create:
                    RILogManager.Default?.SendInformation("Create command");
                    CreateNetwork();
                    PublishMessage("EvolvedAI", "", MLMessageType.Reply, LayerType.None, 0, 0, 0, "Success", "", "");
                    break;
                case MLMessageType.Forward:
                    RILogManager.Default?.SendInformation("Forward command");
                    ForwardPoint(msg.param1, msg.param2, msg.param3);
                    PublishMessage("EvolvedAI", "", MLMessageType.Reply, LayerType.None, 0, 0, 0, "Success", "", "");
                    break;
                case MLMessageType.GetResult:
                    RILogManager.Default?.SendInformation("Requesting Results");
                    PublishMessage("EvolvedAI", "", MLMessageType.Reply, LayerType.None, _probability, 0, 0,
                        "probability that x is class 0: " + _probability.Get(0), "", "");
                    break;
                case MLMessageType.Train:
                    RILogManager.Default?.SendInformation("Training the network");
                    TrainNetwork(msg.param1, msg.param2, msg.param3, msg.param4);
                    PublishMessage("EvolvedAI", "", MLMessageType.Reply, LayerType.None, 0, 0, 0, "Success", "", "");
                    break;
                case MLMessageType.Reply:
                    Console.WriteLine(msg.replyMsg1);
                    Console.WriteLine(msg.replyMsg2);
                    break;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Train network. </summary>
        ///
        /// <param name="val1"> The first value. </param>
        /// <param name="val2"> The second value. </param>
        /// <param name="val3"> The third value. </param>
        /// <param name="val4"> The fourth value. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal void TrainNetwork(double val1, double val2, double val3, double val4)
        {
            var trainer = new SgdTrainer(_net) { LearningRate = 0.3, L2Decay = -0.5 };
            trainer.Train(_trainResult, BuilderInstance.Volume?.From(new[] { 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }, 
                new Shape((int)val1, (int)val2, (int)val3, (int)val4))); // train the network, specifying that x is class zero
        }

        /// <summary>   Creates the network. </summary>
        internal void CreateNetwork()
        {
            _net = new Net<double>();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a layer. </summary>
        ///
        /// <param name="type"> The type. </param>
        /// <param name="val1"> The first value. </param>
        /// <param name="val2"> The second value. </param>
        /// <param name="val3"> The third value. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal void CreateLayer(LayerType type, double val1, double val2, double val3)
        {
            switch (type)
            {
                case LayerType.InputLayer:
                    _net?.AddLayer(new InputLayer((int)val1, (int)val2, (int)val3));
                    break;
                case LayerType.FullyConnLayer:
                    _net?.AddLayer(new FullyConnLayer((int)val1));
                    break;
                case LayerType.ReluLayer:
                    _net?.AddLayer(new ReluLayer());
                    break;
                case LayerType.SoftmaxLayer:
                    _net?.AddLayer(new SoftmaxLayer((int)val1));
                    break;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Forward point. </summary>
        ///
        /// <param name="val1"> The first value. </param>
        /// <param name="val2"> The second value. </param>
        /// <param name="val3"> The third value. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        internal void ForwardPoint(double val1, double val2, double val3)
        {
            _trainResult = BuilderInstance.Volume?.From(new[] { 0.3, -0.5 }, new Shape(2));
            _probability = _net.Forward(_trainResult);
        }

        /// <summary>   Gets the result. </summary>
        internal void GetResult()
        {
            PublishMessage("EvolvedAI", string.Empty, MLMessageType.Reply, LayerType.None, _probability, 0, 0,
                "probability that x is class 0: " + _probability.Get(0), string.Empty, string.Empty);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish message. </summary>
        ///
        /// <param name="exchange">     The exchange. </param>
        /// <param name="routingID">    Identifier for the routing. </param>
        /// <param name="msgType">      Type of the message. </param>
        /// <param name="lt">           The lt. </param>
        /// <param name="val1">         The first value. </param>
        /// <param name="val2">         The second value. </param>
        /// <param name="val3">         The third value. </param>
        /// <param name="msg1">         The first message. </param>
        /// <param name="msg2">         The second message. </param>
        /// <param name="msg3">         The third message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishMessage(string exchange, string routingID, MLMessageType msgType, LayerType lt, double val1, 
            double val2, double val3, string msg1, string msg2, string msg3)
        {
            MLMessage msg = new MLMessage
            {
                MessageType = (int)msgType,
                LayerType = (int)lt,
                replyMsg1 = msg1,
                replyMsg2 = msg2,
                param1 = val1,
                param2 = val2,
                param3 = val3
            };
            Bus.Publish(msg, "MachineLearning");
        }

        
    }
}
