using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeddingPhotoBooth.Classes
{
    public class LogSession
    {
        public string SessionKey { get; set; }
        public DateTime CreateDate { get; set; }
        public bool Offline { get; set; }
        public List<LogItem> Items { get; set; }
    }
}
