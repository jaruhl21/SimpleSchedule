using System.ComponentModel.DataAnnotations;

namespace SimpleSchedule.ViewModels
{
    public class CreateRoleViewModel
    {
        [Required]
        public string RoleName { get; set; }
    }
}