using SimpleSchedule.Models;
using SimpleSchedule.Utilities;
using SimpleSchedule.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Controllers
{
    public class RequestController : Controller
    {
        private readonly IRequestRepository requestRepository;
        private readonly IEmailSender emailSender;
        private readonly IHolidayRepository holidayRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private string[] adminEmails;

        public RequestController(UserManager<ApplicationUser> userManager, IRequestRepository requestRepository, IEmailSender emailSender, IHolidayRepository holidayRepository)
        {
            this.requestRepository = requestRepository;
            this.emailSender = emailSender;
            this.holidayRepository = holidayRepository;
            this.userManager = userManager;
        }

        private static int WeekdayDifference(DateTime StartDate, DateTime EndDate, IHolidayRepository holidayRepository)
        {
            DateTime thisDate = StartDate;
            int weekDays = 0;
            while (thisDate <= EndDate)
            {
                if (thisDate.DayOfWeek != DayOfWeek.Saturday && thisDate.DayOfWeek != DayOfWeek.Sunday) 
                {
                    if (!holidayRepository.GetAllHolidays().Any(d => d.DateOfHoliday == thisDate)) { weekDays++; }
                }
                if (EndDate >= StartDate) { thisDate = thisDate.AddDays(1); } else { thisDate = thisDate.AddDays(-1); }
            }
            return weekDays;
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

        private async Task<int> emailTimeOffSummary(ApplicationUser user, string emailContent)
        {
            var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have " + emailContent + "Your new summary is below.<br><br>" + DateTime.Now.Year + " Days Left: " + user.VacationDaysLeft + "<br>" + DateTime.Now.Year + " Days Used: " + user.VacationDaysUsed + "<br>" + (DateTime.Now.Year+1) + " Days Left: " + user.NextYearVacationDaysLeft + "<br>" + (DateTime.Now.Year+1) + " Days Used: " + user.NextYearVacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
            await emailSender.SendEmailAsync(currentUserMessage);
            await getAdminEmails();
            if (adminEmails.Any())
            {
                var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has " + emailContent + "Their new summary is below.<br><br>" + DateTime.Now.Year + " Days Left: " + user.VacationDaysLeft + "<br>" + DateTime.Now.Year + " Days Used: " + user.VacationDaysUsed + "<br>" + (DateTime.Now.Year + 1) + " Days Left: " + user.NextYearVacationDaysLeft + "<br>" + (DateTime.Now.Year + 1) + " Days Used: " + user.NextYearVacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                await emailSender.SendEmailAsync(adminMessage);
            }
            return 1;
        }

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            IndexRequestViewModel model = new IndexRequestViewModel
            {
                VacationDaysLeft = user.VacationDaysLeft,
                VacationDaysUsed = user.VacationDaysUsed,
                NextYearVacationDaysLeft = user.NextYearVacationDaysLeft,
                NextYearVacationDaysUsed = user.NextYearVacationDaysUsed,
                SickDaysLeft = user.SickDaysLeft,
                SickDaysUsed = user.SickDaysUsed,
                requests = requestRepository.GetAllRequests(userManager.GetUserId(HttpContext.User)),
                applicationUsers = userManager.Users.ToList(),
                allRequests = requestRepository.GetOthersRequests(userManager.GetUserId(HttpContext.User))
            };
            return View(model);
        }

        [HttpGet]
        public ViewResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Request request)
        {
            request.ApplicationUserID = userManager.GetUserId(HttpContext.User);
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                if (request.StartDate > request.EndDate)
                {
                    ModelState.AddModelError("", "End Date must be after Start Date.");
                }
                else if (request.StartDate.Year != request.EndDate.Year)
                {
                    ModelState.AddModelError("", "Request cannot span multiple years. Please make two seperate requests; one for this year and another for next year.");
                }
                else
                {
                    int daysOff = WeekdayDifference(request.StartDate, request.EndDate, holidayRepository);
                    if (request.EndDate.Year == DateTime.Now.Year)
                    {
                        if (user.VacationDaysLeft > daysOff)
                        {
                            user.VacationDaysLeft = user.VacationDaysLeft - daysOff;
                            user.VacationDaysUsed = user.VacationDaysUsed + daysOff;
                        }
                        else
                        {
                            ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                        }
                    }
                    else
                    {
                        if (user.NextYearVacationDaysLeft > daysOff)
                        {
                            user.NextYearVacationDaysLeft = user.NextYearVacationDaysLeft - daysOff;
                            user.NextYearVacationDaysUsed = user.NextYearVacationDaysUsed + daysOff;
                        }
                        else
                        {
                            ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                        }
                    }
                    // Send Emails
                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled Time Off", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please add it to your calendar.", "#", "");
                        await emailSender.SendEmailAsync(otherUsersMessage);
                    }

                    await emailTimeOffSummary(user, "scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                    Request newRequest = requestRepository.Add(request);
                    return RedirectToAction("index");
                }
            }

            return View();
        }

        [HttpGet]
        public ViewResult Edit(int Id)
        {
            Request request = requestRepository.GetRequest(Id);
            var model = new EditRequestViewModel
            {
                RequestId = request.RequestId,
                ApplicationUserID = request.ApplicationUserID,
                StartDate = request.StartDate,
                PreviousStartDate = request.StartDate,
                EndDate = request.EndDate,
                PreviousEndDate = request.EndDate,

            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditRequestViewModel requestUpdate)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                int newDaysOff = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);
                int previousDaysOff = WeekdayDifference(requestUpdate.PreviousStartDate, requestUpdate.PreviousEndDate, holidayRepository);
                if (newDaysOff > previousDaysOff)
                {
                    if (requestUpdate.EndDate.Year == DateTime.Now.Year)
                    {
                        if (user.VacationDaysLeft > (newDaysOff - previousDaysOff))
                        {
                            user.VacationDaysLeft = user.VacationDaysLeft - (newDaysOff - previousDaysOff);
                            user.VacationDaysUsed = user.VacationDaysUsed + (newDaysOff - previousDaysOff);
                            Request request = requestRepository.GetRequest(requestUpdate.RequestId);

                            string[] otherUserEmails = getOtherUserEmails(user);
                            if (otherUserEmails.Any())
                            {
                                var message = new Message(otherUserEmails, user.Email + " Has Modified Their Time Off", user.Email + " has modifieded their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + "<br>Updated time off: " + requestUpdate.StartDate.ToShortDateString() + " thru " + requestUpdate.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                                await emailSender.SendEmailAsync(message);
                            }

                            request.StartDate = requestUpdate.StartDate;
                            request.EndDate = requestUpdate.EndDate;
                            request.ApplicationUserID = requestUpdate.ApplicationUserID;

                            await emailTimeOffSummary(user, "modified time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                            requestRepository.Update(request);
                            return RedirectToAction("index");
                        }
                        else
                        {
                            ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                        }
                    }
                    else
                    {
                        if (user.NextYearVacationDaysLeft > (newDaysOff - previousDaysOff))
                        {
                            user.NextYearVacationDaysLeft = user.NextYearVacationDaysLeft - (newDaysOff - previousDaysOff);
                            user.NextYearVacationDaysUsed = user.NextYearVacationDaysUsed + (newDaysOff - previousDaysOff);
                            Request request = requestRepository.GetRequest(requestUpdate.RequestId);

                            string[] otherUserEmails = getOtherUserEmails(user);
                            if (otherUserEmails.Any())
                            {
                                var message = new Message(otherUserEmails, user.Email + " Has Modified Their Time Off", user.Email + " has modifieded their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + "<br>Updated time off: " + requestUpdate.StartDate.ToShortDateString() + " thru " + requestUpdate.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                                await emailSender.SendEmailAsync(message);
                            }

                            request.StartDate = requestUpdate.StartDate;
                            request.EndDate = requestUpdate.EndDate;
                            request.ApplicationUserID = requestUpdate.ApplicationUserID;

                            await emailTimeOffSummary(user, "modified time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                            requestRepository.Update(request);
                            return RedirectToAction("index");
                        }
                        else
                        {
                            ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                        }
                    }
                }
                else if (previousDaysOff > newDaysOff)
                {
                    if (requestUpdate.EndDate.Year == DateTime.Now.Year)
                    {
                        user.VacationDaysLeft = user.VacationDaysLeft + (previousDaysOff - newDaysOff);
                        user.VacationDaysUsed = user.VacationDaysUsed - (previousDaysOff - newDaysOff);
                    }
                    else
                    {
                        user.NextYearVacationDaysLeft = user.NextYearVacationDaysLeft + (previousDaysOff - newDaysOff);
                        user.NextYearVacationDaysUsed = user.NextYearVacationDaysUsed - (previousDaysOff - newDaysOff);
                    }
                    Request request = requestRepository.GetRequest(requestUpdate.RequestId);

                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Has Modified Their Time Off", user.Email + " has modifieded their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + "<br>Updated time off: " + requestUpdate.StartDate.ToShortDateString() + " thru " + requestUpdate.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }

                    request.StartDate = requestUpdate.StartDate;
                    request.EndDate = requestUpdate.EndDate;
                    request.ApplicationUserID = requestUpdate.ApplicationUserID;

                    await emailTimeOffSummary(user, "modified time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                    requestRepository.Update(request);
                    return RedirectToAction("index");
                }
                else
                {
                    Request request = requestRepository.GetRequest(requestUpdate.RequestId);

                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Has Modified Their Time Off", user.Email + " has modifieded their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + "<br>Updated time off: " + requestUpdate.StartDate.ToShortDateString() + " thru " + requestUpdate.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }

                    request.StartDate = requestUpdate.StartDate;
                    request.EndDate = requestUpdate.EndDate;
                    request.ApplicationUserID = requestUpdate.ApplicationUserID;

                    await emailTimeOffSummary(user, "modified time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                    requestRepository.Update(request);
                    return RedirectToAction("index");
                }
            }
            return View(requestUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int Id)
        {
            Request request = requestRepository.GetRequest(Id);
            var user = await userManager.GetUserAsync(HttpContext.User);
            int daysOff = WeekdayDifference(request.StartDate, request.EndDate, holidayRepository);
            if (request.EndDate.Year == DateTime.Now.Year)
            {
                user.VacationDaysLeft = user.VacationDaysLeft + daysOff;
                user.VacationDaysUsed = user.VacationDaysUsed - daysOff;
            }
            else
            {
                user.NextYearVacationDaysLeft = user.NextYearVacationDaysLeft + daysOff;
                user.NextYearVacationDaysUsed = user.NextYearVacationDaysUsed - daysOff;
            }
            // Send Emails
            string[] otherUserEmails = getOtherUserEmails(user);
            if (otherUserEmails.Any())
            {
                var message = new Message(otherUserEmails, user.Email + " Has Cancelled Their Time Off", user.Email + " has cancelled their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please remove this from your calendar.", "#", "");
                await emailSender.SendEmailAsync(message);
            }

            await emailTimeOffSummary(user, "deleted time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

            requestRepository.Delete(Id);
            return RedirectToAction("index");
        }

        public async Task<IActionResult> CallInSickFull()
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                string[] otherUserEmails = getOtherUserEmails(user);
                if (otherUserEmails.Any())
                {
                    var message = new Message(otherUserEmails, user.Email + " Is Taking A Sick Day", user.Email + " has called in sick for the whole day. Please note their absence and let any customers know.", "#", "");
                    await emailSender.SendEmailAsync(message);
                }

                if (user.SickDaysLeft > 0)
                {
                    user.SickDaysLeft -= 1;
                }
                user.SickDaysUsed += 1;

                await emailTimeOffSummary(user, "called in sick for the day. ");

                var result = await userManager.UpdateAsync(user);
                return RedirectToAction("index");
            }
            return RedirectToAction("index");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex()
        {
            AdminIndexViewModel model = new AdminIndexViewModel
            {
                applicationUsers = userManager.Users.ToList(),
                requests = requestRepository.GetAllRequestsAdmin()
            };
            return View(model);
        }

    }
}
