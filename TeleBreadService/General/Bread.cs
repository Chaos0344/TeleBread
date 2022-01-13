using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

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
        
        public Bread(){}

        public void shop(ITelegramBotClient botClient, Update e, Dictionary<string, string> config, List<ChatListener> listeners)
        {
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Cat Picture (1)")
                {
                    CallbackData = "CatPic"
                }, new InlineKeyboardButton("Cat GIF (3)")
                {
                    CallbackData = "CatGif"
                }
            });
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Dog Picture (1)")
                {
                    CallbackData = "DogPic"
                }, new InlineKeyboardButton("Dog GIF (3)")
                {
                    CallbackData = "DogGif"
                }
            });
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Orb (5)")
                {
                    CallbackData = "Orb"
                }, new InlineKeyboardButton("Inf. Gaunt (10)")
                {
                    CallbackData = "InfGaunt"
                }
            });
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Ring (10)")
                {
                    CallbackData = "Ring"
                }, new InlineKeyboardButton("Purgestone (15)")
                {
                    CallbackData = "Purge"
                }
            });
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Cancel")
                {
                    CallbackData = "Cancel"
                }
            });
            var msgId = botClient.SendTextMessageAsync(e.Message.Chat.Id, "Test box:",
                replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;
            listeners.Add(new ChatListener(e.Message.From.Id, "Callback", $"Shop,{msgId},{e.Message.Chat.Id}"));
        }
    }
}
