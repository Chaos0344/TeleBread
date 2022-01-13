using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using TeleBreadService.General;
using TeleBreadService.Objects;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace TeleBreadService
{
    public class Callbacks
    {
        private Dictionary<string, string> config { get; set; }
        private ITelegramBotClient botClient { get; set; }
        private Update e { get; set; }
        private CommonFunctions cf { get; set; }

        public Callbacks(ITelegramBotClient bC, Update u, Dictionary<string, string> c, ChatListener listener, List<ChatListener> listeners)
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