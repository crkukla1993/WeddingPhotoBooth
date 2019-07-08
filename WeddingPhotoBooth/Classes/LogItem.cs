using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeddingPhotoBooth.Classes
{
    public class LogItem
    {
        public DateTime LogDate { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogItemType LogItemType { get; set; }
        public string Action { get; set; }
        public string EmailAddress { get; set; }
    }

    public enum LogItemType
    {
        Start = 0,
        System = 1,
        Complete = 2,
        Error = 3
    }
}
