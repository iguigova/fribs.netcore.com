using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.system.clients
{
    // TODO-IG: Make it async and run it as retryable task

    public class ImapGmailSearchClient : IEmailSearchClient, IDisposable
    {
        protected ImapClient _client;

        protected const string _host = "imap.gmail.com";
        protected const int _port = 993; 
        protected const bool _useSsl = true;

        public ImapGmailSearchClient()
        {
            _client = new ImapClient
            {
                ServerCertificateValidationCallback = (s, c, h, e) => true
            };

            _client.AuthenticationMechanisms.Remove("XOAUTH2");
        }

        public void Logout()
        {
            if (_client.IsConnected)
            {
                if (_client.Inbox.IsOpen)
                {
                    _client.Inbox.Close();
                }

                _client.Disconnect(true);
            }
        }

        public void ReOpen(IMailFolder folder, FolderAccess folderAccess)
        {
            if (folder.IsOpen)
            {
                folder.Close();
            }

            folder.Open(folderAccess);
        }

        public IEmailSearchClient Authenticate(string emailAddress, string password)
        {
            Logout();

            if (!_client.IsConnected)
            {
                _client.Connect(_host, _port, _useSsl);
            }

            _client.Authenticate(emailAddress, password);

            return this;
        }

        public async Task<IList<UniqueId>> GetInboxAsync(string to = null, string re = null)
        {
            if (!_client.IsAuthenticated)
            {
                throw new UnauthorizedAccessException();
            }

            // NOTE: An open IMailFolder does not seem to be refreshed with the latest messages
            //if (!_client.Inbox.IsOpen)
            //{
            //    _client.Inbox.Open(FolderAccess.ReadOnly);
            //}

            ReOpen(_client.Inbox, FolderAccess.ReadOnly);

            var query = new SearchQuery();

            if (!string.IsNullOrEmpty(to))
            {
                query = SearchQuery.And(query, SearchQuery.ToContains(to));
            }

            if (!string.IsNullOrEmpty(re))
            {
                query = SearchQuery.And(query, SearchQuery.SubjectContains(re));
            }

            //if (date != null)
            //{
            //    // https://github.com/jstedfast/MimeKit/issues/502
            //    query = SearchQuery.And(query, SearchQuery.DeliveredAfter(date.Value));
            //}

            var uids = await _client.Inbox.SearchAsync(query);

            return uids;
        }

        public async Task<string> GetInboxMatchAsync(string to, string re, string regex = null, DateTime? utcDeliveredAfter = null)
        {
            var uid = (await GetInboxAsync(to, re)).LastOrDefault();

            // NOTE: Use the following snippet for debugging purposes
            //if (!uid.IsValid)
            //{
            //    if (_client.Inbox.Count > 0)
            //    {
            //        // fetch the UIDs of the newest 5 messages
            //        var  index = Math.Max(_client.Inbox.Count - 1, 0);
            //        var envelopes = _client.Inbox.Fetch(index, -1, MessageSummaryItems.Envelope).Select(s => s.Envelope);                    
            //    }
            //}

            if (uid.IsValid)
            {
                try
                {
                    var message = await _client.Inbox.GetMessageAsync(uid);

                    if (message != null && message.Date.UtcDateTime.CompareTo(utcDeliveredAfter) >= 0)
                    {
                        if (regex != null)
                        {
                            var match = new Regex(regex).Match(message.Body.ToString());

                            if (match.Success)
                            {
                                return match.Value;
                            }
                        }

                        return uid.ToString();
                    }
                }
                catch { }
            }

            return null;
        }

        public void Dispose()
        {
            Logout();

            _client.Dispose();
        }
    }
}
