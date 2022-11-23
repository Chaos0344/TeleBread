using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TeleBreadService.General;
using TeleBreadService.Items;
using TeleBreadService.Objects;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Poll = TeleBreadService.General.Poll;

namespace TeleBreadService
{
    public class RunCommand
    {
        private async void delMsg(ITelegramBotClient botClient, long delchat, int oldMsg)
        {
            await botClient.DeleteMessageAsync(delchat, oldMsg);
        }
        
        public RunCommand(ITelegramBotClient botClient, Update e, 
            Dictionary<string, string> config, List<OrbPredictions> predictions, 
            List<ChatListener> listeners, List<Poll> polls, List<Trade> trades)
        {
            var chatId = e.Message.Chat.Id;
            var messageText = e.Message.Text;
            var userId = e.Message.From.Id;
            var c = new Commands(config);
            var cf = new CommonFunctions(config);
            var pf = new PositionFunctions(config);
            var p = predictions;
            int maintenance = 0;
            try
            {
                maintenance = cf.ServiceStatus("Maintenance", 0);
            }
            catch (Exception)
            {
                cf.WriteQuery($"INSERT INTO dbo.SERVICES (groupChat, Service, Status) " +
                                                         $"VALUES (0, 'Maintenance', 0)");
            }
            
            // Check for listeners
            foreach (var listener in listeners)
            {
                if (listener.target == e.Message.From.Id && listener.type == "Text")
                {
                    switch (listener.subtype.Split(',')[0])
                    {
                        case "OrbText":
                            if (e.Message.Text.ToLower().Contains("/use orb"))
                            {
                                break;
                            }
                            OrbPredictions prediction = listener.predictionHolder;
                            prediction.AddText(e.Message.Text);
                            prediction.SaveToDB();
                            predictions.Add(prediction);
                            listeners.Remove(listener);
                            cf.AddToInventory("Orb", -1, e.Message.From.Id);
                            botClient.SendTextMessageAsync(cf.GetGroupChat(e.Message.From.Id),
                                $"{e.Message.From.FirstName} has seen into the future!");
                            break;
                        case "TradeSendQty":
                            var trade = (from Trade x in trades
                                where x.SenderId == e.Message.From.Id
                                select x).First();

                            var senderPrivate = cf.GetPrivateChat(trade.SenderId);

                            // Delete old message because I hate leaving prompts chilling.
                            var subSplit = listener.subtype.Split(',');
                            delMsg(botClient, long.Parse(subSplit[1]), Int32.Parse(subSplit[2]));
                            listeners.Remove(listener);
                            int tradeQty;
                            // Make sure the user is submitting a number.
                            try
                            {
                                tradeQty = Int32.Parse(e.Message.Text);
                            }
                            catch (Exception)
                            {
                                botClient.SendTextMessageAsync(senderPrivate,
                                    "I coudln't parse that into a number. Process canceled.");
                                return;
                            }
                            // Make sure user isn't submitting less than 1
                            if (tradeQty < 1)
                            {
                                botClient.SendTextMessageAsync(senderPrivate,
                                    "You cannot offer to trade less than 1 of any item. Process canceled.");
                                
                                return;
                            }
                            if (tradeQty > trade.receiverInventory[trade.ReceiveItem])
                            {
                                botClient.SendTextMessageAsync(senderPrivate,
                                    "You cannot request more of an item than they have.");
                            }
                            trade.ReceiveQty = tradeQty;
                            var otherPrivate = cf.GetPrivateChat(trade.ReceiverId);
                            var buttons = new List<List<InlineKeyboardButton>>();
                            foreach (var key in trade.senderInventory.Keys)
                            {
                                buttons.Add(new List<InlineKeyboardButton>()
                                {
                                    new InlineKeyboardButton($"{key}. Available: {trade.senderInventory[key]}")
                                    {
                                        CallbackData = key
                                    }
                                });
                            }
                            buttons.Add(new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton("Decline")
                                {
                                    CallbackData = "Decline"
                                }
                            });
                            botClient.SendTextMessageAsync(senderPrivate,
                                $"Sending start of request to {trade.ReceiverName}. " +
                                $"You will have the opportunity to confirm their offer before the trade concludes.");

                            var cbId = botClient.SendTextMessageAsync(otherPrivate,
                                $"You have received a trade request from {trade.SenderName}. They are requesting:\n" +
                                $"{trade.ReceiveItem}. Qty:{trade.ReceiveQty}.\n" +
                                $"You can ask for something in return, or decline the trade.",
                                replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;
                            listeners.Add(new ChatListener(trade.ReceiverId, "Callback", $"Trade3,{cbId},{otherPrivate}"));
                            break;
                        case "TradeRecQty":
                            var trade2 = (from Trade x in trades
                                where x.ReceiverId == e.Message.From.Id
                                select x).First();

                            var receiverPrivate = cf.GetPrivateChat(trade2.ReceiverId);

                            // Delete old message because I hate leaving prompts chilling.
                            var subSplit2 = listener.subtype.Split(',');
                            delMsg(botClient, long.Parse(subSplit2[1]), Int32.Parse(subSplit2[2]));
                            listeners.Remove(listener);
                            int tradeQty2;
                            // Make sure the user is submitting a number.
                            try
                            {
                                tradeQty2 = Int32.Parse(e.Message.Text);
                            }
                            catch (Exception)
                            {
                                botClient.SendTextMessageAsync(receiverPrivate,
                                    "I coudln't parse that into a number. Process canceled.");
                                return;
                            }
                            // Make sure user isn't submitting less than 1
                            if (tradeQty2 < 1)
                            {
                                botClient.SendTextMessageAsync(receiverPrivate,
                                    "You cannot offer to trade less than 1 of any item. Process canceled.");
                                
                                return;
                            }
                            if (tradeQty2 > trade2.senderInventory[trade2.SendItem])
                            {
                                botClient.SendTextMessageAsync(receiverPrivate,
                                    "You cannot request more of an item than they have.");
                            }
                            trade2.SendQty = tradeQty2;
                            var otherPrivate2 = cf.GetPrivateChat(trade2.SenderId);
                            var buttons2 = new List<List<InlineKeyboardButton>>();
                            buttons2.Add(new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton($"Accept")
                                {
                                    CallbackData = "Accept"
                                }
                            });
                            buttons2.Add(new List<InlineKeyboardButton>()
                            {
                                new InlineKeyboardButton("Decline")
                                {
                                    CallbackData = "Decline"
                                }
                            });
                            botClient.SendTextMessageAsync(receiverPrivate,
                                $"Sending your offer reply to {trade2.SenderName}. " +
                                $"You will be informed when the trade is accepted or declined..");

                            var cbId2 = botClient.SendTextMessageAsync(otherPrivate2,
                                $"If you accept the trade you will get {trade2.ReceiveItem} qty: {trade2.ReceiveQty}. " +
                                $"\nYou will trade {trade2.SendItem} qty: {trade2.SendQty}.",
                                replyMarkup: new InlineKeyboardMarkup(buttons2)).Result.MessageId;
                            listeners.Add(new ChatListener(trade2.SenderId, "Callback", $"Trade4,{cbId2},{otherPrivate2}"));
                            break;
                    }

                    return;
                }
            }

            // Out of context commands
            if (messageText != null && messageText.ToLower().Contains("boob"))
            {
                c.Boobs(botClient, e);
            } else if (messageText != null && messageText.ToLower().Contains("69"))
            {
                c.Boobs(botClient, e);
            }

            if (messageText != null && messageText.ToLower().Contains("420"))
            {
                botClient.SendTextMessageAsync(e.Message.Chat.Id, "Blaze it!", replyToMessageId: e.Message.MessageId);
            }

            // Can be used in private chat by Admins
            if (cf.CheckPosition(cf.GetGroupChat(userId), userId, "Admin") 
                && cf.GetPrivateChat(userId) == chatId)
            {
                if (messageText != null && messageText.ToLower().Contains("/say"))
                {
                    c.Say(botClient, e);
                    return;
                }

                if (messageText != null && messageText.ToLower().Contains("/resolve"))
                {
                    c.Resolve(botClient, e);
                    return;
                }
            }

            // Can be used in group chat by Admins
            if (cf.CheckPosition(cf.GetGroupChat(userId), userId, "Admin") 
                && cf.GetGroupChat(userId) == chatId)
            {
                if (messageText != null && messageText.ToLower().Contains("/maintenance"))
                {
                    c.Maintenance(botClient, e);
                    return;
                }
                if (messageText != null && messageText.ToLower().Contains("/test"))
                {
                    // Check wow Online
                    string wowText = cf.queryWow();
                    botClient.SendTextMessageAsync(e.Message.Chat.Id, wowText);
                }

                if (messageText != null && messageText.ToLower().Contains("/silence"))
                {
                    string replaceString = e.Message.Text.Replace("/silence ", "");
                    
                    string outString = "";
                    foreach (var letter in replaceString)
                    {
                        if (letter == ' ')
                        {
                            outString += ' ';
                        } else if (Char.IsPunctuation(letter))
                        {
                            outString += letter;
                        }
                        else if (Char.IsUpper(letter))
                        {
                            outString += 'M';
                        }
                        else
                        {
                            outString += 'm';
                        }
                    }

                    string name = cf.GetFirstName(e.Message.From.Id);
                    botClient.DeleteMessageAsync(e.Message.Chat.Id, e.Message.MessageId);
                    botClient.SendTextMessageAsync(e.Message.Chat.Id,
                        $"{name} tried to speak, but all that came out was \"{outString}\"");
                    return;
                }

                if (messageText != null && messageText.ToLower() == "/odds")
                {
                    c.Odds(botClient, e);
                    return;
                }
            }
            
            // Snap skip
            DataTable snaps = cf.RunQuery(
                $"SELECT groupChat, ExpirationDate FROM dbo.Snaps WHERE groupChat = {chatId} " +
                $"AND ExpirationDate > '{DateTime.Now}'", new[] {"GC", "ED"});
            if (snaps.Rows.Count > 0)
            {
                return;
            }

            // Can be used in group chats by Presidents
            if (cf.CheckPosition(cf.GetGroupChat(userId), userId, "President")
                && cf.GetGroupChat(userId) == chatId)
            {
                if (messageText != null && messageText.ToLower() == "/stimulus")
                {
                    pf.Stimulus(botClient, e);
                }
            }

            // Check for Maintenance Mode
            try
            {
                if (e.Message.Entities != null && maintenance == 1 && e.Message.Entities.Length > 0)
                {
                    foreach (var ent in e.Message.Entities)
                    {
                        if (ent.Type == MessageEntityType.BotCommand)
                        {
                            botClient.SendTextMessageAsync(chatId, "TeleBread is currently under maintenance. " +
                                                                   "We apologize for any inconvenience.");
                        }
                    }

                    return;
                }
            }
            catch (NullReferenceException)
            {
                // No entities, we good.
            }
            
            // Can be used in Private Chats by anyone
            if (cf.GetPrivateChat(userId) == chatId)
            {
                if (messageText != null && messageText.ToLower().Contains("/wow"))
                {
                    var wowtext = cf.queryWow();
                    botClient.SendTextMessageAsync(e.Message.Chat.Id, wowtext);
                }
                
                if (messageText != null && messageText.ToLower().Contains("/trade"))
                {
                    // TODO Check for existing trade and delete it.
                    c.Trade(botClient, e, listeners, trades);
                    return;
                }
            }

            // Can be used in Group Chats by anyone
                if (cf.GetGroupChat(userId) == chatId)
                {
                    if(messageText != null && messageText.ToLower().Contains("/inventory"))
                    {
                        c.Inventory(botClient, e);
                        return;
                    }
                    
                    if (messageText != null && messageText.ToLower().Contains("/wow"))
                    {
                        var wowtext = cf.queryWow();
                        botClient.SendTextMessageAsync(e.Message.Chat.Id, wowtext);
                    }

                    if (messageText != null && messageText.ToLower().Contains("/lick"))
                    {
                        foreach (var pred in p)
                        {
                            if (pred.predictionTarget == e.Message.From.Id && pred.predictionChat == e.Message.Chat.Id)
                            {
                                c.Lick(botClient, e, pred.predictionText);
                                pred.Triggered();
                                p.Remove(pred);
                                return;
                            }
                        }
                        c.Lick(botClient, e);
                        return;
                    }

                    if (messageText != null && messageText.ToLower() == "/snap")
                    {
                        new UseItem(botClient, config).Snap(e);
                    }
                    if (messageText != null && messageText.ToLower().Contains("/use"))
                    {
                        _ = new UseItem(botClient, e, listeners, config);
                        return;
                    }

                    if (messageText != null && messageText.ToLower().Contains("/bread"))
                    {
                        _ = new Bread(botClient, e, chatId, config);
                        return;
                    }

                    if (messageText != null && messageText.ToLower().Contains("/ponder"))
                    {
                        c.Ponder(botClient, e);
                        return;
                    }

                    if (messageText != null && messageText.ToLower().Contains("/givebadge"))
                    {
                        c.GiveBadge(botClient, e, polls);
                    }

                    if (messageText != null && messageText.ToLower() == "/badges")
                    {
                        c.Badges(botClient, e);
                        return;
                    }

                    if (messageText != null && (messageText.ToLower() == "/shop" || messageText.ToLower() == "/store"))
                    {
                        new Bread().shop(botClient, e, config, listeners);
                        return;
                    }
                }

                // Can be used in any chat by anyone
            if (messageText != null && messageText.ToLower() == "/start")
            {
                Commands.Start(botClient, e);
                return;
            } else if (messageText != null && messageText.ToLower() == "/private")
            {
                c.PrivateChat(botClient, e);
                return;
            } else if (messageText != null && messageText.ToLower() == "/group")
            {
                c.GroupChat(botClient, e);
                return;
            } else if (messageText != null && messageText.ToLower().Contains("/support"))
            {
                c.Support(botClient, e);
                return;
            }

            // Add to Timesheet
            if (cf.UserInDatabase(userId))
            {
                int msgs = cf.GetTimesheet(userId, chatId);
                msgs += 1;
                cf.WriteQuery($"UPDATE DBO.Timesheet " +
                           $"SET messages = {msgs} " +
                           $"WHERE userID = {userId} " +
                           $"AND groupChat = {chatId}");

                if (e.Message.Chat.Id == cf.GetGroupChat(userId))
                {
                    var ItemChance = cf.ServiceStatus("ItemChance", e.Message.Chat.Id);
                    var rand = new Random();
                    int findItem = rand.Next(1, ItemChance);
                    if (findItem == 1)
                    {
                        var whatItem = rand.Next(1, 5);
                        cf.WriteQuery($"UPDATE dbo.Services SET Status = 1000 " +
                                      $"WHERE Service = 'ItemChance' AND groupChat = {chatId}");
                        var dt = cf.RunQuery($"SELECT ItemName from dbo.Items where ItemID = " +
                                             $"{whatItem}", new []{"ItemName"});
                        var item = dt.Rows[0]["ItemName"].ToString();
                        botClient.SendTextMessageAsync(chatId,
                            $"{e.Message.From.FirstName} found a(n) {item}. How lucky!");
                        cf.AddToInventory(item, 1, userId);
                    }
                    else
                    {
                        ItemChance--;
                        cf.WriteQuery($"UPDATE dbo.Services SET Status = {ItemChance} " +
                                      $"WHERE Service = 'ItemChance' AND groupChat = {chatId}");
                    }
                }
            }
        }
    }
}
