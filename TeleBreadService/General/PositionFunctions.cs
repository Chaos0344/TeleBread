using System.Collections.Generic;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBreadService.General
{
    public class PositionFunctions
    {
        private Dictionary<string, string> Config { get; set; }
        private CommonFunctions cf { get; set; }

        public PositionFunctions(Dictionary<string, string> config)
        {
            Config = config;
            cf = new CommonFunctions(Config);
        }

        public void Stimulus(ITelegramBotClient botClient, Update e)
        {
            List<string> users = new List<string>();
            DataTable dt = cf.RunQuery($"SELECT userID from dbo.Users WHERE groupChat = {e.Message.Chat.Id}",
                new[] {"userID"});
            foreach (DataRow row in dt.Rows)
            {
                long userId = long.Parse(row["userID"].ToString());
                cf.AddToInventory("Bread", 2, userId);
            }

            botClient.SendTextMessageAsync(e.Message.Chat.Id, "Let the stimulation begin.");
        }
    }
}