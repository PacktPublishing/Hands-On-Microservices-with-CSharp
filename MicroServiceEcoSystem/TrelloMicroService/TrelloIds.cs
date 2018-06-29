using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    /// <summary>   A trello identifiers. </summary>
    public static class TrelloIds
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// To re-authorize and receive new token (must have login access for the user to approve the
        /// token)
        /// Other developers working on this should create their own sandbox boards and use their user
        /// accounts during the tests Functional tests will not run successfully without a token with
        /// read/write/account access.
        /// https://trello.com/1/authorize?key=062109670e7f56b88783721892f8f66f&name=Manatee.Trello&expiration=1day&response_type=token&scope=read,
        /// write,account.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public const string AppKey = "062109670e7f56b88783721892f8f66f";
        /// <summary>   The user token. </summary>
        public const string UserToken = "2344af8c861fe21c487895db50fb8d29f37794e5d6f64623ce8b56b8b1de4ee8";
        /// <summary>   Name of the user. </summary>
        public const string UserName = "s_littlecrabsolutions";
        /// <summary>   Identifier for the member. </summary>
        public const string MemberId = "514464db3fa062da6e00254f";
        /// <summary>   Identifier for the board. </summary>
        public const string BoardId = "51478f6469fd3d9341001dae";
        /// <summary>   Identifier for the list. </summary>
        public const string ListId = "51478f6469fd3d9341001daf";
        /// <summary>   Identifier for the card. </summary>
        public const string CardId = "5a72b7ab3711a44643c5ed49";
        /// <summary>   Identifier for the check list. </summary>
        public const string CheckListId = "51478f72231d38143c0057e1";
        /// <summary>   Identifier for the organization. </summary>
        public const string OrganizationId = "50d4eb07a1b0902152003329";
        /// <summary>   Identifier for the action. </summary>
        public const string ActionId = "51446f605061aeb832002655";
        /// <summary>   Identifier for the notification. </summary>
        public const string NotificationId = "51832de023195de57800095c";
        /// <summary>   Identifier for the fake. </summary>
        public const string FakeId = "12345a123456acb123456789";
        /// <summary>   Identifier for the short fake. </summary>
        public const string ShortFakeId = "12345a";
        /// <summary>   URL of the attachment. </summary>
        public const string AttachmentUrl = "http://i.imgur.com/H7ybFd0.png";
    }
}
