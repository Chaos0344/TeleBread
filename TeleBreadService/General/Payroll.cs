using System;
using System.Collections.Generic;
using Telegram.Bot;
using System.Data;
using System.Linq;
using System.Timers;

namespace TeleBreadService.General
{
    public class Payroll
    {
        private Dictionary<string, string> Config { get; set; }
        private CommonFunctions cf;

        private int QueryMembers(long chatId)
        {
            var dt = cf.RunQuery($"SELECT COUNT(userID) FROM dbo.Users where groupChat = {chatId}", 
                new [] {"userCount"});
            return int.Parse(dt.Rows[0]["userCount"].ToString());
        }

        private int TotalMessages(long chatId)
        {
            var dt = cf.RunQuery($"SELECT SUM(messages) FROM Timesheet WHERE groupChat = {chatId}",
                new [] {"messages"});

            try
            {
                return int.Parse(dt.Rows[0]["messages"].ToString());
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private List<long> GetUsers(long chatId)
        {
            var dt = cf.RunQuery($"SELECT userID FROM Timesheet WHERE groupChat = {chatId}",
                new [] {"userId"});

            return (from DataRow row in dt.Rows select long.Parse(row["userId"].ToString())).ToList();
        }

        private int GetActivity(long chatId, long userId)
        {
            var dt = cf.RunQuery($"SELECT messages " +
                                 $"FROM Timesheet " +
                                 $"WHERE userID = {userId} " +
                                 $"AND groupChat = {chatId}",
                new [] {"messages"});
            return int.Parse(dt.Rows[0]["messages"].ToString());
        }

        private List<long> GetChats()
        {
            var chats = cf.RunQuery("SELECT groupChat FROM GroupChats",
                new [] {"groupChat"});

            return (from DataRow row in chats.Rows select long.Parse(row["groupChat"].ToString())).ToList();
        }

        public Payroll(ITelegramBotClient botClient, Dictionary<string, string> c, Timer payDay)
        {
            Config = c;
            cf = new CommonFunctions(Config);
            var chats = GetChats();
            foreach (var chat in chats)
            {
                if (chat == 0)
                {
                    continue;
                }
                var users = GetUsers(chat);
                var members = QueryMembers(chat);
                var budget = members * 5;
                var totalMessages = TotalMessages(chat);
                if (totalMessages == 0)
                {
                    continue;
                }
                foreach (var user in users)
                {
                    var act = GetActivity(chat, user);
                    var pct = (int)Math.Round(act / (double)totalMessages * 100);
                    var pay = (int)Math.Round(budget * (double)pct / 100);
                    cf.AddToInventory("Bread", pay, user);
                    var pvt = cf.GetPrivateChat(user);
                    if (pvt != 0)
                    {
                        botClient.SendTextMessageAsync(pvt,
                            $"You sent {act} messages out of {totalMessages} and received {pct}% of the pot. " +
                            $"You have received {pay} Bread.");
                    }
                }

                botClient.SendTextMessageAsync(chat,
                    "It's Payday! If you have a private chat configured, you can check it for " +
                    "your pay details, otherwise use /inventory to see your new balance.");
                cf.WriteQuery("DELETE FROM dbo.Timesheet");
            }

            payDay.Enabled = false;
            payDay.Interval =
                (DateTime.Parse(DateTime.Now.AddDays(7).ToString("MM/dd/yyyy")+" 06:00:00 AM") - DateTime.Now)
                .TotalMilliseconds;
            payDay.Enabled = true;
        }
        
        public Payroll(ITelegramBotClient botClient, Dictionary<string, string> c)
        {
            Config = c;
            cf = new CommonFunctions(Config);
            var chats = GetChats();
            foreach (var chat in chats)
            {
                if (chat == 0)
                {
                    continue;
                }
                var users = GetUsers(chat);
                var members = QueryMembers(chat);
                var budget = members * 5;
                var totalMessages = TotalMessages(chat);
                if (totalMessages == 0)
                {
                    continue;
                }
                foreach (var user in users)
                {
                    var act = GetActivity(chat, user);
                    var pct = (int)Math.Round(act / (double)totalMessages * 100);
                    var pay = (int)Math.Round(budget * (double)pct / 100);
                    cf.AddToInventory("Bread", pay, user);
                    var pvt = cf.GetPrivateChat(user);
                    if (pvt != 0)
                    {
                        botClient.SendTextMessageAsync(pvt,
                            $"You sent {act} messages out of {totalMessages} and received {pct}% of the pot. " +
                            $"You have received {pay} Bread.");
                    }
                }

                botClient.SendTextMessageAsync(chat,
                    "It's Payday! If you have a private chat configured, you can check it for " +
                    "your pay details, otherwise use /inventory to see your new balance.");
                cf.WriteQuery("DELETE FROM dbo.Timesheet");
            }
        }
    }
}