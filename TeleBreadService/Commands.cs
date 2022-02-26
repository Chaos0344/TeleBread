using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data;
using TeleBreadService.General;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Poll = TeleBreadService.General.Poll;

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

        public async void Odds(ITelegramBotClient botClient, Update e)
        {
            var cf = new CommonFunctions(Config);
            var odds = cf.ServiceStatus("ItemChance", e.Message.Chat.Id);
            await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"The odd are currently 1 in {odds}.");
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
            string foodName = dt.Rows[0]["FoodName"].ToString().Replace("\"", "");
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

        public async void Lick(ITelegramBotClient botClient, Update e, string item)
        {
            await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                $"{e.Message.From.FirstName} licked {item}\nThe prophecy has come true!");
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
        /// Inserts a support ticket into the database
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="e"></param>
        public async void Support(ITelegramBotClient botClient, Update e)
        {
            var request = e.Message.Text.Replace("/support", "");
            request = request.Replace("'", "");
            request = request.Replace("@", "");
            CommonFunctions cf = new CommonFunctions(Config);
            cf.WriteQuery($"INSERT INTO dbo.TICKETS (UserID, [Date], Request) VALUES ({e.Message.From.Id}, '{DateTime.Now}','{request}')");
            await botClient.SendTextMessageAsync(e.Message.Chat, "Your support ticket has been submitted.");
        }

        public async void Resolve(ITelegramBotClient botClient, Update e)
        {
            int rID = Int32.Parse(e.Message.Text.Replace("/resolve ", "").Split(' ')[0]);
            string resolution = e.Message.Text.Replace($"/resolve {rID} ", "");
            var cf = new CommonFunctions(Config);
            var dt = cf.RunQuery($"SELECT UserID, Request from dbo.Tickets where TicketID = {rID}",
                new[] {"UserID", "Request"});
            if (dt.Rows.Count < 1)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat, "Request not found");
                return;
            }

            var user = long.Parse(dt.Rows[0]["UserID"].ToString());
            var request = dt.Rows[0]["Request"].ToString();

            char[] escapeThem = new[]
                {'_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'};

            foreach (var esc in escapeThem)
            {
                request = request.Replace($"{esc}", $"\\{esc}");
            }

            var un = cf.GetFirstName(user);
            
            cf.WriteQuery($"UPDATE dbo.Tickets set Resolved = 1, Resolution = '{resolution}' where TicketID = {rID}");

            long gc = cf.GetGroupChat(user);
            if (gc != 0)
            {
                await botClient.SendTextMessageAsync(gc,
                    $"*Attention [{un}](tg://user?id={user})*\nYour support ticket \"{request}\"  has been resolved with resolution: {resolution}", ParseMode.MarkdownV2);
            }

            return;
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

        public async void Badges(ITelegramBotClient botClient, Update e)
        {
            var userId = e.Message.From.Id;
            CommonFunctions cf = new CommonFunctions(Config);
            DataTable dt = cf.RunQuery($"SELECT badge, [date] FROM dbo.Badges where userID = {userId} order by [date]",
                new[] {"badge", "date"});

            if (dt.Rows.Count < 1)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id,
                    "You do not have any badges. Do something badge-worthy.");
                return;
            }
            
            

            var badges = "You have the following badges:";
            foreach (DataRow row in dt.Rows)
            {
                DateTime badgeDate = DateTime.Parse(row["date"].ToString());
                badges += $"\n{row["badge"]} : {badgeDate.ToString("MM-dd-yy")}";
            }

            await botClient.SendTextMessageAsync(e.Message.Chat.Id, badges);
        }

        public async void GiveBadge(ITelegramBotClient botClient, Update e, List<Poll> polls)
        {
            var cf = new CommonFunctions(Config);
            var chatId = e.Message.Chat.Id;
            var splitMsg = e.Message.Text.Split(' ');
            var badge = e.Message.Text.Replace($"{splitMsg[0]} {splitMsg[1]} ", "");
            
            if (e.Message.Entities.Length != 2)
            {
                await botClient.SendTextMessageAsync(chatId, 
                    "Please _'Mention'_ a user to use this command \\. Example: /givebadge @user badge name", 
                    parseMode:ParseMode.MarkdownV2);
                return;
            }
            if (e.Message.Text.ToLower().Contains("/givebadge @telebread_bot"))
            {
                await botClient.SendTextMessageAsync(chatId, "You cannot give the bot badges...");
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
                await botClient.SendTextMessageAsync(groupChat, "This user is not in the database yet, or hasn't set their " +
                                                          "group chat. They can add themselves with the \\group command.");
                return;
            }
            if (userId == e.Message.From.Id)
            {
                await botClient.SendTextMessageAsync(groupChat, $"{e.Message.From.FirstName} tried to give themselves a badge. " +
                                                          $"This is a federal crime. The authorities have been contacted.");
                return;
            }

            DataTable dt = cf.RunQuery($"SELECT FirstName FROM dbo.Users WHERE userID = {userId}", new[] {"FirstName"});
            var firstName = dt.Rows[0]["FirstName"].ToString();

            var pId = await botClient.SendPollAsync(chatId, $"Should {firstName} receive the '{badge}' badge?", new []{"Yes", "No"}, isAnonymous:false);
            Poll p = new Poll(long.Parse(pId.Poll.Id), "Badge", chatId, userId, null, badge, Config );
            polls.Add(p);
        }

        public async void grantBadge(ITelegramBotClient botClient, long userId, long chatId, string badge)
        {
            var cf = new CommonFunctions(Config);
            DateTime date = DateTime.Now;
            cf.WriteQuery($"INSERT INTO dbo.Badges (userId, badge, date) VALUES ({userId}, '{badge}', '{date.ToString()}')");
            DataTable dt = cf.RunQuery($"SELECT FirstName from dbo.Users where userID = {userId}", new[] {"FirstName"});
            var FirstName = dt.Rows[0]["FirstName"].ToString();
            await botClient.SendTextMessageAsync(chatId, $"{FirstName} has been granted the {badge} badge!");
        }

        public async void Ponder(ITelegramBotClient botClient, Update e)
        {
            var userId = e.Message.From.Id;
            var cf = new General.CommonFunctions(Config);
            var inv = cf.CheckInventory("Orb", userId);
            if (inv > 0)
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, $"{e.Message.From.FirstName} ponders the orb.");
            }
            else
            {
                await botClient.SendTextMessageAsync(e.Message.Chat.Id, "You do not have an Orb to ponder.");
            }
        }

        public async void Trade(ITelegramBotClient botClient, Update e, List<ChatListener> listeners, List<Trade> trades)
        {
            var cf = new CommonFunctions(Config);
            var groupChat = cf.GetGroupChat(e.Message.From.Id);
            DataTable dt = cf.RunQuery($"SELECT FirstName, UserID FROM dbo.Users WHERE groupChat = {groupChat}",
                new[] {"FirstName", "UserID"});
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            var clearTrades = true;
            while (clearTrades)
            {
                foreach (var trade in trades)
                {
                    if (trade.SenderId == e.Message.From.Id)
                    {
                        trades.Remove(trade);
                        break;
                    }
                }
                clearTrades = false;
            }
            foreach (DataRow row in dt.Rows)
            {
                buttons.Add(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton(row["FirstName"].ToString())
                    {
                        CallbackData = row["UserID"].ToString()
                    }
                });
            }
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Cancel")
                {
                    CallbackData = "Cancel"
                }
            });

            var msgId = botClient.SendTextMessageAsync(cf.GetPrivateChat(e.Message.From.Id), "Select a trading partner:",
                replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;
            
            listeners.Add(new ChatListener(e.Message.From.Id, "Callback", $"Trade,{msgId},{e.Message.Chat.Id}"));
        }

    }
}
