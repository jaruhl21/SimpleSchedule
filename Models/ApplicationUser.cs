using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int VacationDaysLeft { get; set; }
        public int VacationDaysUsed { get; set; }
        public int NextYearVacationDaysLeft { get; set; }
        public int NextYearVacationDaysUsed { get; set; }
        public int SickDaysLeft { get; set; }
        public int SickDaysUsed { get; set; }
        public ICollection<Request> Requests { get; set; }
        public ICollection<EarlyRelease> EarlyReleases { get; set; }
    }
}
