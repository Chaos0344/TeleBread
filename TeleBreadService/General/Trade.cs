using System;
using System.Collections.Generic;
using System.Data;
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
            var cf = new CommonFunctions(Config);

            SenderName = cf.GetFirstName(SenderId);
            ReceiverName = cf.GetFirstName(ReceiverId);

            senderInventory = new Dictionary<string, int>();
            receiverInventory = new Dictionary<string, int>();

            DataTable dt =
                cf.RunQuery(
                    $"SELECT b.ItemName, a.quantity " +
                    $"FROM Inventory a JOIN Items b ON a.ItemID = b.ItemID WHERE a.userID = {SenderId}",
                    new[] {"ItemName", "Qty"});
            foreach (DataRow row in dt.Rows)
            {
                senderInventory[row["ItemName"].ToString()] = Int32.Parse(row["Qty"].ToString());
            }
            DataTable dr =
                cf.RunQuery(
                    $"SELECT b.ItemName, a.quantity " +
                    $"FROM Inventory a JOIN Items b ON a.ItemID = b.ItemID WHERE a.userID = {ReceiverId}",
                    new[] {"ItemName", "Qty"});
            foreach (DataRow row in dr.Rows)
            {
                receiverInventory[row["ItemName"].ToString()] = Int32.Parse(row["Qty"].ToString());
            }
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
            botClient.SendTextMessageAsync(senderChat, $"Trade accepted and completed! You have received {ReceiveQty} {ReceiveItem}.");
            botClient.SendTextMessageAsync(receiverChat, $"Trade accepted and completed! You have received {SendQty} {SendItem}.");
        }
        
    }
}