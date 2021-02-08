using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class Holiday
    {
        public int HolidayID { get; set; }
        [Required]
        public DateTime DateOfHoliday { get; set; }
        public string NameOfHoliday { get; set; }
    }
}
