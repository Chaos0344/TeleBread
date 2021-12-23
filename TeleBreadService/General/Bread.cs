using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data;

namespace TeleBreadService.General
{
    class Bread
    {
        public async void bread(ITelegramBotClient botClient, Update e, long chatId, Dictionary<string, string> config)
        {
            if (e.Message.Entities.Length != 2)
            {
                await botClient.SendTextMessageAsync(chatId, 
                    "Please _'Mention'_ a user to use this command \\. Example: /bread @user", 
                    parseMode:Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
                return;
            }
            if (e.Message.Text.ToLower().Contains("/bread @telebread_bot"))
            {
                await botClient.SendTextMessageAsync(chatId, "You cannot give the bot bread...");
            }
            long userID;
            long groupChat = e.Message.Chat.Id;
            if (e.Message.Entities[1].Type == MessageEntityType.Mention && e.Message.Entities[1].User is null)
            {
                // User has username
                userID = new General.CommonFunctions().getUserId(groupChat, e.Message.Text.Split(' ')[1].Replace("@", ""), config);
            } else
            {
                userID = e.Message.Entities[1].User.Id;
            }
            if (userID == 0) {
                await botClient.SendTextMessageAsync(groupChat, "This user is not in the database yet, or hasn't set their " +
                    "group chat. They can add themselves with the \\group command.");
                return;
            }
            if (userID == e.Message.From.Id)
            {
                await botClient.SendTextMessageAsync(groupChat, $"{e.Message.From.FirstName} tried to give themselves bread. " +
                    $"This is a federal crime. The authorities have been contacted.");
                return;
            }

            DataTable dt = new General.CommonFunctions().runQuery("SELECT FirstName FROM dbo.Users WHERE " +
                $"userID = {userID}", new string[] { "FirstName" }, config);
            if (dt.Rows.Count < 1)
            {
                await botClient.SendTextMessageAsync(groupChat, "This user is not in the database yet, or hasn't set their " +
                    "group chat. They can add themselves with the /group command.");
                return;
            }
            string firstName = dt.Rows[0]["FirstName"].ToString();



            int newBread = new General.CommonFunctions().addToInventory("Bread", 1, userID, config);
            int remove = e.Message.Entities[1].Length + e.Message.Entities[1].Offset;
            string text = e.Message.Text.Substring(remove, e.Message.Text.Length-remove).Trim();
            if (text == "")
            {
                text = ".";
            } else
            {
                text = ".\nReason: " + text;
            }
            await botClient.SendTextMessageAsync(groupChat,
                $"{e.Message.From.FirstName} thought {firstName} deserved some bread{text}\nNew Bread Balance: {newBread}.");
        }
    }
}
