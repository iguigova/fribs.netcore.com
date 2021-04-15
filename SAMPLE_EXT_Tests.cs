using BreadButter.Model;
using BreadButter.Management.Model;
using com.system;
using com.system.api;
using com.system.api.tests;
using com.system.clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using ServiceStack;

namespace com.mgmt.api.tests
{
    [TestClass]
    public class Tests : Core
    {
        protected IClient _gw;
        protected IClient _mgmt;

        public Tests()
        {
            _gw = new Client(AppSettings.Get(GW_API_URL), OnTime, OnException, new gw.api.model.ErrorResponseConverter(), new gw.api.model.ValidationExceptionResponseConverter());
            _mgmt = new Client(AppSettings.Get(MGMT_API_URL), OnTime, OnException, new mgmt.api.model.ErrorResponseConverter(), new mgmt.api.model.ValidationExceptionResponseConverter());
        }

        protected IEmailSearchClient _emailSearchClient;
        protected IEmailSearchClient EmailSearchClient { get { return _emailSearchClient ??= new ImapGmailSearchClient().Authenticate(this[EMAILSEARCH_EMAIL_ADDRESS], this[EMAILSEARCH_PASSWORD]); } }

        protected override string Emailstamp { get { return this[EMAIL_ADDRESS].Replace("@", $"{ DateTime.UtcNow.Ticks}@"); } }
        
        protected const string MY_IP_ADDRESS = "my_ip_address";

        protected const string GW_API_URL = "IdPx_ApiUrl";
        protected const string GW_ID = "IdPx_Id";
        protected const string GW_SECRET = "IdPx_Secret";

        protected const string WEBAPP_URL = "WebApp_Url";

        protected const string MGMT_API_URL = "Mgmt_ApiUrl";
        protected const string MGMT_SECRET = "Mgmt_Secret";
        protected const string MGMT_CALLBACK_URL = "app_callback_url";
        protected const string MGMT_CALLBACK_URL_DEFAULT = "https://google.ca/";
        protected const string MGMT_DESTINATION_URL = "app_destination_url";
        protected const string MGMT_DESTINATION_URL_DEFAULT = "https://google.ca/";

        protected const string MGMT_APP_DEVICE_ID = "Mgmt_AppDeviceId";
        protected const string MGMT_APP_ID = "Mgmt_AppId";
        protected const string MGMT_APP_SECRET = "Mgmt_AppSecret";
        protected const string MGMT_APP_SECRET_HEADER = "x-app-secret";

        protected const string EMAIL_ADDRESS = "Mgmt_EmailAddress";
        protected const string EMAIL_ADDRESS2 = "Mgmt_EmailAddress2";
        protected const string EMAIL_ADDRESS3 = "Mgmt_EmailAddress3";
        protected const string PASSWORD = "Mgmt_Password";
        protected const string PIN = "pin";

        protected const string EMAILSEARCH_EMAIL_ADDRESS = "Gmail_EmailAddress";
        protected const string EMAILSEARCH_PASSWORD = "Gmail_AppPassword";

        protected const string EMAILSEARCH_PIN_RE = "Email Confirmation Instructions";
        protected const string EMAILSEARCH_PIN_REGEX = @"(^|\D)(\d{4})($|\D)";
        protected const string EMAILSEARCH_PSWD_RE = "LogonLabs Reset Password";
        protected const string EMAILSEARCH_PSWD_REGEX = @"(^|\D)(\d{6})($|\D)";

        protected const string CURRENT_USER = "current_user";

        protected const string REUSE_SESSION_TOKENS = "reuse_session_tokens";
        protected const string REUSE_RESOURCES = "reuse_resources";

        protected const string TEST_CATEGORY_PERFORMANCE = "performance";
        protected const string TEST_CATEGORY_SMOKE = "smoke";

        protected override void Config(TestContext context)
        {
            base.Config(context);

            AppSettings.Append(CLEANUP_ACTIONS, new Stack<Action>()); // LIFO
        }

        protected override void TestInitialize()
        {
            base.TestInitialize();

            Authenticate();

            LazyLoadAction = LazyLoadActionWithContext(_currentUser);
        }

        protected override void LazyLoad(string name)
        {
            base.LazyLoad(name);

            if (name == MY_IP_ADDRESS)
            {
                LazyLoadAction(() => this[MY_IP_ADDRESS] ??= _mgmt.SendAsync(HttpMethod.Get, new Uri("https://api.ipify.org")).Result.Content.ReadAsStringAsync().Result);
            }
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Cleanup(AppSettings.Get(CLEANUP_ACTIONS, (value) => value as Stack<Action>));
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup(); 
            
            Logout();
        }

        protected override void Cleanup(Action action, params object[] options)
        {
            var emailAddress = _currentUser.EmailAddress;
            var password = _currentUser.Password;
            var logger = _logger;

            var delayed = (options != null && options.Length > 0) && (bool)options[0];
            var cleanupactions = (delayed ? AppSettings.Get(CLEANUP_ACTIONS, (value) => value as Stack<Action>) : TestContext.Properties[CLEANUP_ACTIONS]) as Stack<Action>;

            cleanupactions.Push(() =>
            {
                try
                {
                    Authenticate(emailAddress, password);  // encloses current emailAddress, password
                    action?.Invoke();

                    //System.Diagnostics.Trace.WriteLine(logger.Dump(reset: false));  // visible when debugging in the output window by selecting Debug from the dropdown menu
                }
                catch { } // TOOD: Proceed with the rest of the cleanup.... but is this the best we can do?
            });
        }

        protected Action<Action> LazyLoadAction;
        protected Action<Action> LazyLoadActionWithContext((string EmailAddress, string Password, string AppId, string AppDeviceId) lazyLoadContext)
        {
            return (action) =>
            {
                var (EmailAddress, Password, AppId, AppDeviceId) = _currentUser;

                if (lazyLoadContext.EmailAddress != _currentUser.EmailAddress)
                {
                    Authenticate(lazyLoadContext.EmailAddress, lazyLoadContext.Password, lazyLoadContext.AppId, lazyLoadContext.AppDeviceId);
                }

                action();

                if (EmailAddress == null)
                {
                    Logout();
                }
                else if (EmailAddress != _currentUser.EmailAddress)
                {
                    Authenticate(EmailAddress, Password, AppId, AppDeviceId);
                }
            };
        }

        protected bool ReuseSessionTokens { get { return AppSettings.Get(REUSE_SESSION_TOKENS, false); } }

        protected bool ReuseResources { get { return AppSettings.Get(REUSE_RESOURCES, false); } }

        protected virtual IEnumerable<string> ReuseResourcesProps { get { return new List<string>() { MY_IP_ADDRESS }; } }

        protected override IEnumerable<TestContext> GetTestContext(string name)
        {
            if (ReuseResources)
            {
                foreach (var prop in ReuseResourcesProps)
                {
                    if (name.Contains(prop))
                    {
                        return null; //i.e. stored in AppSettings
                    }
                }
            }

            return base.GetTestContext(name);
        }

        protected string GetReuseKey(params object[] keys)
        {
            var key = string.Empty;

            foreach(var k in keys)
            {
                key += k?.ToString();
            }

            return key;
        }

        protected string ReuseValue(string value, params object[] keys)
        {
            if (ReuseSessionTokens)
            {
                //this[GetPersistedSessionTokenKey(emailAddress, password, appId, deviceId)] = sessionToken;
                AddToConfig(GetReuseKey(keys), value);
            }

            return value;
        } 

        protected virtual string GetPinFromInbox(string emailAddress)
        {
            var emailDeliveredAfter = DateTime.UtcNow.AddMinutes(-1);

            return Extensions.Retry(() => EmailSearchClient.GetInboxMatchAsync(emailAddress, EMAILSEARCH_PIN_RE, EMAILSEARCH_PIN_REGEX, emailDeliveredAfter), (match) => match == null, 20, 10).Result?.Replace(">", "").Replace("<", "").PadLeft(4, '0');
        }

        protected (string EmailAddress, string Password, string AppId, string AppDeviceId) _currentUser
        {
            get { var currentUser = ReadFromConfig(CURRENT_USER); return currentUser == null ? (null, null, null, null) : (ValueTuple<string, string, string, string>)currentUser; }
            set { AddToConfig(CURRENT_USER, value, GetTestContext(CURRENT_USER)); }
        }

        protected virtual string GetAuthenciationToken(string emailAddress, string password, string appId = null, string deviceId = null, string pin = null, bool confirmPin = false)
        {
            var (_, startAuthenticationResponse) = Assert.That.IsOk(_gw.Post(new StartAuthentication()
            {
                app_id = appId,
                device_id = deviceId,
                email_address = emailAddress,
                auth_type = "password",
                user = new AuthUser() { pin = pin, password = password }
            }, maxNumRetries: 5), (response) => 
            {
                Assert.IsNotNull(response.authentication_token);
                Assert.IsTrue(pin == null || !response.pending_pin_confirmation);
            });

            var token = startAuthenticationResponse.authentication_token;

            if (confirmPin && startAuthenticationResponse.pending_pin_confirmation)
            {
                pin = this[PIN] = GetPinFromInbox(emailAddress);

                if (pin != null)
                {                    
                    token = Assert.That.IsOk(_gw.Post(new ConfirmUser()
                    {
                        app_id = appId,
                        device_id = deviceId,
                        email_address = emailAddress,
                        pin = pin
                    })).Item2.authentication_token;
                }
            }

            return token;
        }

        protected virtual string GetSessionToken(string emailAddress, string password, string appId = null, string deviceId = null)
        {
            if (appId == null)
            {
                appId = this[MGMT_APP_ID];
                deviceId = this[MGMT_APP_DEVICE_ID];
            }

            var reusedSessionToken = this[GetReuseKey(emailAddress, password, appId, deviceId)];
            
            if (reusedSessionToken == null)
            {
                deviceId ??= Assert.That.IsOk(_gw.Post(new RegisterDevice() { app_id = appId })).Item2.device_id;

                var (redirectLoginResponseMessage, _) = Assert.That.IsRedirect(_gw.Get(new RedirectAuthentication()
                {
                    app_id = appId,
                    authentication_token = GetAuthenciationToken(emailAddress, password, appId, deviceId)
                }, allowAutoRedirect: false));

                var authCallbackResponseMessage = _mgmt.SendAsync(HttpMethod.Get, redirectLoginResponseMessage.Headers.Location, allowAutoRedirect: false).Result;

                Assert.IsNotNull(authCallbackResponseMessage?.Headers?.Location?.Query);

                reusedSessionToken = ReuseValue(HttpUtility.ParseQueryString(authCallbackResponseMessage.Headers.Location.Query).Get("data"), emailAddress, password, appId, deviceId);
            }

            return reusedSessionToken;
        }

        protected virtual void Authenticate(string emailAddress = null, string password = null, string appId = null, string deviceId = null)
        {
            emailAddress ??= this[EMAIL_ADDRESS];
            password ??= this[PASSWORD];

            if (!string.IsNullOrEmpty(_currentUser.EmailAddress) && _currentUser.EmailAddress != emailAddress)
            {
                Logout();
            }

            if (this[SESSION_TOKEN] == null)
            {                
                this[SESSION_TOKEN] = GetSessionToken(emailAddress, password, appId, deviceId);

                _mgmt.ResetDefaultRequestHeader(SESSION_TOKEN_HEADER, this[SESSION_TOKEN]);                

                _currentUser = (emailAddress, password, appId, deviceId);
            }
        }

        protected virtual void Logout(bool invalidateSessionToken = false)
        {
            _mgmt.ResetDefaultRequestHeader(HttpHeaders.Authorization);

            if (this[SESSION_TOKEN] != null)
            {
                if (invalidateSessionToken)
                {
                    _mgmt.Delete(new Logout());
                }

                _mgmt.ResetDefaultRequestHeader(SESSION_TOKEN_HEADER);

                this[SESSION_TOKEN] = null;
            }

            _currentUser = (null, null, null, null);
        }
    }
}
