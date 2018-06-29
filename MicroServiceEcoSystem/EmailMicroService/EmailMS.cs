using System;
using System.Text;
using System.Threading.Tasks;
using CommonMessages;
using EasyNetQ;
using JetBrains.Annotations;
using ReflectSoftware.Insight;
using Topshelf;


namespace EmailMicroService
{
    using System.ComponentModel;
    using System.Net.Mail;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using MicroServiceEcoSystem;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   An email milliseconds. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{EmailMicroService.EmailMS, CommonMessages.DeploymentStopMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class EmailMS : BaseMicroService<EmailMS, DeploymentStopMessage>
    {

        /// <summary>   Initializes a new instance of the EmailMicroService.EmailMS class. </summary>
        public EmailMS()
        {
            Name = "Email Microservice_" + Environment.MachineName;
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
            Start(host);
            Subscribe();
            EmailSendRequest m = new EmailSendRequest();
            m.Subject = "Test";
            m.To = "noone@gmail.com";
            m.From = "nobody@nowhere.com";
            m.Body = "This is a test of the email microservice system";
            SendEmail(m);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            Continue();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            Pause();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            Resume();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            Shutdown();
            return true;
        }

        /// <summary>   True to mail sent. </summary>
        static bool mailSent = false;
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
            IQueue queue = Bus.Advanced.QueueDeclare("Email");
            Bus.Advanced.Bind(exchange, queue, Environment.MachineName);

            Bus.Subscribe<EmailSendRequest>(Environment.MachineName, msg => { ProcessEmailSendRequestMessage(msg); },
                config => config?.WithTopic("Email"));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the email send request message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessEmailSendRequestMessage(EmailSendRequest msg)
        {
            Console.WriteLine("Received Email Send Request Message");
            RILogManager.Default?.SendInformation("Received Email Send Request Message");
            SendEmail(msg);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sends a completed callback. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Asynchronous completed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            String token = (string)e.UserState;

            if (e.Cancelled)
            {
                Console.WriteLine("[{0}] Send canceled.", token);
            }
            if (e.Error != null)
            {
                Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
            }
            else
            {
                Console.WriteLine("Message sent.");
            }
            mailSent = true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sends an email. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SendEmail(EmailSendRequest msg)
        {
            Console.WriteLine(EmailSender.Send(msg.From, msg.To, msg.Subject, msg.Body)
                ? "Email sent" : "Email not sent");
        }
    }
}
