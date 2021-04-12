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
        public IActionResult AdminIndex(string searchString, DateTime searchStartDate, DateTime searchEndDate)
        {
            ViewData["NameFilter"] = searchString;
            ViewData["StartDateFilter"] = searchStartDate;
            ViewData["EndDateFilter"] = searchEndDate;

            DateTime searchdatestart = new DateTime(2020, 1, 1);

            AdminIndexERViewModel model = new AdminIndexERViewModel
            {
                applicationUsers = userManager.Users.ToList(),
                earlyReleases = earlyReleaseRepository.GetAllEarlyReleasesAdmin()
            };

            List<EarlyRelease> ERList = model.earlyReleases.ToList();

            if (!String.IsNullOrEmpty(searchString))
            {
                var searchusers = model.applicationUsers.Where(s => s.Email.Contains(searchString)).ToList();
                foreach (var er in ERList.ToArray())
                {
                    if (!searchusers.Contains(model.applicationUsers.First(s => s.Id == er.ApplicationUserID)))
                    {
                        ERList.Remove(er);
                        model.Message = "No Results from search";
                    }
                }
            }
            if (searchStartDate > searchdatestart)
            {
                foreach (var er in ERList.ToArray())
                {
                    if (er.EarlyReleaseDateTime < searchStartDate)
                    {
                        ERList.Remove(er);
                        model.Message = "No Results from search";
                    }
                }
            }
            if (searchEndDate > searchdatestart)
            {
                foreach (var er in ERList.ToArray())
                {
                    if (er.EarlyReleaseDateTime > searchEndDate)
                    {
                        ERList.Remove(er);
                        model.Message = "No Results from search";
                    }
                }
            }

            model.earlyReleases = ERList.AsEnumerable();

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateERViewModel earlyRelease)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            earlyRelease.ApplicationUserID = userManager.GetUserId(HttpContext.User);

            if (ModelState.IsValid)
            {
                earlyRelease.EarlyReleaseDateTime = earlyRelease.AdjustmentDate.Date.Add(earlyRelease.AdjustmentTime.TimeOfDay);

                DateTime dayToCheck = earlyRelease.EarlyReleaseDateTime.Date;
                if (dayToCheck.DayOfWeek == DayOfWeek.Monday || dayToCheck.DayOfWeek == DayOfWeek.Tuesday || dayToCheck.DayOfWeek == DayOfWeek.Wednesday || dayToCheck.DayOfWeek == DayOfWeek.Thursday)
                {
                    dayToCheck = dayToCheck.AddHours(18);
                }
                else if (dayToCheck.DayOfWeek == DayOfWeek.Friday)
                {
                    dayToCheck = dayToCheck.AddHours(17);
                }
                else
                {
                    ModelState.AddModelError("", "It's the weekend! Try again on a workday.");
                    return View();
                }

                if (earlyRelease.AdjustmentType == "Show Up Late")
                {
                    float hoursOff = earlyRelease.EarlyReleaseDateTime.Hour - 8;
                    if (earlyRelease.EarlyReleaseDateTime.Minute == 0)
                    {
                        hoursOff -= 0.5f;
                    }
                    else if (earlyRelease.EarlyReleaseDateTime.Minute == 15)
                    {
                        hoursOff -= 0.25f;
                    }
                    else if (earlyRelease.EarlyReleaseDateTime.Minute == 45)
                    {
                        hoursOff += 0.25f;
                    }

                    earlyRelease.TimeMissed = hoursOff;

                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Will Be Arriving At " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " On " + earlyRelease.EarlyReleaseDateTime.ToShortDateString(), user.Email + " will be arriving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " on " + earlyRelease.EarlyReleaseDateTime.ToShortDateString() + ". Please note their absence and let any customers know.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }

                    var currentUserMessage = new Message(new string[] { user.Email }, "You submitted an ER", "You have submitted that you will be arriving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " on " + earlyRelease.EarlyReleaseDateTime.ToShortDateString() + "<br><br>If this was made in error, please ask an Admin to delete it.", "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);

                    EarlyRelease newER = earlyReleaseRepository.Add(earlyRelease);
                    return RedirectToAction("Index", "Request");
                }
                else if (earlyRelease.AdjustmentType == "Leave Early")
                {
                    float hoursOff = dayToCheck.Hour - earlyRelease.EarlyReleaseDateTime.Hour;
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
                        var message = new Message(otherUserEmails, user.Email + " Will Be Leaving At " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " On " + earlyRelease.EarlyReleaseDateTime.ToShortDateString(), user.Email + " will be leaving at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " on " + earlyRelease.EarlyReleaseDateTime.ToShortDateString() + ". Please note their absence and let any customers know.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }

                    var currentUserMessage = new Message(new string[] { user.Email }, "You submitted an ER", "You have submitted that you will be leaving early at " + earlyRelease.EarlyReleaseDateTime.ToShortTimeString() + " on " + earlyRelease.EarlyReleaseDateTime.ToShortDateString() + "<br><br>If this was made in error, please ask an Admin to delete it.", "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);

                    EarlyRelease newER = earlyReleaseRepository.Add(earlyRelease);
                    return RedirectToAction("Index", "Request");
                }
                else
                {
                    ModelState.AddModelError("", "Please choose an option to show late or leave early");
                }
            }
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int Id)
        {
            earlyReleaseRepository.Delete(Id);
            return RedirectToAction("AdminIndex");
        }

    }
}
