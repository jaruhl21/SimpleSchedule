using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class SQLHolidayRepo : IHolidayRepository
    {
        private readonly AppDbContext context;

        public SQLHolidayRepo(AppDbContext context)
        {
            this.context = context;
        }

        public Holiday Add(Holiday holiday)
        {
            context.Holidays.Add(holiday);
            context.SaveChanges();
            return holiday;
        }

        public Holiday Delete(int HolidayId)
        {
            Holiday holiday = context.Holidays.Find(HolidayId);
            if (holiday != null)
            {
                context.Holidays.Remove(holiday);
                context.SaveChanges();
            }
            return holiday;

        }

        public IEnumerable<Holiday> GetAllHolidays()
        {
            return context.Holidays.OrderBy(r => r.DateOfHoliday);
        }

        public Holiday GetHoliday(int HolidayId)
        {
            return context.Holidays.Find(HolidayId);
        }

        public Holiday Update(Holiday holidayChanges)
        {
            var holiday = context.Holidays.Attach(holidayChanges);
            holiday.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            context.SaveChanges();
            return holidayChanges;
        }
    }
}
