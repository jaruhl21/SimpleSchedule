using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class SQLEarlyReleaseRepo : IEarlyReleaseRepository
    {
        private readonly AppDbContext context;

        public SQLEarlyReleaseRepo(AppDbContext context)
        {
            this.context = context;
        }

        public EarlyRelease Add(EarlyRelease earlyRelease)
        {
            context.EarlyReleases.Add(earlyRelease);
            context.SaveChanges();
            return earlyRelease;
        }

        public EarlyRelease Delete(int EarlyReleaseId)
        {
            EarlyRelease earlyRelease = context.EarlyReleases.Find(EarlyReleaseId);
            if (earlyRelease != null)
            {
                context.EarlyReleases.Remove(earlyRelease);
                context.SaveChanges();
            }
            return earlyRelease;

        }

        public IEnumerable<EarlyRelease> GetAllEarlyReleases(string UserId)
        {
            return context.EarlyReleases.Where(r => r.ApplicationUserID == UserId).OrderBy(r => r.EarlyReleaseDateTime);
        }

        public IEnumerable<EarlyRelease> GetAllEarlyReleasesAdmin()
        {
            return context.EarlyReleases.OrderBy(r => r.EarlyReleaseDateTime);
        }

        public EarlyRelease GetEarlyRelease(int EarlyReleaseId)
        {
            return context.EarlyReleases.Find(EarlyReleaseId);
        }

        public EarlyRelease Update(EarlyRelease earlyReleaseChanges)
        {
            var earlyRelease = context.EarlyReleases.Attach(earlyReleaseChanges);
            earlyRelease.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            context.SaveChanges();
            return earlyReleaseChanges;
        }
    }
}
