using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleBreadService.General;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBreadService
{
    public class RunCommand
    {
        public RunCommand(ITelegramBotClient botClient, Update e, Dictionary<string, string> config)
        {
            var chatId = e.Message.Chat.Id;
            var messageText = e.Message.Text;
            var userId = e.Message.From.Id;
            var c = new Commands();
            var cf = new General.CommonFunctions();
            int maintenance = 0;
            try
            {
                maintenance = new General.CommonFunctions().serviceStatus("Maintenance", 0, config);
            }
            catch (Exception z)
            {
                new General.CommonFunctions().writeQuery($"INSERT INTO dbo.SERVICES (groupChat, Service, Status) " +
                                                         $"VALUES (0, 'Maintenance', 0)", config);
            }

            // Out of context commands
            if (messageText.ToLower().Contains("boobs"))
            {
                c.boobs(botClient, e);

            }

            // Can be used in private chat by Admins
            if (cf.checkPosition(cf.getGroupChat(userId,config), userId, "Admin", config) 
                && cf.getPrivateChat(userId, config) == chatId)
            {
                if (messageText.ToLower().Contains("/say"))
                {
                    c.say(botClient, e, config);
                    return;
                }
            }

            // Can be used in group chat by Admins
            if (cf.checkPosition(cf.getGroupChat(userId, config), userId, "Admin", config) 
                && cf.getGroupChat(userId, config) == chatId)
            {
                if (messageText.ToLower().Contains("/maintenance"))
                {
                    new Commands().maintenance(botClient, e, config);
                    return;
                }
                if (messageText.ToLower().Contains("/test"))
                {
                    _ = new Payroll(botClient, config);
                    return;
                }
            }

            try
            {
                if (maintenance == 1 && e.Message.Entities.Length > 0)
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
            catch (NullReferenceException z)
            {
                // No entities, we good.
            }

            // Cab be used in Group Chats by anyone
                if (cf.getGroupChat(userId, config) == chatId)
                {
                    if(messageText.ToLower().Contains("/inventory"))
                    {
                        c.inventory(botClient, e, config);
                        return;
                    }

                    if (messageText.ToLower().Contains("/lick"))
                    {
                        c.lick(botClient, e, config);
                        return;
                    }

                    if (messageText.ToLower().Contains("/bread"))
                    {
                        new Bread().bread(botClient, e, chatId, config);
                        return;
                    }
                }

                // Can be used in any chat by anyone
            if (messageText.ToLower() == "/start")
            {
                c.start(botClient, e);
                return;
            } else if (messageText.ToLower() == "/private")
            {
                c.privateChat(botClient, e, config);
                return;
            } else if (messageText.ToLower() == "/group")
            {
                c.groupChat(botClient, e, config);
                return;
            }

            if (cf.userInDatabase(userId, config))
            {
                int msgs = cf.getTimesheet(userId, chatId, config);
                msgs += 1;
                cf.writeQuery($"UPDATE DBO.Timesheet " +
                           $"SET messages = {msgs} " +
                           $"WHERE userID = {userId} " +
                           $"AND groupChat = {chatId}", config);
            }
        }
    }
}
