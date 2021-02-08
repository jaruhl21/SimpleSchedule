using SimpleSchedule.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class CreateERViewModel : EarlyRelease
    {
        public string ApplicationUserId { get; set; }
    }
}
