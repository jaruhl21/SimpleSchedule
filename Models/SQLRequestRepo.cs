using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public class SQLRequestRepo : IRequestRepository
    {
        private readonly AppDbContext context;
        private DateTime current = DateTime.Today;

        public SQLRequestRepo(AppDbContext context)
        {
            this.context = context;
        }

        public Request Add(Request request)
        {
            context.Requests.Add(request);
            context.SaveChanges();
            return request;
        }

        public Request Delete(int RequestId)
        {
            Request request = context.Requests.Find(RequestId);
            if (request != null)
            {
                context.Requests.Remove(request);
                context.SaveChanges();
            }
            return request;

        }

        public IEnumerable<Request> GetAllRequests(string UserId)
        {
            return context.Requests.Where(r => r.ApplicationUserID == UserId && r.EndDate >= current).OrderBy(r => r.StartDate);
        }

        public IEnumerable<Request> GetRequestsAdmin(string UserId)
        {
            return context.Requests.Where(r => r.ApplicationUserID == UserId).OrderBy(r => r.StartDate);
        }

        public IEnumerable<Request> GetAllRequestsAdmin()
        {
            return context.Requests.OrderBy(r => r.StartDate);
        }

        public IEnumerable<Request> GetOthersRequests(string UserId)
        {
            return context.Requests.Where(r => r.ApplicationUserID != UserId && r.EndDate >= current).OrderBy(r => r.StartDate);
        }

        public Request GetRequest(int RequestId)
        {
            return context.Requests.Find(RequestId);
        }

        public Request Update(Request requestChanges)
        {
            var request = context.Requests.Attach(requestChanges);
            request.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            context.SaveChanges();
            return requestChanges;
        }
    }
}
