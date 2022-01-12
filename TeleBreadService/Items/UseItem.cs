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
                    if (item == "orb")
                    {
                        UseOrb(e, l);
                        return;
                    }
                    // TODO: Add additional items here
                }
            }

            botClient.SendTextMessageAsync(e.Message.Chat.Id, $"You aren't holding any {item}");
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

                    listeners.Add(new ChatListener(e.Message.From.Id, "Callback", "OrbTarget"));
                    await botClient.SendTextMessageAsync(privateChat, "Select a target:",
                        replyMarkup: new InlineKeyboardMarkup(buttons));
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