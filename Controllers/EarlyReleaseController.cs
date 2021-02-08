using SimpleSchedule.Models;
using SimpleSchedule.Utilities;
using SimpleSchedule.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Controllers
{
    public class EarlyReleaseController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEarlyReleaseRepository earlyReleaseRepository;
        private readonly IEmailSender emailSender;
        private string[] adminEmails;

        public EarlyReleaseController(UserManager<ApplicationUser> userManager, IEarlyReleaseRepository earlyReleaseRepository, IEmailSender emailSender)
        {
            this.userManager = userManager;
            this.earlyReleaseRepository = earlyReleaseRepository;
            this.emailSender = emailSender;
        }

        private string[] getOtherUserEmails(ApplicationUser user)
        {
            List<string> list = new List<string>();
            foreach (var otherUsers in userManager.Users)
            {
                if (otherUsers != user)
                {
                    list.Add(otherUsers.Email);
                }
            }
            string[] returnStringArray = list.ToArray();
            return returnStringArray;
        }

        private async Task<string[]> getAdminEmails()
        {
            List<string> list = new List<string>();
            foreach (var user in userManager.Users.ToList())
            {
                if (await userManager.IsInRoleAsync(user, "Admin"))
                {
                    list.Add(user.Email);
                }
            }
            adminEmails = list.ToArray();
            return adminEmails;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            
            AdminIndexERViewModel model = new AdminIndexERViewModel
            {
                applicationUsers = userManager.Users.ToList(),
                earlyReleases = earlyReleaseRepository.GetAllEarlyReleasesAdmin()
            };

            if (!String.IsNullOrEmpty(searchString))
            {
                string filteredUserID = model.applicationUsers.First(s => s.Email.Contains(searchString)).Id;
                model.earlyReleases = earlyReleaseRepository.GetAllEarlyReleases(filteredUserID);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EarlyRelease earlyRelease)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            earlyRelease.ApplicationUserID = userManager.GetUserId(HttpContext.User);

            DateTime today = DateTime.Today;
            if (today.DayOfWeek == DayOfWeek.Sunday || today.DayOfWeek == DayOfWeek.Tuesday || today.DayOfWeek == DayOfWeek.Wednesday || today.DayOfWeek == DayOfWeek.Thursday)
            {
                today = today.AddHours(18);
            }
            else if (today.DayOfWeek == DayOfWeek.Friday)
            {
                today = today.AddHours(17);
            }
            else
            {
                ModelState.AddModelError("", "You can't call out on a weekend!");
            }

            if (ModelState.IsValid)
            {
                float hoursOff = today.Hour - earlyRelease.EarlyReleaseDateTime.Hour;
                float minutesOff = 0;
                if (earlyRelease.EarlyReleaseDateTime.Minute == 0) 
                { 
                    earlyRelease.TimeMissed = hoursOff; 
                }
                else
                {
                    minutesOff = 60 - earlyRelease.EarlyReleaseDateTime.Minute;
                    hoursOff = hoursOff + (minutesOff / 60) - 1;
                    earlyRelease.TimeMissed = hoursOff;
                }

                string[] otherUserEmails = getOtherUserEmails(user);
                if (otherUserEmails.Any())
                {
                    var message = new Message(otherUserEmails, user.Email + " Will Be Leaving At " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString(), user.Email + " will be leaving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + ". Please note their absence and let any customers know.", "#", "");
                    await emailSender.SendEmailAsync(message);
                }

                var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have submitted that you will be leaving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                await emailSender.SendEmailAsync(currentUserMessage);
                await getAdminEmails();
                if (adminEmails.Any())
                {
                    var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has submitted that they will be leaving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(adminMessage);
                }

                EarlyRelease newER = earlyReleaseRepository.Add(earlyRelease);
                return RedirectToAction("Index", "Request");
            }
            return View();
        }
    }
}
