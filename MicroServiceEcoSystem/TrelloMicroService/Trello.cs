using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using ExpectedObjects;
    using NodaTime;
    using RabbitMQ.Client.Framing;
    using TrelloNet;

    /// <summary>   A trello milliseconds. </summary>
    public static class TrelloMS
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the trello. </summary>
        ///
        /// <value> The trello. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static ITrello trello { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the board. </summary>
        ///
        /// <value> The board. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static Board board { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the list. </summary>
        ///
        /// <value> The list. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static List list { get; set; }

        /// <summary>   Initializes static members of the TrelloMicroService.TrelloMS class. </summary>
        static TrelloMS()
        {
            trello = new Trello(TrelloIds.AppKey);
            var url = trello.GetAuthorizationUrl("https://trello.com/b/j4CnV8Vq/work", Scope.ReadWrite);
            trello.Authorize(TrelloIds.UserToken);


        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds a list. </summary>
        ///
        /// <param name="name"> The name. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void AddList(string name)
        {
            list = trello.Lists.Add(name, board);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds a card to 'list'. </summary>
        ///
        /// <param name="name"> The name. </param>
        /// <param name="list"> The list. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void AddCard(string name, List list)
        {
            trello.Cards.Add(name, list);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds a board. </summary>
        ///
        /// <param name="name"> The name. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void AddBoard(string name)
        {
            board = trello.Boards.Add(name);
        }
    }
}
