using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using TeleBreadService.General;
using TeleBreadService.Objects;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TeleBreadService
{
    public class Callbacks
    {
        private Dictionary<string, string> config { get; set; }
        private ITelegramBotClient botClient { get; set; }
        private Update e { get; set; }
        private CommonFunctions cf { get; set; }

        public Callbacks(ITelegramBotClient bC, Update u, Dictionary<string, string> c, ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            config = c;
            botClient = bC;
            e = u;
            var listSplit = listener.subtype.Split(',');
            var st = listSplit[0];
            var oldMsg = Int32.Parse(listSplit[1]);
            var delChat = long.Parse(listSplit[2]);
            cf = new General.CommonFunctions(config);
            delMsg(delChat, oldMsg);
            if (e.CallbackQuery.Data == "Cancel")
            {
                return;
            }
            switch (st)
            {
                case "Shop":
                    Shop(listener, listeners);
                    break;
                case "OrbTarget":
                    Orb(listener,listeners);
                    break;
                case "Trade":
                    Trade(listener,listeners, trades);
                    break;
                case "Trade2":
                    Trade2(listener, listeners, trades);
                    break;
            }
        }

        private async void delMsg(long delchat, int oldMsg)
        {
            await botClient.DeleteMessageAsync(delchat, oldMsg);
        }

        private void Shop(ChatListener listener, List<ChatListener> listeners)
        {
            listeners.Remove(listener);
            var userId = listener.target;
            var chatId = cf.GetGroupChat(userId);
            int breadQty = cf.CheckInventory("Bread", userId);
            int price = 0;
            string item = "";
            switch (e.CallbackQuery.Data)
            {
                case "CatPic":
                    price = 1;
                    break;
                case "DogPic":
                    price = 1;
                    break;
                case "CatGif":
                    price = 3;
                    break;
                case "DogGif":
                    price = 3;
                    break;
                case "Orb":
                    price = 5;
                    item = "Orb";
                    break;
                case "InfGaunt":
                    price = 10;
                    item = "Infinity Gauntlet";
                    break;
                case "Ring":
                    price = 10;
                    item = "Ring";
                    break;
                case "Purge":
                    price = 15;
                    item = "Purgestone";
                    break;
            }

            if (breadQty < price)
            {
                botClient.SendTextMessageAsync(chatId, "You cannot afford that.");
                return;
            }

            if (item != "")
            {
                cf.AddToInventory(item, 1, userId);
                cf.AddToInventory("Bread", -price, userId);
                botClient.SendTextMessageAsync(chatId, $"{e.CallbackQuery.From.FirstName} has bought a(n) {item}.");
                return;
            } 
            
            cf.AddToInventory("Bread", -price, userId);
            switch (e.CallbackQuery.Data)
            {
                case "CatPic":
                    sendCat(chatId, "img");
                    break;
                case "CatGif":
                    sendCat(chatId, "gif");
                    break;
                case "DogPic":
                    sendDog(chatId, "img");
                    break;
                case "DogGif":
                    sendDog(chatId, "gif");
                    break;
            }

        }
        

        private void Orb(ChatListener listener, List<ChatListener> listeners) {
            if (e.CallbackQuery.Data == "Cancel")
            {
                listeners.Remove(listener);
                return;
            }

            ChatListener newListener =
                new ChatListener(e.CallbackQuery.From.Id, "Text", "OrbText");
            newListener.predictionHolder = new OrbPredictions(
                long.Parse(e.CallbackQuery.Data), 
                chat: new CommonFunctions(config).GetGroupChat(e.CallbackQuery.From.Id), 
                config);
            listeners.Add(newListener);
            listeners.Remove(listener);
            var chat = new CommonFunctions(config).GetPrivateChat(e.CallbackQuery.From.Id);
            botClient.SendTextMessageAsync(chat, "Please send a message " +
                                                 "containing the item that your target will lick next.");
        }

        private void Trade(ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            if (e.CallbackQuery.Data == "Cancel")
            {
                listeners.Remove(listener);
                return;
            }

            var senderId = listener.target;
            var receiverId = long.Parse(e.CallbackQuery.Data);

            DataTable dt = new CommonFunctions(config).RunQuery(
                $"SELECT b.ItemName, a.quantity FROM Inventory a JOIN Items b ON a.ItemID = b.ItemID WHERE a.userID = {receiverId}",
                new[] {"ItemName", "Qty"});
            
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            Dictionary<string, int> receiverInv = new Dictionary<string, int>();
            
            foreach (DataRow row in dt.Rows)
            {
                buttons.Add(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton($"{row["ItemName"]}. Available: {row["Qty"]}")
                    {
                        CallbackData = row["ItemName"].ToString()
                    }
                });
                receiverInv[row["ItemName"].ToString()] = int.Parse(row["Qty"].ToString());
            }
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Cancel")
                {
                    CallbackData = "Cancel"
                }
            });
            var trade = new General.Trade(config, senderId, receiverId);
            trade.SenderName = cf.GetFirstName(senderId);
            trade.ReceiverName = cf.GetFirstName(receiverId);
            trade.receiverInventory = receiverInv;
            trades.Add(trade);
            var chat = new CommonFunctions(config).GetPrivateChat(senderId);

            var oldMsg = botClient.SendTextMessageAsync(chat,
                $"Please select what you want from {trade.ReceiverName}.", replyMarkup: new InlineKeyboardMarkup(buttons)).Result.MessageId;
            ChatListener newListener = new ChatListener(senderId, "Callback", $"Trade2,{oldMsg},{chat}");
            listeners.Add(newListener);
            listeners.Remove(listener);

        }

        private void Trade2(ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            if (e.CallbackQuery.Data == "Cancel")
            {
                listeners.Remove(listener);
                return;
            }

            foreach (var trade in trades)
            {
                if (trade.SenderId == e.CallbackQuery.From.Id)
                {
                    var privateChat = new CommonFunctions(config).GetPrivateChat(e.CallbackQuery.From.Id);
                    trade.ReceiveItem = e.CallbackQuery.Data;
                    listeners.Remove(listener);
                    var mId = botClient
                        .SendTextMessageAsync(privateChat, $"Please reply with the Quantity of {trade.SendItem} you would like to receive. (Max: {trade.senderInventory[trade.ReceiveItem]})")
                        .Result.MessageId;
                    listeners.Add(
                        new ChatListener(e.CallbackQuery.From.Id, "Text", $"TradeSendQty,{privateChat},{mId}"));
                }
            }
            
        }
        
        public async void  sendCat(long chatId, string imgType)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.thecatapi.com");
                client.DefaultRequestHeaders.Add("x-api-key", config["theCatAPI"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string url = "";
                switch (imgType)
                {
                    case "img":
                        url = "v1/images/search?mime_types=jpg,png";
                        break;
                    case "gif":
                        url = "v1/images/search?mime_types=gif";
                        break;
                    default:
                        url = "";
                        break;
                }
                HttpResponseMessage response = await client.GetAsync(url);
                var resp = await response.Content.ReadAsStringAsync();
                resp = resp.Substring(1, resp.Length - 2);
                var values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp);

                switch (imgType)
                {
                    case "img":
                        TelegramBotClientExtensions.SendPhotoAsync(botClient, chatId, photo: values["url"], disableNotification:true);
                        return;
                    case "gif":
                        await botClient.SendAnimationAsync(chatId: chatId, new InputOnlineFile(values["url"]), disableNotification: true);
                        return;
                    default:
                        return;
                }
            }
        }

        public async void sendDog(long chatID, string imgType)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.thedogapi.com");
                client.DefaultRequestHeaders.Add("x-api-key", config["theDogAPI"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string url = "";
                switch (imgType)
                {
                    case "img":
                        url = "v1/images/search?mime_types=jpg,png";
                        break;
                    case "gif":
                        url = "v1/images/search?mime_types=gif";
                        break;
                    default:
                        url = "";
                        break;
                }
                HttpResponseMessage response = await client.GetAsync(url);
                var resp = await response.Content.ReadAsStringAsync();
                resp = resp.Substring(1, resp.Length - 2);
                var values = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp);

                switch (imgType)
                {
                    case "img":
                        await TelegramBotClientExtensions.SendPhotoAsync(botClient, chatId: chatID, photo: values["url"], disableNotification: true);
                        return;
                    case "gif":
                        await botClient.SendAnimationAsync(chatId: chatID, new InputOnlineFile(values["url"]), disableNotification: true);
                        return;
                    default:
                        return;
                }
            }
        }
            
    }
}