using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem
{
    using EasyNetQ;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// (Serializable) attribute for exchange name. This class cannot be inherited.
    /// </summary>
    ///
    /// <seealso cref="T:System.Attribute"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ExchangeNameAttribute : Attribute
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.ExchangeNameAttribute class.
        /// </summary>
        ///
        /// <param name="exchangeName"> The name of the exchange. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ExchangeNameAttribute(string exchangeName)
        {
            ExchangeName = exchangeName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the name of the exchange. </summary>
        ///
        /// <value> The name of the exchange. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string ExchangeName { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable) attribute for queue name. </summary>
    ///
    /// <seealso cref="T:System.Attribute"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class QueueNameAttribute : Attribute
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.QueueNameAttribute class.
        /// </summary>
        ///
        /// <param name="queueName">    The name of the queue. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QueueNameAttribute(string queueName)
        {
            QueueName = queueName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the name of the queue. </summary>
        ///
        /// <value> The name of the queue. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string QueueName { get; set; }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   An attribute based conventions. </summary>
    ///
    /// <seealso cref="T:EasyNetQ.Conventions"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class AttributeBasedConventions : Conventions
    {
        /// <summary>   The type name serializer. </summary>
        private readonly ITypeNameSerializer _typeNameSerializer;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.AttributeBasedConventions class.
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">    Thrown when one or more required arguments are
        ///                                             null. </exception>
        ///
        /// <param name="typeNameSerializer">   The type name serializer. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public AttributeBasedConventions(ITypeNameSerializer typeNameSerializer)
            : base(typeNameSerializer)
        {
            _typeNameSerializer = typeNameSerializer ?? throw new ArgumentNullException("typeNameSerializer");

            ExchangeNamingConvention = GenerateExchangeName;
            QueueNamingConvention = GenerateQueueName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Generates an exchange name. </summary>
        ///
        /// <param name="messageType">  Type of the message. </param>
        ///
        /// <returns>   The exchange name. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private string GenerateExchangeName(Type messageType)
        {
            var exchangeNameAtt = messageType.GetCustomAttributes(typeof(ExchangeNameAttribute), true).SingleOrDefault() as ExchangeNameAttribute;

            return (exchangeNameAtt == null) ? _typeNameSerializer?.Serialize(messageType) : exchangeNameAtt.ExchangeName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Generates a queue name. </summary>
        ///
        /// <param name="messageType">      Type of the message. </param>
        /// <param name="subscriptionId">   Identifier for the subscription. </param>
        ///
        /// <returns>   The queue name. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private string GenerateQueueName(Type messageType, string subscriptionId)
        {
            var queueNameAtt = messageType?.GetCustomAttributes(typeof(QueueNameAttribute), true)
                .SingleOrDefault() as QueueNameAttribute;

            if (queueNameAtt == null)
            {
                var typeName = _typeNameSerializer?.Serialize(messageType);
                return $"{typeName}_{subscriptionId}";
            }

            return string.IsNullOrWhiteSpace(subscriptionId)
                ? queueNameAtt.QueueName : string.Concat(queueNameAtt.QueueName, "_", subscriptionId);
        }
    }
}
