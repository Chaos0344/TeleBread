using System;
using System.Collections.Generic;
using Telegram.Bot;
using System.Data;

namespace TeleBreadService.General
{
    public class Payroll
    {
        private CommonFunctions cf = new CommonFunctions();

        private int queryMembers(long chatId, Dictionary<string, string> config)
        {
            DataTable dt = cf.runQuery($"SELECT COUNT(userID) FROM dbo.Users where groupChat = {chatId}", 
                new string[] {"userCount"}, config);
            return Int32.Parse(dt.Rows[0]["userCount"].ToString());
        }

        private int totalMessages(long chatId, Dictionary<string, string> config)
        {
            DataTable dt = cf.runQuery($"SELECT SUM(messages) FROM Timesheet WHERE groupChat = {chatId}",
                new string[] {"messages"},
                config);
            return Int32.Parse(dt.Rows[0]["messages"].ToString());
        }

        private List<long> getUsers(long chatId, Dictionary<string, string> config)
        {
            DataTable dt = cf.runQuery($"SELECT userID FROM Timesheet WHERE groupChat = {chatId}",
                new string[] {"userId"}, config);
            List<long> users = new List<long>();
            foreach (DataRow row in dt.Rows)
            {
                users.Add(long.Parse(row["userId"].ToString()));
            }

            return users;
        }

        private int getActivity(long chatId, long userId, Dictionary<string, string> config)
        {
            DataTable dt = cf.runQuery($"SELECT messages " +
                                       $"FROM Timesheet " +
                                       $"WHERE userID = {userId} " +
                                       $"AND groupChat = {chatId}",
                new string[] {"messages"}, config);
            return Int32.Parse(dt.Rows[0]["messages"].ToString());
        }

        private List<long> getChats(Dictionary<string, string> config)
        {
            DataTable chts = cf.runQuery("SELECT groupChat FROM GroupChats",
                new string[] {"groupChat"}, config);
            List<long> chats = new List<long>();
            foreach (DataRow row in chts.Rows)
            {
                chats.Add(long.Parse(row["groupChat"].ToString()));
            }

            return chats;
        }

        public Payroll(ITelegramBotClient botClient, Dictionary<string, string> config)
        {
            var chats = getChats(config);
            foreach (long chat in chats)
            {
                if (chat == 0)
                {
                    continue;
                }
                var users = getUsers(chat, config);
                int mems = queryMembers(chat, config);
                int budget = mems * 5;
                int totalMsgs = totalMessages(chat, config);
                foreach (long user in users)
                {
                    int act = getActivity(chat, user, config);
                    int pct = (Int32)Math.Ceiling(((double)act / (double)totalMsgs) * 100);
                    int pay = (Int32)Math.Ceiling((double)budget * ((double)pct / 100));
                    cf.addToInventory("Bread", pay, user, config);
                    long prvt = cf.getPrivateChat(user, config);
                    if (prvt != 0)
                    {
                        botClient.SendTextMessageAsync(prvt,
                            $"You sent {act} messages out of {totalMsgs} and received {pct}% of the pot. " +
                            $"You have received {pay} Bread.");
                    }
                }

                botClient.SendTextMessageAsync(chat,
                    "It's Payday! If you have a private chat configured, you can check it for " +
                    "your pay details, otherwise use /inventory to see your new balance.");
                cf.writeQuery("DELETE FROM dbo.Timesheet", config);
            }
        }
    }
}