using SimpleSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class EditRequestViewModel : Request
    {
        public DateTime PreviousStartDate { get; set; }
        public DateTime PreviousEndDate { get; set; }
    }
}
