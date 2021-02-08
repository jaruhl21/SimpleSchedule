using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public interface IEarlyReleaseRepository
    {
        EarlyRelease GetEarlyRelease(int EarlyReleaseId);
        IEnumerable<EarlyRelease> GetAllEarlyReleases(string UserId);
        IEnumerable<EarlyRelease> GetAllEarlyReleasesAdmin();
        EarlyRelease Add(EarlyRelease earlyRelease);
        EarlyRelease Update(EarlyRelease earlyReleaseChanges);
        EarlyRelease Delete(int EarlyReleaseId);
    }
}
