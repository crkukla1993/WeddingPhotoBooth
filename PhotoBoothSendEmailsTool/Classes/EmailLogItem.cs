using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoBoothSendEmailsTool.Classes
{
    public class EmailLogItem
    {
        public EmailLogItem(bool sendEmail, string sessionID, string emailAddress, bool offline)
        {
            SendEmail = sendEmail;
            SessionID = sessionID;
            EmailAddress = emailAddress;
            Offline = offline;
        }
        public bool SendEmail { get; set; }
        public string SessionID { get; }
        public string EmailAddress { get; }
        public bool Offline { get; }
    }
}
