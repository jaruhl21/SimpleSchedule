using SimpleSchedule.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SimpleSchedule.ViewModels
{
    public class CreateERViewModel : EarlyRelease
    {
        public string ApplicationUserId { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime AdjustmentDate { get; set; }
        public DateTime AdjustmentTime { get; set; }
    }
}
