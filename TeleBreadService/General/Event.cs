using System;
using System.Collections.Generic;
using System.Net;

namespace TeleBreadService.General
{
    public class Event
    {
        private string EventType { get; set; }
        private DateTime EventDateTime { get; set; }
        private Dictionary<string, string> Args { get; set; }

        public void RunEvent()
        {
            switch (EventType)
            {
                case "OrbPrediction":
                    
                    break;
                default:
                    Console.WriteLine("Doesn't exist");
                    break;
            }
        }

        Event(string et, DateTime edt, Dictionary<string, string> a)
        {
            EventType = et;
            EventDateTime = edt;
            Args = a;
        }
    }
}