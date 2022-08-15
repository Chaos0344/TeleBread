using System;
using System.Collections.Generic;
using System.Data;
using TeleBreadService;
using TeleBreadService.General;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBreadService.Objects
{
    public class Purgestone
    {
        public long userID { get; set; }
        public long groupChatID { get; set; }
        public long privateChatId { get; set; }
        public Dictionary<string, string> config { get; set; }
        private CommonFunctions cf { get; set; }

        public Purgestone(long uid, Dictionary<string, string> c)
        {
            config = c;
            cf = new CommonFunctions(config);
            userID = uid;
            groupChatID = cf.GetGroupChat(userID);
            privateChatId = cf.GetPrivateChat(userID);

        }

        public void listBadges(ITelegramBotClient botClient, Update e, List<ChatListener> listeners)
        {
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            DataTable dt = cf.RunQuery($"SELECT badge FROM Badges WHERE userID = {userID}", new[] {"badge"});
            foreach (DataRow row in dt.Rows)
            {
                buttons.Add(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton(row["badge"].ToString())
                    {
                        CallbackData = row["badge"].ToString()
                    }
                });
            }
            
            var msgId = botClient.SendTextMessageAsync(privateChatId, "Select the badge you want to remove:",
                replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;

            var pvt = cf.GetPrivateChat(e.Message.From.Id);
            
            listeners.Add(new ChatListener(userID, "Callback", $"Purgestone,{msgId},{pvt}"));
        }
        
        
        
    }
}