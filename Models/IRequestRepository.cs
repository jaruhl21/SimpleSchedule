using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleSchedule.Models
{
    public interface IRequestRepository
    {
        Request GetRequest(int RequestId);
        IEnumerable<Request> GetAllRequests(string UserId);
        IEnumerable<Request> GetRequestsAdmin(string UserId);
        IEnumerable<Request> GetAllRequestsAdmin();
        IEnumerable<Request> GetOthersRequests(string UserId);
        Request Add(Request request);
        Request Update(Request requestChanges);
        Request Delete(int RequestId);
    }
}
