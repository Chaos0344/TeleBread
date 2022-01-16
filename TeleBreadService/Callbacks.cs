using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Configuration;
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
                case "Trade3":
                    Trade3(listener, listeners, trades);
                    break;
                case "Trade4":
                    Trade4(listener, listeners, trades);
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
            if (e.CallbackQuery.Data == "Cancel")
            {
                return;
            }
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
            listeners.Remove(listener);
            if (e.CallbackQuery.Data == "Cancel")
            {
                return;
            }
            ChatListener newListener =
                new ChatListener(e.CallbackQuery.From.Id, "Text", "OrbText");
            newListener.predictionHolder = new OrbPredictions(
                long.Parse(e.CallbackQuery.Data), 
                chat: new CommonFunctions(config).GetGroupChat(e.CallbackQuery.From.Id), 
                config);
            listeners.Add(newListener);
            var chat = new CommonFunctions(config).GetPrivateChat(e.CallbackQuery.From.Id);
            botClient.SendTextMessageAsync(chat, "Please send a message " +
                                                 "containing the item that your target will lick next.");
        }

        private void Trade(ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            var senderId = listener.target;
            var receiverId = long.Parse(e.CallbackQuery.Data);
            var trade = new General.Trade(config, senderId, receiverId);
            if (e.CallbackQuery.Data == "Cancel")
            {
                listeners.Remove(listener);
                return;
            }

            if (new CommonFunctions(config).GetPrivateChat(receiverId) == 0)
            {
                listeners.Remove(listener);
                botClient.SendTextMessageAsync(new CommonFunctions(config).GetPrivateChat(senderId),
                    "Your selected partner does not have a private chat configured. " +
                    "Please reach out to them to create one and use the /private command.");
                return;
            }
            
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();
            
            foreach (var key in trade.receiverInventory.Keys)
            {
                buttons.Add(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton($"{key}. Available: {trade.receiverInventory[key]}")
                    {
                        CallbackData = key
                    }
                });
            }
            buttons.Add(new List<InlineKeyboardButton>()
            {
                new InlineKeyboardButton("Cancel")
                {
                    CallbackData = "Cancel"
                }
            });
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
            
            
            foreach (var trade in trades)
            {
                if (trade.SenderId == e.CallbackQuery.From.Id)
                {
                    if (e.CallbackQuery.Data == "Cancel")
                    {
                        listeners.Remove(listener);
                        trades.Remove(trade);
                        return;
                    }
                    var privateChat = new CommonFunctions(config).GetPrivateChat(e.CallbackQuery.From.Id);
                    trade.ReceiveItem = e.CallbackQuery.Data;
                    listeners.Remove(listener);
                    var mId = botClient
                        .SendTextMessageAsync(privateChat, $"Please reply with the Quantity of {trade.ReceiveItem} you would like to receive. (Max: {trade.receiverInventory[trade.ReceiveItem]})")
                        .Result.MessageId;
                    listeners.Add(
                        new ChatListener(e.CallbackQuery.From.Id, "Text", $"TradeSendQty,{privateChat},{mId}"));
                }
            }
            
        }

        private void Trade3(ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            foreach (var trade in trades)
            {
                if (trade.ReceiverId == e.CallbackQuery.From.Id)
                {
                    var privateChat = new CommonFunctions(config).GetPrivateChat(e.CallbackQuery.From.Id);
                    var otherPrivate = new CommonFunctions(config).GetPrivateChat(trade.SenderId);
                    if (e.CallbackQuery.Data == "Decline")
                    {
                        listeners.Remove(listener);
                        trades.Remove(trade);
                        botClient.SendTextMessageAsync(privateChat, "You have declined the trade.");
                        botClient.SendTextMessageAsync(otherPrivate, $"{trade.SenderName} has declined the trade.");
                        return;
                    }
                    trade.SendItem = e.CallbackQuery.Data;
                    listeners.Remove(listener);
                    var mId = botClient
                        .SendTextMessageAsync(privateChat, $"Please reply with the Quantity of {trade.SendItem} you would like to receive. (Max: {trade.senderInventory[trade.SendItem]})")
                        .Result.MessageId;
                    listeners.Add(
                        new ChatListener(e.CallbackQuery.From.Id, "Text", $"TradeRecQty,{privateChat},{mId}"));
                }
            }
        }

        private void Trade4(ChatListener listener, List<ChatListener> listeners, List<Trade> trades)
        {
            foreach (var trade in trades)
            {
                if (trade.SenderId == e.CallbackQuery.From.Id)
                {
                    var cf = new CommonFunctions(config);
                    if (e.CallbackQuery.Data == "Decline")
                    {
                        var senderPrivate = cf.GetPrivateChat(trade.SenderId);
                        var receiverPrivate = cf.GetPrivateChat(trade.ReceiverId);
                        botClient.SendTextMessageAsync(senderPrivate, "You have declined the trade.");
                        botClient.SendTextMessageAsync(receiverPrivate,
                            $"The trade was declined by {trade.SenderName}");
                        listeners.Remove(listener);
                        trades.Remove(trade);
                        return;
                    } else if (e.CallbackQuery.Data == "Accept")
                    {
                        trade.completeTrade(botClient);
                        listeners.Remove(listener);
                        trades.Remove(trade);
                        return;
                    }
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