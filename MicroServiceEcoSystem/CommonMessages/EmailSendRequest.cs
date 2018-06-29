using System;

namespace CommonMessages
{
    using EasyNetQ;

    /// <summary>   (Serializable) the email request message. </summary>
    [Queue("Email", ExchangeName = "EvolvedAI")]
    [Serializable]
    public class EmailSendRequest
    {
        /// <summary>   Source for the. </summary>
        public string From;
        /// <summary>   to. </summary>
        public string To;
        /// <summary>   The subject. </summary>
        public string Subject;
        /// <summary>   The body. </summary>
        public string Body;
    }
}
