using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public interface IHolidayRepository
    {
        Holiday GetHoliday(int HolidayId);
        IEnumerable<Holiday> GetAllHolidays();
        Holiday Add(Holiday holiday);
        Holiday Update(Holiday holidayChanges);
        Holiday Delete(int HolidayId);

    }
}
