using System;
using System.Collections.Generic;
using System.Data;
using System.Security;
using TeleBreadService.General;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBreadService.Items
{
    public class UseItem
    {
        private Dictionary<string, string> config { get; set; }
        private General.CommonFunctions cf { get; set; }
        private ITelegramBotClient botClient { get; set; }

        /// <summary>
        /// Set-up Use Item to use a sub command. Rare use-case
        /// </summary>
        /// <param name="bc">botClient</param>
        /// <param name="c">config</param>
        public UseItem(ITelegramBotClient bc, Dictionary<string, string> c)
        {
            config = c;
            botClient = bc;
            cf = new CommonFunctions(config);
        }

        public UseItem(ITelegramBotClient bc, Update e, List<ChatListener> l, Dictionary<string, string> c)
        {
            config = c;
            botClient = bc;
            var userId = e.Message.From.Id;
            string item = e.Message.Text.ToLower().Replace("/use ", "").Trim();
            cf = new General.CommonFunctions(config);
            DataTable dt = cf.RunQuery($"SELECT ItemName, Quantity " +
                                      $"FROM dbo.Inventory " +
                                      $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                                      $"WHERE UserID = {userId}",
                new[] { "ItemName", "Quantity" });
            foreach (DataRow row in dt.Rows)
            {
                int quantity = Int32.Parse(row["Quantity"].ToString());
                string returnedItem = row["ItemName"].ToString();
                if (quantity > 0 && returnedItem.ToLower() == item)
                {
                    switch (item)
                    {
                        case "orb":
                            UseOrb(e, l);
                            break;
                        case "infinity gauntlet":
                            botClient.SendTextMessageAsync(e.Message.Chat.Id,
                                "The power surges through you. The urge to /snap is strong...");
                            break;
                    }

                    return;
                    // TODO: Add additional items here
                }
            }

            botClient.SendTextMessageAsync(e.Message.Chat.Id, $"You aren't holding any {item}");
        }

        public async void Snap(Update e)
        {
            var chatId = e.Message.Chat.Id;
            var userId = e.Message.From.Id;
            int inv = cf.CheckInventory("Infinity Gauntlet", userId);
            if (inv < 1)
            {
                await botClient.SendTextMessageAsync(chatId, "You snap while humming a little tune.");
                return;
            }

            cf.AddToInventory("Infinity Gauntlet", -1, userId);
            await botClient.SendTextMessageAsync(chatId, "As you snap a silence falls across the world. " +
                                                   "A scream is heard in the distance. TeleBreadBot looks scared as " +
                                                   "it fades to dust..");
            cf.WriteQuery($"INSERT INTO dbo.Snaps (groupChat, ExpirationDate) " +
                          $"VALUES ({e.Message.Chat.Id}, '{DateTime.Now.AddMinutes(10)}')");
        }
        
        private async void UseOrb(Update e, List<ChatListener> listeners)
        {
            try
            {
                var userId = e.Message.From.Id;
                var privateChat = cf.GetPrivateChat(userId);
                var groupChat = e.Message.Chat.Id;
                if (privateChat != 0)
                {
                    DataTable dt = cf.RunQuery($"SELECT FirstName, userID from dbo.Users where groupChat = {groupChat}",
                        new[] {"FirstName", "userID"});
                    List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        string First = row["FirstName"].ToString();
                        long gcuserId = long.Parse(row["userID"].ToString());
                        if (gcuserId != userId)
                        {
                            buttons.Add(new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton(First)
                                {
                                    Text = First,
                                    CallbackData = gcuserId.ToString()
                                }
                            });
                        }
                    }
                    buttons.Add(new List<InlineKeyboardButton>()
                    {
                        new InlineKeyboardButton("Cancel")
                        {
                            CallbackData = "Cancel"
                        }
                    });

                    var msgId = botClient.SendTextMessageAsync(privateChat, "Select a target:",
                        replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;
                    listeners.Add(new ChatListener(e.Message.From.Id, "Callback", $"OrbTarget,{msgId},{privateChat}"));
                    return;
                }

                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You have not linked your private chat " +
                                                                        "yet. Please open a private chat with " +
                                                                        "TeleBread and use the /private command to use " +
                                                                        "this item.");
            }
            catch (Exception z)
            {
                new Service1().WriteToFile(z.ToString());
            }
        }
    }
}