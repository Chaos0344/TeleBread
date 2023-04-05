using System;
using System.Collections.Generic;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;

namespace TeleBreadService.AI
{
    public class ImageGen
    {
        private Dictionary<string, string> config { get; set; }
        
        private ITelegramBotClient botClient { get; set; }

        public ImageGen(ITelegramBotClient bC, Dictionary<string, string> c, string query, Update update)
        {
            botClient = bC;
            config = c;
            string model = "";

            if (query.ToLower().Contains("anime"))
            {
                model = "wd-v1-3-full,";
            }
            //else if (query.ToLower().Contains("man") || query.ToLower().Contains("woman") || query.ToLower().Contains("boy") || query.ToLower().Contains("girl"))
            //{
                //model = "realisticVisionV12_v12,";
            //}
            else
            {
                model = "mdjrny-v4,";
                //model = "stable-diffusion-1.5,";
            }

            botClient.SendTextMessageAsync(update.Message.Chat.Id, "Working on it...");

            try
            {
                using TcpClient client = new TcpClient("10.0.20.50", 8992);
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(model + query);
                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 300000;

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", query);

                // Receive the server response.

                // Buffer to store the response bytes.
                data = new Byte[1024];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                if (responseData.Contains("SIZE"))
                {
                    data = System.Text.Encoding.ASCII.GetBytes("GOT SIZE");
                    stream.Write(data, 0, data.Length);
                }
                else if (responseData.Contains("Error"))
                {
                    botClient.SendTextMessageAsync(update.Message.Chat.Id, "Error, bad input");
                    return;
                }
                else
                {
                    botClient.SendTextMessageAsync(update.Message.Chat.Id, "Error, Unknown");
                    return;
                }

                var size = Int32.Parse(responseData.Replace("SIZE ", ""));
                data = new Byte[size];
                bytes = stream.Read(data, 0, size);
                while (bytes != size)
                {
                    Console.WriteLine($"Recieved {bytes}");
                    bytes += stream.Read(data, bytes, size - bytes);
                }
                Console.WriteLine($"GOT DATA");

                File.WriteAllBytes("C:/dev/TeleBread/.local/bot.png", data);

                Console.WriteLine("Data should be written");

                Stream filePath = System.IO.File.Open(@"C:/dev/TeleBread/.local/bot.png", FileMode.Open);
                botClient.SendPhotoAsync(update.Message.Chat.Id,
                    new InputOnlineFile(filePath, "bot.png"));
            }
            catch
            {
                botClient.SendTextMessageAsync(update.Message.Chat.Id, "Request has timed out");
            }
        }
        
        
    }
}