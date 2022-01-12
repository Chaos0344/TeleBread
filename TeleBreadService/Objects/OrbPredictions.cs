using System;
using System.Collections.Generic;
using TeleBreadService;

namespace TeleBreadService.Objects
{
    public class OrbPredictions
    {
        public string predictionText { get; set; }
        public long predictionTarget { get; set; }
        public long predictionChat { get; set; }
        private Dictionary<string, string> config { get; set; }
        public int predictionId { get; set; }

        public void Triggered()
        {
            new General.CommonFunctions(config).WriteQuery(
                $"UPDATE dbo.Predictions set triggered = 1 WHERE predictionID = {predictionId}");
        }

        public OrbPredictions(long target, long chat, Dictionary<string, string> c)
        {
            predictionTarget = target;
            predictionChat = chat;
            config = c;
        }
        
        public OrbPredictions(string text, long target, long chat, Dictionary<string, string> c, int id)
        {
            predictionText = text;
            predictionTarget = target;
            predictionChat = chat;
            predictionId = id;
            config = c;
        }

        public void AddText(string text)
        {
            predictionText = text;
        }

        public void SaveToDB()
        {
            predictionId = new General.CommonFunctions(config).WriteQueryWithId(
                $"INSERT INTO dbo.Predictions " +
                $"(userID, groupChat, predictionText) " +
                $"VALUES ({predictionTarget}, {predictionChat}, '{predictionText}'); " +
                $"SELECT SCOPE_IDENTITY()");
        }
    }
}