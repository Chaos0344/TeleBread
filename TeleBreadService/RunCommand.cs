using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

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

            // Out of context commands
            if (messageText.ToLower().Contains("boobs"))
            {
                c.boobs(botClient, e);

            }

            // Can be used in private chat by Admins
            if (cf.checkPosition(cf.getGroupChat(userId,config), userId, "Admin", config) && cf.getPrivateChat(userId, config) == chatId)
            {
                if (messageText.ToLower().Contains("/say"))
                {
                    c.say(botClient, e, config);
                    return;
                }
            }

            // Can be used in group chat by Admins
            if (cf.checkPosition(cf.getGroupChat(userId, config), userId, "Admin", config) && cf.getGroupChat(userId, config) == chatId)
            {
                if (messageText.ToLower().Contains("/test"))
                {
                    new General.Bread().bread(botClient, e, chatId, config);
                    return;
                }
            }

                // Cab be used in Group Chats by anyone
                if (cf.getGroupChat(userId, config) == chatId && messageText.ToLower().Contains("/inventory"))
            {
                c.inventory(botClient, e, config);
                return;
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

        }
    }
}
