using SimpleSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class EditHolidayViewModel : Holiday
    {
        public DateTime PreviousDate { get; set; }
    }
}
