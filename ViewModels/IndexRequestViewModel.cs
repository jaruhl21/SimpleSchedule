using SimpleSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class IndexRequestViewModel
    {
        public int VacationDaysLeft { get; set; }
        public int VacationDaysUsed { get; set; }
        public int SickDaysLeft { get; set; }
        public int SickDaysUsed { get; set; }
        public IEnumerable<Request> requests { get; set; }
    }
}
