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
            botClient.SendTextMessageAsync(e.Message.Chat.Id,
                "Hello! TeleBread is a bot primarily made for goofing around with friends in a group chat!\n" +
                "Please visit telebread.net if you want additional information about our bot!");
            botClient.SendTextMessageAsync(e.Message.Chat.Id,
                "If you already know how to use this bot and wish to proceed, please use /group to link this as " +
                "your group chat, or /private to link it as your private chat.");
        }
    }
}
