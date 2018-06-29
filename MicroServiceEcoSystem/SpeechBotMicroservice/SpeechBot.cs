using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechBotMicroservice
{
    using System.Speech.Recognition;
    using System.Speech.Synthesis;
    using System.Threading;
    using System.Timers;
    using Autofac;
    using CacheManager.Core;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using Humanizer;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using Nerdle.Ensure;
    using ReflectSoftware.Insight;
    using Topshelf;
    using Topshelf.Leader;
    using WikipediaNET;
    using WikipediaNET.Enums;
    using WikipediaNET.Objects;
    using Timer = System.Timers.Timer;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A speech bottom. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{SpeechBotMicroservice.SpeechBot, CommonMessages.HealthStatusMessage}"/>
    /// <seealso cref="T:Topshelf.Leader.ILeaseManager"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class SpeechBot : BaseMicroService<SpeechBot, HealthStatusMessage>, ILeaseManager
    {
        /// <summary>   The bus. </summary>
        private IBus _bus = null;
        /// <summary>   The timer. </summary>
        private Timer _timer = null;
        /// <summary>   The container. </summary>
        private IContainer _container;
        /// <summary>   Identifier for the owning node. </summary>
        private string _owningNodeId;
        /// <summary>   The voice. </summary>
        SpeechSynthesizer voice = new SpeechSynthesizer();
        /// <summary>   The speech recognition engine. </summary>
        SpeechRecognitionEngine speechRecognitionEngine = null;
        /// <summary>   The completed. </summary>
        ManualResetEvent _completed = new ManualResetEvent(false);
        /// <summary>   The user interface. </summary>
        private SpeechBotUI ui;
        /// <summary>   The wikipedia. </summary>
        Wikipedia wikipedia = new Wikipedia();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the SpeechBotMicroservice.SpeechBot class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public SpeechBot()
        {

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the SpeechBotMicroservice.SpeechBot class.
        /// </summary>
        ///
        /// <param name="container">    The container. </param>
        /// <param name="owningNode">   The owning node. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public SpeechBot(IContainer container, string owningNode)
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
            Name = "SpeechBot_" + Environment.MachineName;

            Start(host);

            voice.SpeakCompleted += Voice_SpeakCompleted;
            voice.SpeakStarted += Voice_SpeakStarted;
            Subscribe();

            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Starting");
            }
            Cache = CacheFactory.Build("SpeechBotCache", settings => settings.WithSystemRuntimeCacheHandle("SpeechBotCache"));

            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _timer.Start();

            SpeechRequestMessage msg = new SpeechRequestMessage
            {
                rate = 0,
                maleSpeaker = 0,
                volume = 50,
                ID = 1,
                text =
                    "As to country N and President Z, we believe we’re entering a missile renaissance, said Ian Williams, ",
            };

            msg.text += "an associate director at the Center for Strategic and International Studies, who has been compiling data on missile programs in different countries.";
            msg.text += " A growing number of countries with ready access to missiles increases regional tensions and makes war more likely, Mr.Williams said. Countries are more apt to use their arsenals if they think their missiles could be targeted.";
            msg.text += " In addition, many of the missiles being developed by these countries are based on obsolete technologies, which makes them less accurate, increasing the risk to civilians. And there is a risk that missiles could fall into the hands of militias and terrorist groups.";
            msg.text += " Y'all should buy Bitcoin at $15543. Foreign currency comes in at ? 12435. It might also show, says Mr. Williams, like ? 15533435.";

//            Bus.Publish(msg, "Speech");

            WikipediaSearchMessage wmsg = new WikipediaSearchMessage
            {
                maxReturns = 10,
                searchTerm = "cryptocurrency"
            };
            Bus.Publish(wmsg, "Speech");
            return true;
        }

        /// <summary>   True to speaking. </summary>
        private bool speaking;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by Voice for speak started events. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Speak started event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Voice_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            speaking = true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by Voice for speak completed events. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Speak completed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Voice_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            speaking = false;
        }


        /// <summary>   Callback, called when the set. </summary>
        delegate void SetCallback();

        /// <summary>   Closes the window. </summary>
        private void CloseWindow()
        {
            if (ui.textBox1.InvokeRequired)
            {
                SetCallback d = new SetCallback(CloseWindow);
                ui.Invoke(d, new object[] { });
            }
            else
            {
                ui.Close();
            }
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
                logger?.LogInformation(Name + string.Intern(" Heartbeat"));
            }
        }

        /// <summary>   Subscribes this object. </summary>
        private void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });


            IExchange exchange = Bus?.Advanced?.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus?.Advanced?.QueueDeclare("Speech");
            Bus?.Advanced?.Bind(exchange, queue, "");

            Bus?.Subscribe<SpeechRequestMessage>(Environment.MachineName, msg => ProcessSpeechRequestMessage(msg),
                config => config?.WithTopic("Speech"));
            Bus?.Subscribe<WikipediaSearchMessage>(Environment.MachineName, msg => ProcessWikipediaSearchMessage(msg),
                config => config?.WithTopic("Speech"));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the wikipedia search message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessWikipediaSearchMessage(WikipediaSearchMessage msg)
        {
            SearchWikipedia(msg.searchTerm, msg.maxReturns, msg.maxReturns, Language.English);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the speech request message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessSpeechRequestMessage(SpeechRequestMessage msg)
        {
            WriteLineInColor("Received Speech Bot Request", ConsoleColor.Red);
            WriteLineInColor("Text to speak: " + msg.text, ConsoleColor.Yellow);

            voice?.SelectVoiceByHints(msg.maleSpeaker == 1 ? VoiceGender.Male : VoiceGender.Female);

            Ensure.Argument(msg.volume).GreaterThanOrEqualTo(0);
            Ensure.Argument(msg.volume).LessThanOrEqualTo(100);
            Ensure.Argument(msg.rate).GreaterThanOrEqualTo(-10);
            Ensure.Argument(msg.rate).LessThanOrEqualTo(10);

            voice.Volume = msg.volume;
            voice.Rate = msg.rate;

            PromptBuilder builder = new PromptBuilder();
            builder.ClearContent();
            builder.StartSentence();
            builder.AppendText(msg?.text);
            builder.EndSentence();
            voice.SpeakAsync(builder);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes a line in color. </summary>
        ///
        /// <param name="message">          The message. </param>
        /// <param name="foregroundColor">  The foreground color. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WriteLineInColor(string message, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Searches for the first wikipedia. </summary>
        ///
        /// <param name="text">             The text. </param>
        /// <param name="maxResults">       The maximum results. </param>
        /// <param name="resultsToRead">    The results to read. </param>
        /// <param name="language">         The language. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void SearchWikipedia(string text, int maxResults, int resultsToRead, Language language)
        {
            wikipedia.Limit = maxResults;
            
            wikipedia.Language = language;

            voice.SpeakAsync("I just received a search request for the term " + text);
            QueryResult results = wikipedia.Search(text);

            if (results.Search.Count == 0)
            {
                voice.SpeakAsync("I'm sorry, I could not find anything for " + text);
            }
            else
            {
                if (results.Search.Count < resultsToRead)
                    resultsToRead = results.Search.Count;

                voice.SpeakAsync("I found " + results.Search.Count + " results for " + text + ". According to Wikipedia, here are the top " + resultsToRead + " results I found");
                PromptBuilder builder = new PromptBuilder();

                builder.StartSentence();
                for (int x = 0; x < resultsToRead; x++)
                {
                    builder.AppendText(results.Search[x].Snippet.Substring(results.Search[x].Snippet.LastIndexOf("</span>") + 7));
                }
                builder.EndSentence();

                voice.SpeakAsync(builder);
            }
        }
    }
}
