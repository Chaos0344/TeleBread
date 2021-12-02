using System;
using TeleBreadService;

namespace TeleBreadService.Objects
{
    public class OrbPredictions
    {
        public string predictionText { get; set; }
        public long predictionTarget { get; set; }
        public long predictionChat { get; set; }

        public OrbPredictions(string text, long target, long chat)
        {
            predictionText = text;
            predictionTarget = target;
            predictionChat = chat;
        }
    }
}