using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TeleBreadService.AI
{
    public class OpenAI
    {
        
        private Dictionary<string, string> config { get; set; }
        
        private ITelegramBotClient botClient { get; set; }

        public OpenAI(ITelegramBotClient bC, Dictionary<string, string> c, string query, Update update, string SmartOrDumb)
        {
            botClient = bC;
            config = c;

            string apiKey = config["openAIAPI"];
            string uriString = "";

            string sObject = "";


            if (SmartOrDumb == "Smart"){
                uriString = "https://api.openai.com/v1/chat/completions";
                var jsonData = new jsonThing2()
                {

                    model = "gpt-3.5-turbo",
                    messages = new List<Dictionary<string, string>>(){
                        new Dictionary<string, string>() {
                            { "role", "system" },
                            {"content", "You are TelebreadBot, a helpful and sassy assistant."}
                        },
                        new Dictionary<string, string>() {
                            { "role", "user" },
                            {"content", query }
                        }
                    },
                    max_tokens = 3500,
                    temperature = .9,
                    top_p = 1,
                    n = 1,
                    stream = false
                };
                sObject = JsonConvert.SerializeObject(jsonData);
            }
            else if(SmartOrDumb == "Dumb")
            {
                uriString = "https://api.openai.com/v1/completions";
                var jsonData = new jsonThing()
                {

                    model = "text-ada-001",
                    prompt = query,
                    max_tokens = 300,
                    temperature = .9,
                    top_p = .1,
                    n = 1,
                    stream = false
                };
                sObject = JsonConvert.SerializeObject(jsonData);
            }

            var client = new HttpClient();
            client.BaseAddress = new Uri(uriString);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            var result = client.PostAsync("", new StringContent(sObject, System.Text.Encoding.UTF8, "application/json")).Result;
            JObject joResponse = JObject.Parse(result.Content.ReadAsStringAsync().Result);
            string outtext = "";
            try
            {
                JObject joArray = (JObject)joResponse["choices"][0];
                if (SmartOrDumb == "Smart")
                {
                    outtext = joArray["message"]["content"].ToString();
                }
                else
                {
                    outtext = joArray["text"].ToString();
                }
            } catch
            {
                botClient.SendTextMessageAsync(update.Message.Chat.Id, "Recieved error from GPT", replyToMessageId: update.Message.MessageId);
                return;
            }
            
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);
            Console.WriteLine(outtext);
            botClient.SendTextMessageAsync(update.Message.Chat.Id, outtext, replyToMessageId:update.Message.MessageId);
        }
    }

    class jsonThing
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public int max_tokens { get; set; }
        public double temperature { get; set; }
        public double top_p { get; set; }
        public int n { get; set; }
        public bool stream { get; set; }
    }

    class jsonThing2
    {
        public string model { get; set; }
        public List<Dictionary<string,string>> messages { get; set; }
        public int max_tokens { get; set; }
        public double temperature { get; set; }
        public double top_p { get; set; }
        public int n { get; set; }
        public bool stream { get; set; }
    }
}