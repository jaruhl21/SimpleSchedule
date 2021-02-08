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

        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            IndexRequestViewModel model = new IndexRequestViewModel
            {
                VacationDaysLeft = user.VacationDaysLeft,
                VacationDaysUsed = user.VacationDaysUsed,
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
                else
                {
                    int daysOff = WeekdayDifference(request.StartDate, request.EndDate, holidayRepository);
                    if (user.VacationDaysLeft > daysOff)
                    {
                        string[] otherUserEmails = getOtherUserEmails(user);
                        if (otherUserEmails.Any())
                        {
                            var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled Time Off", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please add it to your calendar.", "#", "");
                            await emailSender.SendEmailAsync(otherUsersMessage);
                        }

                        user.VacationDaysLeft = user.VacationDaysLeft - daysOff;
                        user.VacationDaysUsed = user.VacationDaysUsed + daysOff;

                        var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                        await emailSender.SendEmailAsync(currentUserMessage);
                        await getAdminEmails();
                        if (adminEmails.Any())
                        {
                            var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                            await emailSender.SendEmailAsync(adminMessage);
                        }

                        Request newRequest = requestRepository.Add(request);
                        return RedirectToAction("index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                    }
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

                        var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                        await emailSender.SendEmailAsync(currentUserMessage);
                        await getAdminEmails();
                        if (adminEmails.Any())
                        {
                            var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                            await emailSender.SendEmailAsync(adminMessage);
                        }

                        requestRepository.Update(request);
                        return RedirectToAction("index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                    }
                }
                else if (previousDaysOff > newDaysOff)
                {
                    user.VacationDaysLeft = user.VacationDaysLeft + (previousDaysOff - newDaysOff);
                    user.VacationDaysUsed = user.VacationDaysUsed - (previousDaysOff - newDaysOff);
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

                    var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);
                    await getAdminEmails();
                    if (adminEmails.Any())
                    {
                        var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                        await emailSender.SendEmailAsync(adminMessage);
                    }

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

                    var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);
                    await getAdminEmails();
                    if (adminEmails.Any())
                    {
                        var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has scheduled time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                        await emailSender.SendEmailAsync(adminMessage);
                    }

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

            string[] otherUserEmails = getOtherUserEmails(user);
            if (otherUserEmails.Any())
            {
                var message = new Message(otherUserEmails, user.Email + " Has Cancelled Their Time Off", user.Email + " has cancelled their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please remove this from your calendar.", "#", "");
                await emailSender.SendEmailAsync(message);
            }

            user.VacationDaysLeft = user.VacationDaysLeft + daysOff;
            user.VacationDaysUsed = user.VacationDaysUsed - daysOff;

            var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have deleted your time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
            await emailSender.SendEmailAsync(currentUserMessage);
            await getAdminEmails();
            if (adminEmails.Any())
            {
                var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has deleted their time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                await emailSender.SendEmailAsync(adminMessage);
            }

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

                var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "You have called in sick for the day. Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                await emailSender.SendEmailAsync(currentUserMessage);
                await getAdminEmails();
                if (adminEmails.Any())
                {
                    var adminMessage = new Message(adminEmails, user.Email + "'s New Time Off Summary", user.Email + " has called in sick for the day. Their new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(adminMessage);
                }

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
