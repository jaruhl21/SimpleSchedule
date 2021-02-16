using SimpleSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class AdminIndexERViewModel
    {
        public IEnumerable<ApplicationUser> applicationUsers { get; set; }
        public IEnumerable<EarlyRelease> earlyReleases { get; set; }
        public string Message { get; set; }
    }
}
