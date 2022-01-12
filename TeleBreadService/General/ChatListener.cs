using TeleBreadService.Objects;

namespace TeleBreadService.General
{
    public class ChatListener
    {
        public long target { get; set; }
        public string type { get; set; } // Valid types: Callback, Text
        public string subtype { get; set; }
        
        public OrbPredictions predictionHolder { get; set; }

        public ChatListener(long _target, string _type, string _subtype)
        {
            target = _target;
            type = _type;
            subtype = _subtype;
        }
    }
}