using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using ExpectedObjects;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using NodaTime;
    using Topshelf;
    using TrelloNet;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A trello micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{TrelloMicroService.TrelloMicroService, CommonMessages.TrelloResponseMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class TrelloMicroService : BaseMicroService<TrelloMicroService, TrelloResponseMessage>
    {
        //TrelloAuthorization.Default.AppKey = "9dbf8c09499d07abac02bbd6d5af4b9c";
        //TrelloAuthorization.Default.UserToken = "95da70bf03bd43b82648f515477d44ec84baa2fb9e811cb7284be10d94512b81";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the trello. </summary>
        ///
        /// <value> The trello. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ITrello trello { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the board. </summary>
        ///
        /// <value> The board. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Board board { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the list. </summary>
        ///
        /// <value> The list. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public List list { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the card. </summary>
        ///
        /// <value> The card. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Card card { get; set; }
        /// <summary>   The random. </summary>
        private Random random = new Random();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the TrelloMicroService.TrelloMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public TrelloMicroService()
        {
            Name = "Trello Microservice_" + Environment.MachineName;
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
            Subscribe();
            trello = new Trello("16548e3857c79f75e31bb40002a0595b"); 
            var url = trello.GetAuthorizationUrl("dummy", Scope.ReadWrite);
            trello.Authorize("6a5a70e52074548a989ce1ac8eb48bb1dd60f7ac80645caf5fa0b1814727c955");




            var expectedBoard = CreateBoard("Microservice Ecosystem", "Hands on Microservices with C#");
            var expectedList = CreateList("Drafts");

            for (int x=0; x<10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(12)
                        : DateTime.MinValue);


            CreateList("Proofs");

            for (int x = 0; x < 10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(24)
                        : DateTime.MinValue);


            CreateList("Final Copies");

            for (int x = 0; x < 10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(36)
                        : DateTime.MinValue);


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
            IQueue queue = Bus.Advanced.QueueDeclare("MachineLearning");
            Bus.Advanced.Bind(exchange, queue, "");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a board. </summary>
        ///
        /// <param name="name"> The name. </param>
        /// <param name="desc"> The description. </param>
        ///
        /// <returns>   The new board. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private ExpectedObject CreateBoard(string name, string desc)
        {
           board = trello.Boards.Add(name);
            
            if (board != null)
            {
                board.Closed = false;
                board.Pinned = true;
                board.Desc = desc;
                board.Prefs = new BoardPreferences
                {
                    Comments = CommentPermission.Members,
                    Invitations = InvitationPermission.Members,
                    PermissionLevel = PermissionLevel.Public,
                    Voting = VotingPermission.Members
                };
            }
            
            return board.ToExpectedObject();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a list. </summary>
        ///
        /// <param name="name"> The name. </param>
        ///
        /// <returns>   The new list. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private ExpectedObject CreateList(string name)
        {
            // Create a new list
            BoardId bi = new BoardId(board.GetBoardId());
            list = trello.Lists.Add(new NewList(name, bi));

            if (list != null)
            {
                list.Closed = false;
                list.IdBoard = board.GetBoardId();
                list.Name = name;
                list.Pos = 1;
            }
            
            return list.ToExpectedObject();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a card. </summary>
        ///
        /// <param name="name">     The name. </param>
        /// <param name="desc">     The description. </param>
        /// <param name="closed">   True if closed. </param>
        /// <param name="dueDate">  The due date. </param>
        ///
        /// <returns>   The new card. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private ExpectedObject CreateCard(string name, string desc, bool closed, DateTime dueDate)
        {
            Card c = trello.Cards.Add(name, list);
            if (c != null)
            {
                c.Desc = desc;
                c.Closed = closed;
                //c.IdList = "4f2b8b4d4f2cb9d16d3684c1";
                c.IdBoard = board.GetBoardId();
                if (dueDate > DateTime.MinValue)
                    c.Due = dueDate;
                c.Labels = new List<Label>();
                c.IdShort = 1;
                c.Checklists = new List<Card.Checklist>();
                c.Url = "https://trello.com/b/bz7S3fiv/trello-microservice";
                c.ShortUrl = "https://trello.com/b/bz7S3fiv";
                c.Pos = 32768;
                c.DateLastActivity = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime();
                c.Badges = new Card.CardBadges
                {
                    Votes = 1,
                    Attachments = 1,
                    Comments = 2,
                    CheckItems = 0,
                    CheckItemsChecked = 0,
                    Description = true,
                    Due = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(1),
                    FogBugz = ""
                };
                
                c.IdMembers = new List<string> {"4f2b8b464f2cb9d16d368326"};
            }



            if (dueDate == DateTime.MinValue)
            {
                Label l = new Label
                {
                    Color = Color.Green,
                    IdBoard = board.GetBoardId(),
                    Name = "Green Label"
                };
                c.Labels.Add(l);

                //// Label card
                trello.Cards.AddLabel(c, Color.Green);
            }
            trello.Cards.Update(c);

            // Assign member to card
            trello.Cards.AddMember(c, trello.Members.Me());

            // Comment on a card
            trello.Cards.AddComment(c, RandomString(50));

            Card.CheckItem ci = new Card.CheckItem();
            ci.Pos = 994;
            ci.Name = "Draft";
            ci.Id = RandomString(12);

            Card.Checklist cl = new Card.Checklist();
            cl.IdBoard = board.GetBoardId();
            cl.Name = "To Do";
            cl.Pos = 2485;
            cl.CheckItems = new List<Card.CheckItem>();
            c.Checklists.Add(cl);
            trello.Cards.Update(c);

            cl.CheckItems.Add(ci);
            trello.Cards.Update(c);





            card = c;
            return c.ToExpectedObject();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates check list. </summary>
        ///
        /// <param name="name"> The name. </param>
        ///
        /// <returns>   The new check list. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private ExpectedObject CreateCheckList(string name)
        {
            BoardId bi = new BoardId(board.GetBoardId());
            var aNewChecklist = trello.Checklists.Add(name, board);


            //ChecklistId id = new ChecklistId(aNewChecklist.GetChecklistId());
            //trello.Cards.AddChecklist(card, id);

            // Add check items
            trello.Checklists.AddCheckItem(aNewChecklist, "First Draft");
            return aNewChecklist.ToExpectedObject();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Random string. </summary>
        ///
        /// <param name="length">   The length. </param>
        ///
        /// <returns>   A string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
