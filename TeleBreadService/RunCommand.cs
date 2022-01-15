using System;
using System.Collections.Generic;
using System.Linq;
using TeleBreadService.General;
using TeleBreadService.Items;
using TeleBreadService.Objects;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
            Dictionary<string, string> config, List<OrbPredictions> predictions, List<ChatListener> listeners, List<Poll> polls, List<Trade> trades)
        {
            var chatId = e.Message.Chat.Id;
            var messageText = e.Message.Text;
            var userId = e.Message.From.Id;
            var c = new Commands(config);
            var cf = new CommonFunctions(config);
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
                            // TODO Handle qty message
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
                                    "I coudln't process that into a number. Process canceled.");
                                return;
                            }
                            
                            // Make sure user isn't submitting less than 1
                            if (tradeQty < 1)
                            {
                                botClient.SendTextMessageAsync(senderPrivate,
                                    "You cannot offer to trade less than 1 of any item. Process canceled.");
                                
                                return;
                            }

                            trade.SendQty = tradeQty;

                            var otherPrivate = cf.GetPrivateChat(trade.ReceiverId);
                            

                            botClient.SendTextMessageAsync(senderPrivate,
                                "Sending start of offer to other party. You will have the opportunity to confirm their offer before the trade concludes.");
                            
                            botClient.SendTextMessageAsync(otherPrivate, "")
                            
                            var senderId = listener.target;
                            var receiverId = long.Parse(listener.subtype.Split(',')[1]);
                            break;
                    }

                    return;
                }
            }

            // Out of context commands
            if (messageText != null && messageText.ToLower().Contains("boobs"))
            {
                c.Boobs(botClient, e);

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
                    //_ = new Payroll(botClient, config);
                    //cf.AddToInventory("Orb", 1, e.Message.From.Id);
                    new Bread().shop(botClient, e, config, listeners);
                    return;
                }
            }

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
                if (messageText != null && messageText.ToLower().Contains("/trade"))
                {
                    // TODO Check for existing trade and delete it.
                    c.Trade(botClient, e, listeners);
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
            }

            if (cf.UserInDatabase(userId))
            {
                int msgs = cf.GetTimesheet(userId, chatId);
                msgs += 1;
                cf.WriteQuery($"UPDATE DBO.Timesheet " +
                           $"SET messages = {msgs} " +
                           $"WHERE userID = {userId} " +
                           $"AND groupChat = {chatId}");
            }
        }
    }
}
