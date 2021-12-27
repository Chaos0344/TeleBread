using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data;

namespace TeleBreadService
{
    public class Commands
    {
        private Dictionary<string, string> Config { get; set; }

        public Commands(Dictionary<string, string> c)
        {
            Config = c;
        }

        /// <summary>
        /// Returns basic instructions on how to get more info or begin using the bot.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public static async void Start(ITelegramBotClient botClient, Update e)
        {
            await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                "Hello! TeleBread is a bot primarily made for goofing around with friends in a group chat!\n" +
                "Please visit telebread.net if you want additional information about our bot!");
            await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                "If you already know how to use this bot and wish to proceed, please use /group to link this as " +
                "your group chat, or /private to link it as your private chat.");
        }

        /// <summary>
        /// Adds user to database if doesn't exist, and updates private chat if user already exists.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void PrivateChat(ITelegramBotClient botClient, Update e)
        {
            var c = new General.CommonFunctions(Config);
            if (c.UserInDatabase(e.Message.From.Id))
            {
                c.WriteQuery($"Update dbo.Users set privateChat = {e.Message.Chat.Id} " +
                    $"where userID = {e.Message.From.Id}");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been updated.");
            } else
            {
                c.WriteQuery($"INSERT INTO dbo.Users (userID, username, FirstName, LastName, privateChat) " +
                    $"VALUES ({e.Message.From.Id}, '{e.Message.From.Username}', '{e.Message.From.FirstName}', " +
                    $"'{e.Message.From.LastName}', {e.Message.Chat.Id})");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been added to database.");
            }
        }

        /// <summary>
        /// Adds user to database if doesn't exist, and updates group chat if user already exists.
        /// ALSO checks if GroupChat exists in the DB yet.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void GroupChat(ITelegramBotClient botClient, Update e)
        {
            var c = new General.CommonFunctions(Config);

            if (!c.GroupChatExists(e.Message.Chat.Id))
            {
                c.WriteQuery($"INSERT INTO dbo.GroupChats (groupChat, dateAdded) " +
                             $"VALUES ({e.Message.Chat.Id}, '{DateTime.Now}')");
                c.WriteQuery($"INSERT INTO dbo.Services (Service, groupChat, Status) " +
                             $"VALUES ('ItemChance', {e.Message.Chat.Id}, 1000)");
                c.AddToInventory("Bread", 5, e.Message.From.Id);
            }

            if (c.UserInDatabase(e.Message.From.Id))
            {
                c.WriteQuery($"Update dbo.Users set groupChat = {e.Message.Chat.Id} " +
                    $"where userID = {e.Message.From.Id}");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been updated.");
            }
            else
            {
                c.WriteQuery($"INSERT INTO dbo.Users (userID, username, FirstName, LastName, groupChat) " +
                    $"VALUES ({e.Message.From.Id}, '{e.Message.From.Username}', '{e.Message.From.FirstName}', " +
                    $"'{e.Message.From.LastName}', {e.Message.Chat.Id})");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been added to database.");
            }
        }

        /// <summary>
        /// Admin command, sends message after pipe to group from bot.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void Say(ITelegramBotClient botClient, Update e)
        {
            var chatId = new General.CommonFunctions(Config).GetGroupChat(e.Message.From.Id);
            await botClient.SendTextMessageAsync(chatId, e.Message.Text.Split('|')[1]);
        }

        public async void Boobs(ITelegramBotClient botClient, Update e)
        {
            await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "Lol nice",
                    disableNotification: true);
        }

        public async void Lick(ITelegramBotClient botClient, Update e)
        {
            var c = new General.CommonFunctions(Config);

            if (c.GetGroupChat(e.Message.From.Id) != e.Message.Chat.Id)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Lick cannot be performed outside of your group chat.");
                return;
            }

            DataTable dt = c.RunQuery("SELECT TOP 1 foodName from dbo.food order by NEWID()", new[] { "FoodName" });
            string foodName = dt.Rows[0]["FoodName"].ToString();
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"{e.Message.From.FirstName} licked {foodName.ToLower()}.\nBon appétit!");
            
            if (foodName.ToLower().Contains("bread"))
            {
                if (!c.UserInDatabase(e.Message.From.Id))
                {
                    await botClient.SendTextMessageAsync(chatId: e.Message.Chat,
                        text:
                        $"{e.Message.From.FirstName} found some secret bread!, but they aren't in the database yet.",
                        disableNotification: true);
                    return;
                }
                //TODO after addBread command is revised/created.
                var newBread = c.AddToInventory("Bread", 1, e.Message.From.Id);
                await botClient.SendTextMessageAsync(chatId: e.Message.Chat,
                        text: $"{e.Message.From.FirstName} found some secret bread!\nNew bread balance: {newBread}.",
                        disableNotification: true);
            }
        }

        /// <summary>
        /// Admin command, sets chat in maintenance mode.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void Maintenance(ITelegramBotClient botClient, Update e)
        {
            var c = new General.CommonFunctions(Config);
            int status = c.ServiceStatus("Maintenance", 0);
            if (status == 1)
            {
                c.WriteQuery("Update dbo.Services set Status = 0 where Service = 'Maintenance'");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Maintenance mode disabled.");
            }
            else
            {
                c.WriteQuery("Update dbo.Services set Status = 1 where Service = 'Maintenance'");
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Maintenance mode enabled.");
            }
        }

        /// <summary>
        /// Inventory command allows users to check their inventory and see item descriptions if they are holding that item.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void Inventory(ITelegramBotClient botClient, Update e)
        {
            long userId = e.Message.From.Id;
            var c = new General.CommonFunctions(Config);
            if (e.Message.Text.ToLower() == "/inventory")
            {
                DataTable dt = c.RunQuery($"SELECT ItemName, Quantity " +
                    $"FROM dbo.Inventory " +
                    $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                    $"WHERE UserID = {userId}",
                    new[] { "ItemName", "Quantity" });
                var itemList = "You are holding:\n";
                foreach (DataRow row in dt.Rows)
                {
                    if (Int32.Parse(row["Quantity"].ToString()) > 0)
                        itemList += $"{row["ItemName"]}: {row["Quantity"]}\n";
                }
                itemList = itemList.TrimEnd('\n');
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, itemList);
            }
            else
            {
                // Get the description of an item in their inventory.
                var itemName = e.Message.Text.ToLower().Replace("/inventory ", "").Trim();
                DataTable dt = c.RunQuery($"SELECT Items.ItemDescription " +
                    $"FROM dbo.inventory " +
                    $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                    $"WHERE UserID = {userId} " +
                    $"AND ItemName = '{itemName}' " +
                    $"AND Quantity > 0", new[] { "Description" });
                if (dt.Rows.Count < 1)
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"You are not holding any {itemName}.");
                } else
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, dt.Rows[0]["Description"].ToString());
                }
            }
        }

    }
}
