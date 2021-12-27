using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.IO;
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
        private static string _local = "/Users/blakeroetzel/Documents/Dev/TeleBreadService/.local/";
        //private string local = "C:/dev/TeleBread/.local/";

        private static readonly Dictionary<string, string> _config = new Dictionary<string, string>();
      

        private static ITelegramBotClient botClient;
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private System.Timers.Timer _payDay = new System.Timers.Timer();
        
        

        /// <summary>
        /// Reads the config file in the local path (Not tracked in git)
        /// </summary>
        private void ReadConfig()
        {
            using (StreamReader sr = new StreamReader(_local + "config.conf"))
            {
                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine();
                    var linesplit = line.Split('=');
                    _config[linesplit[0]] = linesplit[1];
                }
            }
        }


        public void RunServer()
        {
            ReadConfig();
            _payDay.Interval = (GetNextPayday() - DateTime.Now).TotalMilliseconds;
            _payDay.Elapsed += (sender, e) => new Payroll(botClient, _config, _payDay);
            _payDay.AutoReset = false;
            _payDay.Enabled = true;
            botClient = new TelegramBotClient(_config["apiKey"]);
            

            var receiverOptions = new ReceiverOptions();
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
                _ = new RunCommand(botClient, update, _config);
            }
        }
        
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        public void OnDebug()
        {
            RunServer();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Bot Starting");
            RunServer();
        }

        protected override void OnStop()
        {
            cts.Cancel();
            WriteToFile("Bot Stopping.");
        }

        private DateTime GetNextPayday()
        {
            if (DateTime.Today.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour < 6)
            {
                return DateTime.Parse(DateTime.Now.ToString("MM/dd/yyyy")+" 06:00:00 AM");
            }
            if (DateTime.Today.DayOfWeek == DayOfWeek.Friday)
            {
                return DateTime.Parse(DateTime.Now.AddDays(7).ToString("MM/dd/yyyy")+" 06:00:00 AM");
            }

            DateTime returnDate = DateTime.Now;
            while (returnDate.DayOfWeek != DayOfWeek.Friday)
            {
                returnDate = returnDate.AddDays(1);
            }

            return DateTime.Parse(returnDate.ToString("MM/dd/yyy")+" 06:00:00 AM");
        }

        public void WriteToFile(string message)
        {
            message = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " " + message;
            string path = _local;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            string filepath = _local+"TemepromptBot.txt";
            if (!System.IO.File.Exists(filepath))
            {
                using (StreamWriter sw = System.IO.File.CreateText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
        }
    }
}
