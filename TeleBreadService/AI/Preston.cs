using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data;
using TeleBreadService.General;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBreadService.AI
{
    public class Preston
    {
        //private static string _local = "/Users/blakeroetzel/Documents/Dev/TeleBreadService/.local/";
        private string _local = "C:/dev/TeleBread/.local/";
        private static readonly Dictionary<string, string> Config = new Dictionary<string, string>();
        private static CommonFunctions cf = new CommonFunctions(Config);

        private List<long> Chats { get; set; }
        private Dictionary<long, long> Users { get; set; }

        public Preston()
        {
            // Initializer for Preston on server start
            
            // Get List of Chats
            DataTable chats = cf.RunQuery("SELECT DISTINCT groupChat from dbo.GroupChats", new[] {"groupChat"});
            foreach (DataRow row in chats.Rows)
            {
                Chats.Add(long.Parse(row["groupChat"].ToString()));
            }
            
            // Get List of Users
            foreach (long chat in Chats)
            {
                DataTable users = cf.RunQuery($"SELECT userId FROM dbo.Users WHERE groupChat = {chat}",
                    new[] {"userId"});
                foreach (DataRow user in users.Rows)
                {
                    Users[long.Parse(user["userId"].ToString())] = chat;
                }
            }
        }
    }
}