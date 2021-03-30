using MailKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace com.system.clients
{
    public interface IEmailSearchClient
    {
        IEmailSearchClient Authenticate(string emailAddress, string password);

        void Logout();

        Task<IList<UniqueId>> GetInboxAsync(string to = null, string re = null);

        Task<string> GetInboxMatchAsync(string to, string re, string regex = null, DateTime? utcDeliveredAfter = null);
    }
}
