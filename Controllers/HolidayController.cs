using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SimpleSchedule.Models;
using SimpleSchedule.Utilities;
using SimpleSchedule.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Controllers
{
    public class HolidayController : Controller
    {
        private readonly IHolidayRepository holidayRepository;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IRequestRepository requestRepository;
        private readonly IEmailSender emailSender;

        public HolidayController(IHolidayRepository holidayRepository, UserManager<ApplicationUser> userManager, IRequestRepository requestRepository, IEmailSender emailSender)
        {
            this.holidayRepository = holidayRepository;
            this.userManager = userManager;
            this.requestRepository = requestRepository;
            this.emailSender = emailSender;
        }

        private async Task<int> newHolidayAdjustment(DateTime newHoliday, UserManager<ApplicationUser> userManager, IRequestRepository requestRepository, IEmailSender emailSender)
        {
            int changes = 0;
            foreach (var req in requestRepository.GetAllRequestsAdmin().ToList())
            {
                if (req.StartDate <= newHoliday && req.EndDate >= newHoliday)
                {
                    var user = await userManager.FindByIdAsync(req.ApplicationUserID);
                    user.VacationDaysLeft++;
                    user.VacationDaysUsed--;
                    var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "Good news! A new holiday on " + newHoliday.ToShortDateString() + " has been added during one of your Time-Off Requests. Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);
                }
            }
            return changes;
        }

        private async Task<int> deleteHolidayAdjustment(DateTime oldHoliday, UserManager<ApplicationUser> userManager, IRequestRepository requestRepository, IEmailSender emailSender)
        {
            int changes = 0;
            foreach (var req in requestRepository.GetAllRequestsAdmin().ToList())
            {
                if (req.StartDate <= oldHoliday && req.EndDate >= oldHoliday)
                {
                    var user = await userManager.FindByIdAsync(req.ApplicationUserID);
                    user.VacationDaysLeft--;
                    user.VacationDaysUsed++;
                    var currentUserMessage = new Message(new string[] { user.Email }, "Your New Time Off Summary", "Unfortunately a holiday on " + oldHoliday.ToShortDateString() + " has been deleted during one of your Time-Off Requests. Your new summary is below.<br><br>Vacation Days Left: " + user.VacationDaysLeft + "<br>Vacation Days Used: " + user.VacationDaysUsed + "<br>Sick Days Left: " + user.SickDaysLeft + "<br>Sick Days Used: " + user.SickDaysUsed, "#", "");
                    await emailSender.SendEmailAsync(currentUserMessage);
                }
            }
            return changes;
        }

        public IActionResult Index()
        {
            HolidayIndexViewModel model = new HolidayIndexViewModel
            {
                holidays = holidayRepository.GetAllHolidays()
            };
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ViewResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Holiday holiday)
        {
            if (ModelState.IsValid)
            {
                if (holiday.DateOfHoliday.DayOfWeek != DayOfWeek.Saturday && holiday.DateOfHoliday.DayOfWeek != DayOfWeek.Sunday)
                {
                    await newHolidayAdjustment(holiday.DateOfHoliday, userManager, requestRepository, emailSender);
                }
                Holiday newHoliday = holidayRepository.Add(holiday);
                return RedirectToAction("index");
            }
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ViewResult Edit(int Id)
        {
            Holiday holiday = holidayRepository.GetHoliday(Id);
            EditHolidayViewModel model = new EditHolidayViewModel
            {
                DateOfHoliday = holiday.DateOfHoliday,
                HolidayID = holiday.HolidayID,
                NameOfHoliday = holiday.NameOfHoliday,
                PreviousDate = holiday.DateOfHoliday
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(EditHolidayViewModel holidayChange)
        {
            if (ModelState.IsValid)
            {
                if (holidayChange.DateOfHoliday != holidayChange.PreviousDate)
                {
                    if (holidayChange.PreviousDate.DayOfWeek != DayOfWeek.Saturday && holidayChange.PreviousDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await deleteHolidayAdjustment(holidayChange.PreviousDate, userManager, requestRepository, emailSender);
                    }
                    if (holidayChange.DateOfHoliday.DayOfWeek != DayOfWeek.Saturday && holidayChange.DateOfHoliday.DayOfWeek != DayOfWeek.Sunday)
                    {
                        await newHolidayAdjustment(holidayChange.DateOfHoliday, userManager, requestRepository, emailSender);
                    }
                }
                Holiday holiday = holidayRepository.GetHoliday(holidayChange.HolidayID);
                holiday.DateOfHoliday = holidayChange.DateOfHoliday;
                holiday.NameOfHoliday = holidayChange.NameOfHoliday;
                holidayRepository.Update(holiday);
                return RedirectToAction("index");
            }
            return View(holidayChange);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int Id)
        {
            DateTime holidayDate = holidayRepository.GetHoliday(Id).DateOfHoliday;
            if (holidayDate.DayOfWeek != DayOfWeek.Saturday && holidayDate.DayOfWeek != DayOfWeek.Sunday)
            {
                await deleteHolidayAdjustment(holidayDate, userManager, requestRepository, emailSender);
            }
            holidayRepository.Delete(Id);
            return RedirectToAction("index");
        }
    }
}
