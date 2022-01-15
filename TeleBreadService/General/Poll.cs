using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;
using Telegram.Bot.Requests;

namespace TeleBreadService.General
{
    public class Poll
    {
        public string Type { get; set; }
        public Dictionary<long, bool?> Votes = new Dictionary<long, bool?>();
        public int Time = 0; // in minutes
        public long PollId;
        public long TargetId;
        public long ChatId;
        public string Value;
        private Dictionary<string, string> Config { get; set; }

        public Poll(){}

        public Poll(long pId, string type, long chatId, long targetId, int? time, string value, Dictionary<string, string> config)
        {
            PollId = pId;
            Type = type;
            TargetId = targetId;
            ChatId = chatId;
            Value = value;
            if (time != null)
            {
                Time = (int)time;
            }

            Config = config;

            CommonFunctions cf = new CommonFunctions(Config);
            DataTable dt = cf.RunQuery($"SELECT userID from dbo.Users WHERE groupChat = {chatId}", new[] {"userID"});
            foreach (DataRow row in dt.Rows)
            {
                long uid = long.Parse(row["userID"].ToString());
                Votes[uid] = null;
            }
        }

        public void CastVote(long userId, string vote)
        {
            bool? v = null;
            if (vote == "Yes")
            {
                v = true;
            }
            else if (vote == "No")
            {
                v = false;
            }

            Votes[userId] = v;
        }

        public void retractVote(long userId)
        {
            Votes[userId] = null;
        }

        public bool tallyWeighted()
        {
            int counter = 0;
            int yes = 0;
            int no = 0;
            int obstain = 0;
            foreach (var result in Votes.Values)
            {
                counter++;
                if (result == true)
                {
                    yes++;
                } else if (result == false)
                {
                    no++;
                }
                else
                {
                    obstain++;
                }
            }
            if (yes > (no + obstain))
            {
                return true;
            }
            return false;
        }
        
        public bool tallyHalf()
        {
            int counter = 0;
            int yes = 0;
            int no = 0;
            int obstain = 0;
            foreach (var result in Votes.Values)
            {
                counter++;
                if (result == true)
                {
                    yes++;
                } else if (result == false)
                {
                    no++;
                }
                else
                {
                    obstain++;
                }
            }
            if (yes > (counter/2))
            {
                return true;
            }
            return false;
        }
    }
}