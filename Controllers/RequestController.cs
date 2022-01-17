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

        public static int WeekdayDifference(DateTime StartDate, DateTime EndDate, IHolidayRepository holidayRepository)
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

        private int conflictCheck(ApplicationUser user, DateTime firstDay, DateTime lastDay)
        {
            var requests = requestRepository.GetAllRequests(user.Id);
            foreach (var req in requests)
            {
                if((firstDay >= req.StartDate && firstDay <= req.EndDate) || (lastDay >= req.StartDate && lastDay <= req.EndDate))
                {
                    return req.RequestId;
                }
                if ((req.StartDate >= firstDay && req.StartDate <= lastDay) || (req.EndDate >= firstDay && req.EndDate <= lastDay))
                {
                    return req.RequestId;
                }
            }
            return 0;
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
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            model.ApplicationUserID = userManager.GetUserId(HttpContext.User);
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                if (model.StartDate > model.EndDate)
                {
                    ModelState.AddModelError("", "End Date must be after Start Date.");
                }
                else
                {
                    var conCheck = conflictCheck(user, model.StartDate, model.EndDate);

                    if (conCheck != 0)
                    {
                        ModelState.AddModelError("", "This conflicts with Request ID : " + conCheck);
                        return View(model);
                    }

                    if (model.SpecialCase == "Business Trip")
                    {
                        Request newRequest = new Request
                        {
                            ApplicationUserID = model.ApplicationUserID,
                            StartDate = model.StartDate,
                            EndDate = model.EndDate,
                            SpecialCase = "Business Trip to " + model.BusinessTripLocation,
                            numOfDays = WeekdayDifference(model.StartDate, model.EndDate, holidayRepository)
                        };
                        requestRepository.Add(newRequest);

                        // Send Emails
                        string[] otherUserEmails = getOtherUserEmails(user);
                        if (otherUserEmails.Any())
                        {
                            var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled A Business Trip", user.Email + " has scheduled a business trip for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + " to visit " + model.BusinessTripLocation + ".<br><br>Please add it to your calendar.", "#", "");
                            await emailSender.SendEmailAsync(otherUsersMessage);
                        }

                        await emailTimeOffSummary(user, "scheduled a <b>business trip</b> for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + " to visit " + model.BusinessTripLocation + ". ");

                        return RedirectToAction("index");
                    }
                    else if (model.SpecialCase == "Unpaid")
                    {
                        Request newRequest = new Request
                        {
                            ApplicationUserID = model.ApplicationUserID,
                            StartDate = model.StartDate,
                            EndDate = model.EndDate,
                            SpecialCase = "Unpaid Personal/Vacation",
                            numOfDays = WeekdayDifference(model.StartDate, model.EndDate, holidayRepository)
                        };
                        requestRepository.Add(newRequest);

                        // Send Emails
                        string[] otherUserEmails = getOtherUserEmails(user);
                        if (otherUserEmails.Any())
                        {
                            var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled Time Off", user.Email + " has scheduled time off for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ".<br><br>Please add it to your calendar.", "#", "");
                            await emailSender.SendEmailAsync(otherUsersMessage);
                        }

                        await emailTimeOffSummary(user, "scheduled <b>unpaid time off</b> for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ". ");

                        return RedirectToAction("index");
                    }
                    else
                    {
                        if (model.StartDate.Year == model.EndDate.Year)
                        {
                            int daysOff = WeekdayDifference(model.StartDate, model.EndDate, holidayRepository);
                            if (model.EndDate.Year == DateTime.Now.Year)
                            {
                                if (user.VacationDaysLeft >= daysOff)
                                {
                                    user.VacationDaysLeft -= daysOff;
                                    user.VacationDaysUsed += daysOff;

                                    Request newRequest = new Request
                                    {
                                        ApplicationUserID = model.ApplicationUserID,
                                        StartDate = model.StartDate,
                                        EndDate = model.EndDate,
                                        SpecialCase = "Standard Personal/Vacation",
                                        numOfDays = WeekdayDifference(model.StartDate, model.EndDate, holidayRepository)
                                    };
                                    requestRepository.Add(newRequest);

                                    // Send Emails
                                    string[] otherUserEmails = getOtherUserEmails(user);
                                    if (otherUserEmails.Any())
                                    {
                                        var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled Time Off", user.Email + " has scheduled time off for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ".<br><br>Please add it to your calendar.", "#", "");
                                        await emailSender.SendEmailAsync(otherUsersMessage);
                                    }

                                    await emailTimeOffSummary(user, "scheduled time off for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ". ");

                                    return RedirectToAction("index");
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
                                    user.NextYearVacationDaysLeft -= daysOff;
                                    user.NextYearVacationDaysUsed += daysOff;

                                    Request newRequest = new Request
                                    {
                                        ApplicationUserID = model.ApplicationUserID,
                                        StartDate = model.StartDate,
                                        EndDate = model.EndDate,
                                        SpecialCase = "Standard Personal/Vacation",
                                        numOfDays = WeekdayDifference(model.StartDate, model.EndDate, holidayRepository)
                                    };
                                    requestRepository.Add(newRequest);

                                    // Send Emails
                                    string[] otherUserEmails = getOtherUserEmails(user);
                                    if (otherUserEmails.Any())
                                    {
                                        var otherUsersMessage = new Message(otherUserEmails, user.Email + " Has Scheduled Time Off", user.Email + " has scheduled time off for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ".<br><br>Please add it to your calendar.", "#", "");
                                        await emailSender.SendEmailAsync(otherUsersMessage);
                                    }

                                    await emailTimeOffSummary(user, "scheduled time off for " + model.StartDate.ToShortDateString() + " thru " + model.EndDate.ToShortDateString() + ". ");

                                    return RedirectToAction("index");
                                }
                                else
                                {
                                    ModelState.AddModelError("", "You do not have enough Vacation Days Left for this request. Please shorten your request or talk to Arif.");
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Request cannot span multiple years. Please make two seperate requests; one for this year and another for next year.");
                        }
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
                SpecialCase = request.SpecialCase
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditRequestViewModel requestUpdate)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                if (requestUpdate.SpecialCase.Contains("Business Trip"))
                {
                    Request request = requestRepository.GetRequest(requestUpdate.RequestId);
                    request.StartDate = requestUpdate.StartDate;
                    request.EndDate = requestUpdate.EndDate;
                    request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);
                    requestRepository.Update(request);

                    // Send emails
                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Has Modified Their Business Trip", user.Email + " has modifieded their Business Trip.<br><br>Previous trip: " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + "<br>Updated trip: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }
                    await emailTimeOffSummary(user, "modified <b>" + request.SpecialCase + "</b> from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                    return RedirectToAction("index");
                }
                else if (requestUpdate.SpecialCase.Contains("Unpaid"))
                {
                    Request request = requestRepository.GetRequest(requestUpdate.RequestId);
                    request.StartDate = requestUpdate.StartDate;
                    request.EndDate = requestUpdate.EndDate;
                    request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);
                    requestRepository.Update(request);

                    // Send emails
                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Has Modified Their Time Off", user.Email + " has modifieded their time off.<br><br>Previous time off: " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + "<br>Updated time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please update your calendar.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }
                    await emailTimeOffSummary(user, "modified <b>" + request.SpecialCase + "</b> time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                    return RedirectToAction("index");
                }
                else
                {
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
                                request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);

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
                                request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);

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
                        request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);

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
                        request.numOfDays = WeekdayDifference(requestUpdate.StartDate, requestUpdate.EndDate, holidayRepository);

                        await emailTimeOffSummary(user, "modified time off from " + requestUpdate.PreviousStartDate.ToShortDateString() + " thru " + requestUpdate.PreviousEndDate.ToShortDateString() + " to " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                        requestRepository.Update(request);
                        return RedirectToAction("index");
                    }
                }
            }
            return View(requestUpdate);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int Id)
        {
            Request request = requestRepository.GetRequest(Id);
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (request.SpecialCase.Contains("Business"))
            {
                // Send Emails
                string[] otherUserEmails = getOtherUserEmails(user);
                if (otherUserEmails.Any())
                {
                    var message = new Message(otherUserEmails, user.Email + " Has Cancelled Their Business Trip", user.Email + " has cancelled their business trip.<br><br>Previous trip: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please remove this from your calendar.", "#", "");
                    await emailSender.SendEmailAsync(message);
                }

                await emailTimeOffSummary(user, "deleted <b>" + request.SpecialCase + "</b> for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                requestRepository.Delete(Id);
                return RedirectToAction("index");
            }
            else if (request.SpecialCase.Contains("Unpaid"))
            {
                // Send Emails
                string[] otherUserEmails = getOtherUserEmails(user);
                if (otherUserEmails.Any())
                {
                    var message = new Message(otherUserEmails, user.Email + " Has Cancelled Their Time Off", user.Email + " has cancelled their time off.<br><br>Previous time off: " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ".<br><br>Please remove this from your calendar.", "#", "");
                    await emailSender.SendEmailAsync(message);
                }

                await emailTimeOffSummary(user, "deleted <b>" + request.SpecialCase + "</b> time off for " + request.StartDate.ToShortDateString() + " thru " + request.EndDate.ToShortDateString() + ". ");

                requestRepository.Delete(Id);
                return RedirectToAction("index");
            }
            else
            {
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
        }

        [HttpGet]
        public ViewResult CallInSickFull()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CallInSickFull(Request sickRequest)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                var conCheck = conflictCheck(user, sickRequest.StartDate, sickRequest.EndDate);

                if (conCheck != 0)
                {
                    ModelState.AddModelError("", "This conflicts with Request ID : " + conCheck);
                    return View(sickRequest);
                }

                DateTime dayToCheck = sickRequest.StartDate.Date;
                if (dayToCheck.DayOfWeek == DayOfWeek.Saturday || dayToCheck.DayOfWeek == DayOfWeek.Sunday)
                {
                    ModelState.AddModelError("", "Can't call in sick on the weekend! Try again on a workday.");
                    return View(sickRequest);
                }

                if (user.SickDaysLeft > 0 || sickRequest.SpecialCase == "Unpaid Personal/Vacation (Sick)")
                {
                    string[] otherUserEmails = getOtherUserEmails(user);
                    if (otherUserEmails.Any())
                    {
                        var message = new Message(otherUserEmails, user.Email + " Is Taking A Sick Day on " + sickRequest.StartDate.ToShortDateString(), user.Email + " has called in sick for the whole day on " + sickRequest.StartDate.ToShortDateString() + ". Please note their absence and let any customers know.", "#", "");
                        await emailSender.SendEmailAsync(message);
                    }

                    if (sickRequest.SpecialCase == "Sick Day")
                    {
                        user.SickDaysLeft -= 1;
                        user.SickDaysUsed += 1;
                    }

                    if (sickRequest.SpecialCase == "Unpaid Personal/Vacation (Sick)")
                    {
                        user.SickDaysUsed += 1;
                    }

                    await emailTimeOffSummary(user, "called in sick on " + sickRequest.StartDate.ToShortDateString() + ". ");

                    sickRequest.EndDate = sickRequest.StartDate;
                    sickRequest.ApplicationUserID = user.Id;
                    sickRequest.numOfDays = 1;

                    requestRepository.Add(sickRequest);
                    var result = await userManager.UpdateAsync(user);
                    return RedirectToAction("index");
                }
                ModelState.AddModelError("", "You do not have any Sick Days left. Please change this to Unpaid Personal/Vacation (Sick).");
            }
            return View(sickRequest);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex(string searchString, DateTime searchStartDate, DateTime searchEndDate)
        {
            ViewData["NameFilter"] = searchString;
            ViewData["StartDateFilter"] = searchStartDate;
            ViewData["EndDateFilter"] = searchEndDate;

            DateTime searchdatestart = new DateTime(2020, 1, 1);

            AdminIndexViewModel model = new AdminIndexViewModel
            {
                applicationUsers = userManager.Users.ToList(),
                requests = requestRepository.GetAllRequestsAdmin(),
                Message = "No Requests at the moment."
            };

            List<Request> requestsList = model.requests.ToList();

            if (!String.IsNullOrEmpty(searchString))
            {
                var searchusers = model.applicationUsers.Where(s => s.Email.Contains(searchString)).ToList();
                foreach (var req in requestsList.ToArray())
                {
                    if (!searchusers.Contains(model.applicationUsers.First(s => s.Id == req.ApplicationUserID)))
                    {
                        requestsList.Remove(req);
                        model.Message = "No Results from search";
                    }
                }
            }
            if (searchStartDate > searchdatestart)
            {
                foreach (var req in requestsList.ToArray())
                {
                    if (req.EndDate < searchStartDate)
                    {
                        requestsList.Remove(req);
                        model.Message = "No Results from search";
                    }
                }
            }
            if (searchEndDate > searchdatestart)
            {
                foreach (var req in requestsList.ToArray())
                {
                    if (req.StartDate > searchEndDate)
                    {
                        requestsList.Remove(req);
                        model.Message = "No Results from search";
                    }
                }
            }

            model.requests = requestsList.AsEnumerable();

            return View(model);
        }

    }
}
