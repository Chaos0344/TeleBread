using System.Collections.Generic;
using Telegram.Bot;

namespace TeleBreadService.General
{
    public class Trade
    {
        public long SenderId { get; set; }
        public long ReceiverId { get; set; }
        public string SendItem { get; set; }
        public int SendQty { get; set; }
        public string ReceiveItem { get; set; }
        public int ReceiveQty { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        private Dictionary<string, string> Config { get; set; }
        public Dictionary<string, int> senderInventory { get; set; }
        public Dictionary<string, int> receiverInventory { get; set; }


        public Trade(Dictionary<string, string> config, long sender, long receiver)
        {
            Config = config;
            SenderId = sender;
            ReceiverId = receiver;
        }

        public void SetSendItem(string item)
        {
            SendItem = item;
        }

        public bool SetSendQty(int qty)
        {
            int inventory = new CommonFunctions(Config).CheckInventory(SendItem, SenderId);
            if (inventory >= qty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetReceiveItem(string item)
        {
            ReceiveItem = item;
        }

        public bool SetReceiveQty(int qty)
        {
            int inventory = new CommonFunctions(Config).CheckInventory(ReceiveItem, ReceiverId);
            if (inventory >= qty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void completeTrade(ITelegramBotClient botClient)
        {
            var cf = new CommonFunctions(Config);
            cf.AddToInventory(SendItem, -SendQty, SenderId);
            cf.AddToInventory(SendItem, SendQty, ReceiverId);
            cf.AddToInventory(ReceiveItem, ReceiveQty, SenderId);
            cf.AddToInventory(ReceiveItem, -ReceiveQty, ReceiverId);

            var senderChat = cf.GetPrivateChat(SenderId);
            var receiverChat = cf.GetPrivateChat(ReceiverId);
            botClient.SendTextMessageAsync(senderChat, "Trade accepted and completed!");
            botClient.SendTextMessageAsync(receiverChat, "Trade accepted and completed!");
        }
        
    }
}