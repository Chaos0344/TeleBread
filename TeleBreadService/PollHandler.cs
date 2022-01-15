using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Poll = TeleBreadService.General.Poll;

namespace TeleBreadService
{
    public class PollHandler
    {
        public PollHandler(ITelegramBotClient botClient, long pollId, List<Poll> polls, Update e, Dictionary<string, string> config)
        {
            foreach (var poll in polls)
            {
                if (poll.PollId == pollId)
                {
                    try
                    {
                        if (e.PollAnswer.OptionIds[0] == 0)
                        {
                            poll.CastVote(e.PollAnswer.User.Id, "Yes");
                        }
                        else
                        {
                            poll.CastVote(e.PollAnswer.User.Id, "No");
                        }
                    }
                    catch (Exception z)
                    {
                        poll.retractVote(e.PollAnswer.User.Id);
                    }

                    if (poll.Type == "Badge")
                    {
                        if (poll.tallyHalf())
                        {
                            new Commands(config).grantBadge(botClient, poll.TargetId, poll.ChatId, poll.Value);
                            polls.Remove(poll);
                            return;
                        }
                    }
                }
            }
        }
    }
}