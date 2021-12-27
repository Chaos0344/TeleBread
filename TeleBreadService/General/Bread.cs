using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data;

namespace TeleBreadService.General
{
    class Bread
    {
        public Bread(ITelegramBotClient botClient, Update e, long chatId, Dictionary<string, string> config)
        {
            var cf = new CommonFunctions(config);
            
            if (e.Message.Entities.Length != 2)
            {
                botClient.SendTextMessageAsync(chatId, 
                    "Please _'Mention'_ a user to use this command \\. Example: /bread @user", 
                    parseMode:ParseMode.MarkdownV2);
                return;
            }
            if (e.Message.Text.ToLower().Contains("/bread @telebread_bot"))
            {
                botClient.SendTextMessageAsync(chatId, "You cannot give the bot bread...");
            }
            long userId;
            long groupChat = e.Message.Chat.Id;
            if (e.Message.Entities[1].Type == MessageEntityType.Mention && e.Message.Entities[1].User is null)
            {
                // User has username
                userId = cf.GetUserId(groupChat, e.Message.Text.Split(' ')[1].Replace("@", ""));
            } else
            {
                userId = e.Message.Entities[1].User.Id;
            }
            if (userId == 0) {
                botClient.SendTextMessageAsync(groupChat, "This user is not in the database yet, or hasn't set their " +
                    "group chat. They can add themselves with the \\group command.");
                return;
            }
            if (userId == e.Message.From.Id)
            {
                botClient.SendTextMessageAsync(groupChat, $"{e.Message.From.FirstName} tried to give themselves bread. " +
                    $"This is a federal crime. The authorities have been contacted.");
                return;
            }

            DataTable dt = cf.RunQuery($"SELECT FirstName FROM dbo.Users WHERE " +
                $"userID = {userId}", new [] { "FirstName" });
            if (dt.Rows.Count < 1)
            {
                botClient.SendTextMessageAsync(groupChat, "This user is not in the database yet, or hasn't set their " +
                    "group chat. They can add themselves with the /group command.");
                return;
            }
            string firstName = dt.Rows[0]["FirstName"].ToString();

            if (cf.CheckInventory("Bread", e.Message.From.Id) < 1)
            {
                botClient.SendTextMessageAsync(chatId, "You do not have any bread to give!");
                return;
            }

            _ = cf.AddToInventory("Bread", -1, e.Message.From.Id);
            int newBread = cf.AddToInventory("Bread", 1, userId);
            int remove = e.Message.Entities[1].Length + e.Message.Entities[1].Offset;
            string text = e.Message.Text.Substring(remove, e.Message.Text.Length-remove).Trim();
            if (text == "")
            {
                text = ".";
            } else
            {
                text = ".\nReason: " + text;
            }
            botClient.SendTextMessageAsync(groupChat,
                $"{e.Message.From.FirstName} thought {firstName} deserved some bread{text}\nNew Bread Balance: {newBread}.");
        }
    }
}
