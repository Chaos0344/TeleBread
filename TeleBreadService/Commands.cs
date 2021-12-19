using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using System.Timers;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data;
using System.Threading.Tasks;

namespace TeleBreadService
{
    public class Commands
    {
        public Commands()
        {
        }

        /// <summary>
        /// Returns basic instructions on how to get more info or begin using the bot.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void start(ITelegramBotClient botClient, Update e)
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
        /// <param name="config"></param>
        public async void privateChat(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var c = new General.CommonFunctions();
            if (c.userInDatabase(e.Message.From.Id, config))
            {
                c.writeQuery($"Update dbo.Users set privateChat = {e.Message.Chat.Id} " +
                    $"where userID = {e.Message.From.Id}", config);
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been updated.");
            } else
            {
                c.writeQuery($"INSERT INTO dbo.Users (userID, username, FirstName, LastName, privateChat) " +
                    $"VALUES ({e.Message.From.Id}, '{e.Message.From.Username}', '{e.Message.From.FirstName}', " +
                    $"'{e.Message.From.LastName}', {e.Message.Chat.Id})", config);
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been added to database.");
            }
        }

        /// <summary>
        /// Adds user to database if doesn't exist, and updates group chat if user already exists.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        /// <param name="config"></param>
        public async void groupChat(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var c = new General.CommonFunctions();

            if (!c.groupChatExists(e.Message.Chat.Id, config))
            {
                c.writeQuery($"INSERT INTO dbo.GroupChats (groupChat, dateAdded) VALUES ({e.Message.Chat.Id}, '{DateTime.Now}')", config);
            }

            if (c.userInDatabase(e.Message.From.Id, config))
            {
                c.writeQuery($"Update dbo.Users set groupChat = {e.Message.Chat.Id} " +
                    $"where userID = {e.Message.From.Id}", config);
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been updated.");
            }
            else
            {
                c.writeQuery($"INSERT INTO dbo.Users (userID, username, FirstName, LastName, groupChat) " +
                    $"VALUES ({e.Message.From.Id}, '{e.Message.From.Username}', '{e.Message.From.FirstName}', " +
                    $"'{e.Message.From.LastName}', {e.Message.Chat.Id})", config);
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "User has been added to database.");
            }
        }

        /// <summary>
        /// Admin command, sends message after pipe to group from bot.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        /// <param name="config"></param>
        public async void say(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var chatID = new General.CommonFunctions().getGroupChat(e.Message.From.Id, config);
            await botClient.SendTextMessageAsync(chatID, e.Message.Text.ToString().Split('|')[1]);
        }

        public async void boobs(ITelegramBotClient botClient, Update e)
        {
            await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "Lol nice",
                    disableNotification: true);
        }

        public async void lick(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var c = new General.CommonFunctions();

            if (c.getGroupChat(e.Message.From.Id, config) != e.Message.Chat.Id)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "Lick cannot be performed outside of your group chat.");
                return;
            }

            DataTable dt = c.runQuery("SELECT TOP 1 [Food Name] from dbo.food order by NEWID()", new string[] { "FoodName" }, config);
            string foodName = dt.Rows[0]["FoodName"].ToString();
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"{e.Message.From.FirstName} licked {foodName.ToLower()}.\nBon appétit!");
            /*
            if (foodName.ToLower().Contains("bread"))
            {
                //TODO after addBread command is revised/created.
                var newBread = addBread(e.Message.Chat.Id.ToString(), e.Message.From.Id.ToString());
                await botClient.SendTextMessageAsync(chatId: e.Message.Chat,
                        text: $"{e.Message.From.FirstName} found some secret bread!\nNew bread balance: {newBread}.",
                        disableNotification: true);
            }*/
        }

        /// <summary>
        /// Admin command, sets chat in maintenance mode.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        /// <param name="config"></param>
        public async void maintenance(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var c = new General.CommonFunctions();
            int status = c.serviceStatus("Maintenance", config);
            if (status == 1)
            {
                c.writeQuery("Update dbo.Services set Status = 0 where ServiceName = 'Maintenance'", config);
                botClient.SendTextMessageAsync(e.Message.Chat.Id, "Maintenance mode disabled.");
            }
            else
            {
                c.writeQuery("Update dbo.Services set Status = 1 where ServiceName = 'Maintenance'", config);
                botClient.SendTextMessageAsync(e.Message.Chat.Id, "Maintenance mode enabled.");
            }
        }

        /// <summary>
        /// Inventory command allows users to check their inventory and see item descriptions if they are holding that item.
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        /// <param name="config"></param>
        public async void inventory(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            long userID = e.Message.From.Id;
            var c = new General.CommonFunctions();
            if (e.Message.Text.ToLower() == "/inventory")
            {
                DataTable dt = c.runQuery($"SELECT ItemName, Quantity " +
                    $"FROM dbo.Inventory " +
                    $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                    $"WHERE UserID = {userID}",
                    new string[] { "ItemName", "Quantity" }, config);
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
                DataTable dt = c.runQuery($"SELECT Items.ItemDescription " +
                    $"FROM dbo.inventory " +
                    $"JOIN dbo.Items on Inventory.ItemID = Items.ItemID " +
                    $"WHERE UserID = {userID} " +
                    $"AND ItemName = '{itemName}' " +
                    $"AND Quantity > 0", new string[] { "Description" }, config);
                if (dt.Rows.Count < 1)
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"You are not holding any {itemName}.");
                    return;
                } else
                {
                    await botClient.SendTextMessageAsync(e.Message.Chat.Id, dt.Rows[0]["Description"].ToString());
                }
            }
        }

    }
}
