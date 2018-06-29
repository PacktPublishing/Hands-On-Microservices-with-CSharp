using System;
using EasyNetQ;

namespace CommonMessages
{
    /// <summary>   Values that represent Miles message types. </summary>
    public enum MLMessageType
    {
        /// <summary>   An enum constant representing the create= 1 option. </summary>
        Create=1,

        /// <summary>   An enum constant representing the add layer= 2 option. </summary>
        AddLayer=2,

        /// <summary>   An enum constant representing the forward= 3 option. </summary>
        Forward=3,

        /// <summary>   An enum constant representing the train= 4 option. </summary>
        Train=4,

        /// <summary>   An enum constant representing the get result= 5 option. </summary>
        GetResult=5,

        /// <summary>   An enum constant representing the reply= 6 option. </summary>
        Reply=6
    }

    /// <summary>   Values that represent layer types. </summary>
    public enum LayerType
    {
        /// <summary>   An enum constant representing the none= 0 option. </summary>
        None=0,

        /// <summary>   An enum constant representing the fully Connection layer= 1 option. </summary>
        FullyConnLayer=1,

        /// <summary>   An enum constant representing the relu layer= 2 option. </summary>
        ReluLayer=2,

        /// <summary>   An enum constant representing the input layer= 3 option. </summary>
        InputLayer=3,

        /// <summary>   An enum constant representing the softmax layer= 4 option. </summary>
        SoftmaxLayer=4
    }


    /// <summary>   (Serializable) the miles message. </summary>
    [Serializable]
    [Queue("MachineLearning", ExchangeName = "EvolvedAI")]
    public class MLMessage
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the identifier. </summary>
        ///
        /// <value> The identifier. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public long ID { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the type of the message. </summary>
        ///
        /// <value> The type of the message. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int MessageType { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the type of the layer. </summary>
        ///
        /// <value> The type of the layer. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int LayerType { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the parameter 1. </summary>
        ///
        /// <value> The parameter 1. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double param1 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the parameter 2. </summary>
        ///
        /// <value> The parameter 2. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double param2 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the parameter 3. </summary>
        ///
        /// <value> The parameter 3. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double param3 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the parameter 4. </summary>
        ///
        /// <value> The parameter 4. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double param4 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the reply value 1. </summary>
        ///
        /// <value> The reply value 1. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double replyVal1 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the reply value 2. </summary>
        ///
        /// <value> The reply value 2. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public double replyVal2 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the reply message 1. </summary>
        ///
        /// <value> The reply message 1. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string replyMsg1 { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the reply message 2. </summary>
        ///
        /// <value> The reply message 2. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string replyMsg2 { get; set; }
    }
}
