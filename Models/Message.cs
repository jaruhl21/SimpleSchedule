using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string ButtonLink { get; set; }
        public string ButtonText { get; set; }
        public Message(IEnumerable<string> to, string subject, string content, string link, string btntext)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(x => new MailboxAddress(x)));
            Subject = subject;
            Content = content;
            ButtonLink = link;
            ButtonText = btntext;
        }
    }
}
