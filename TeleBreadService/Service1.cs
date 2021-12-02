using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.IO;
using System.Net;
using System.Timers;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using TeleBreadService.General;

namespace TeleBreadService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }
        private static string local = "/Users/blakeroetzel/Library/Mobile Documents/com~apple~CloudDocs/Dev/TeleBread/.local/";
        //static string local = "C:/Dev/TeleBread/.local/config.conf";

        private static Dictionary<string, string> config = new Dictionary<string, string>();
        private string Dbuser { get; set; }
        private string Dbpass { get; set; }
        private string Dbserver { get; set; }
        private string ApiKey { get; set; }

        private static ITelegramBotClient botClient;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        
        

        /// <summary>
        /// Reads the config file in the local path (Not tracked in git)
        /// </summary>
        public void readConfig()
        {
            using (StreamReader sr = new StreamReader(local + "config.conf"))
            {
                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine();
                    var linesplit = line.Split('=');
                    config[linesplit[0]] = linesplit[1];
                }
            }
            Dbuser = config["dbuser"];
            Dbpass = config["dbpassword"];
            Dbserver = config["dbserver"];
            ApiKey = config["apiKey"];
        }


        public async void runServer()
        {
            readConfig();
            botClient = new TelegramBotClient(ApiKey);
            

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };
            botClient.StartReceiving(HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);
            
        }
        
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;
            // Only process text messages
            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            // Handle Message Type Updates

            if (update.Message!.Type == MessageType.Text)
            {

                if (messageText.ToLower() == "/start")
                {
                    new Commands().start(botClient, update);
                    return;
                }
            }
        }
        
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public void OnDebug()
        {
            runServer();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Bot Starting");
            runServer();
        }

        protected override void OnStop()
        {
            cts.Cancel();
            WriteToFile("Bot Stopping.");
        }

        public void WriteToFile(string Message)
        {
            Message = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " " + Message;
            string path = local;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string filepath = local+"TemepromptBot.txt";
            if (!System.IO.File.Exists(filepath))
            {
                using (StreamWriter sw = System.IO.File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
