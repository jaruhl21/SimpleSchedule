using SimpleSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Utilities
{
    public interface IEmailSender
    {
        Task SendEmailAsync(Message message);
    }
}
