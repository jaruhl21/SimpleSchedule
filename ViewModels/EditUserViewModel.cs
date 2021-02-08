using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.ViewModels
{
    public class EditUserViewModel
    {
        public EditUserViewModel()
        {
            Roles = new List<string>();
        }

        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Personal/Vacation Days Left")]
        public int VacationDaysLeft { get; set; }

        [Display(Name = "Sick Days Left")]
        public int SickDaysLeft { get; set; }

        [Display(Name = "Personal/Vacation Days Used")]
        public int VacationDaysUsed { get; set; }

        [Display(Name = "Sick Days Used")]
        public int SickDaysUsed { get; set; }

        public IList<string> Roles { get; set; }
    }
}
