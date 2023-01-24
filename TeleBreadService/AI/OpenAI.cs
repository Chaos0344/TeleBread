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

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.openai.com/v1/completions");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var jsonData = new jsonThing();
            
            if (SmartOrDumb == "Smart"){
                jsonData = new jsonThing()
                {
                    model = "text-davinci-003",
                    prompt = query,
                    max_tokens = 100,
                    temperature = .9,
                    top_p = 1,
                    n = 1,
                    stream = false
                };
            }
            else if(SmartOrDumb == "Dumb")
            {
                jsonData = new jsonThing()
                {
                    model = "text-ada-001",
                    prompt = query,
                    max_tokens = 100,
                    temperature = .9,
                    top_p = 1,
                    n = 1,
                    stream = false
                };
            }
                    

            var result = client.PostAsync("", new StringContent(JsonConvert.SerializeObject(jsonData), System.Text.Encoding.UTF8, "application/json")).Result;
            JObject joResponse = JObject.Parse(result.Content.ReadAsStringAsync().Result);
            JObject joArray = (JObject) joResponse["choices"][0];
            var outtext = joArray["text"].ToString();
            Console.WriteLine(result.Content.ReadAsStringAsync().Result);
            Console.WriteLine(outtext);
            botClient.SendTextMessageAsync(update.Message.Chat.Id, outtext);
        }
    }

    class jsonThing
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public int max_tokens { get; set; }
        public double temperature { get; set; }
        public int top_p { get; set; }
        public int n { get; set; }
        public bool stream { get; set; }
    }
}