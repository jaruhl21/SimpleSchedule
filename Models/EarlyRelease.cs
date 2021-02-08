using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class EarlyRelease
    {
        public int EarlyReleaseID { get; set; }
        [Required]
        public DateTime EarlyReleaseDateTime { get; set; }
        public float TimeMissed { get; set; }
        public string Reason { get; set; }
        public string ApplicationUserID { get; set; }
        public ApplicationUser User { get; set; }
    }
}
