using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace com.system.api.tests
{
    [TestClass]
    public class Core  // https://pdsa.com/BlogPosts/04-UnitTesting-Attributes.pdf, https://pdsa.com/BlogPosts/03-UnitTesting-InitCleanup.pdf, https://blog.fairwaytech.com/add-attributes-to-unit-tests
    {
        public static TestContext AssemblyTestContext { get; set; }
        public static TestContext ClassTestContext { get; set; }

        public TestContext TestContext { get; set; }  // https://pdsa.com/BlogPosts/02-UnitTesting-Configuration.pdf

        protected Logger _logger;

        [TestInitialize]
        public void Init()
        {
            TestInitialize();
        }

        protected virtual void TestInitialize()
        {
            Config(TestContext);

            _logger = new Logger(this[VERBOSITY], Formatting, (log, message) =>
            {
                return (log != Log.Alert || this[ALERTON] == null || message.Any(s => this[ALERTON].Contains(s.ToString())));
            });

            _logger.Write(Log.Timestamp, TestContext.TestName);
            _logger.Write(Log.Context, TestContext.Properties, AppSettings.Properties.ToDictionary(s => s.Key, s => s.Value?.ToString()));
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
            try
            {
                Cleanup(TestContext.Properties[CLEANUP_ACTIONS] as Stack<Action>);
            }
            finally
            {
                TestContext.WriteLine(_logger.Dump());
            }
        }

        protected static void Cleanup(Stack<Action> cleanupactions)
        {
            while (cleanupactions?.Count > 0)
            {
                cleanupactions.Pop().Invoke();
            }
        }

        protected virtual void Cleanup(Action action, params object[] options)
        {
            (TestContext.Properties[CLEANUP_ACTIONS] as Stack<Action>).Push(() =>
            {
                action?.Invoke();
            });
        }

        public string this[string name]
        {
            get
            {
                name = GetName(name);                
                return ReadFromConfig(name)?.ToString();
            }

            set
            {
                name = GetName(name);
                AddToConfig(name, value, GetTestContext(name));
            }
        }

        protected virtual object ReadFromConfig(string name)
        {
            return TestContext.Properties.Contains(name) ? TestContext.Properties[name] : ClassTestContext?.Properties.Contains(name) ?? false ? ClassTestContext.Properties[name] : AssemblyTestContext?.Properties.Contains(name) ?? false ? AssemblyTestContext.Properties[name] : AppSettings.Properties.ContainsKey(name) ? AppSettings.Properties[name] : null;
        }

        protected virtual void AddToConfig(string name, object value, IEnumerable<TestContext> contexts = null)
        {
            if (contexts != null)
            {
                foreach (var context in contexts.Where(s => s != null))
                {
                    if (context.Properties.Contains(name))
                    {
                        context.Properties[name] = value;
                    }
                    else
                    {
                        context.Properties.Add(name, value);
                    }
                }
            } 
            else
            {
                AppSettings.Append(name, value);
            }
        }

        protected virtual IEnumerable<TestContext> GetTestContext(string name)
        {
            return new List<TestContext> { TestContext };
        }

        protected virtual string GetName(string name)
        {
            return name;
        }

        public static Formatting Formatting { get { return AppSettings.Get(FORMATTING, (val) => { return val?.Contains(Formatting.Indented.ToString()) ?? false ? Formatting.Indented : Formatting.None; }); } }
        public static int RepeatCount { get { return AppSettings.Get(REPEAT_COUNT, 1); } }

        protected const string VERBOSITY = "verbosity";
        protected const string FORMATTING = "formatting";
        protected const string ALERTON = "alertOn";
        protected const string REPEAT_COUNT = "repeat_count";

        protected const string SESSION_TOKEN = "session_token";
        protected string SESSION_TOKEN_HEADER = "X-Session-Token";
        protected string OPERATION_ID_HEADER = "X-Operation-Id";

        protected const string CURRENT_USER = "current_user";
        protected const string CLEANUP_ACTIONS = "cleanup_actions";

        protected const string RANDOM = "random";
        protected const string INVALID = "invalid";

        protected const string GUID = "8482ab66-a10f-4acd-8f15-6a2e035c1b97"; //Guid.NewGuid().ToString("N");
        protected const string OBJECTID = "60396c404c89b5840593363a";

        protected virtual string emailstamp { get { return $"{RANDOM}+{DateTime.UtcNow.Ticks}@mailinator.com"; } } 
        protected virtual string namestamp { get { return $"{TestContext.TestName/*.Split('_')[0]*/}_{DateTime.UtcNow.Ticks}.com"; } }
        protected virtual string textstamp { get { return Guid.NewGuid().ToString("n").Substring(0, 8); } }  // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
        protected virtual string urlstamp { get { return $"https://{textstamp}.com"; } }

        protected virtual void Config(TestContext context)
        {
            context.Properties[CLEANUP_ACTIONS] = new Stack<Action>(); // LIFO
        }

        protected const string VALIDATION_ERROR = "validation_error";
        protected const string API_ERROR = "api_error";
        protected const string UNAUTHORIZED_ERROR = "unauthorized_error";

        protected virtual Action<WebServiceException> Is(HttpStatusCode statusCode, string errorCode = null)
        {
            return (ex) =>
            {
                OnException(ex);

                Assert.IsTrue(ex.StatusCode == (int)statusCode);
                Assert.IsTrue(ex.ResponseBody.Contains(errorCode ?? string.Empty/*"_error"*/));
            };
        }

        protected virtual void OnException(Exception ex)
        {
            var httpResponseMessage = ex.Data.PopValueByType<HttpResponseMessage>();

            var sessionToken = SerializeHeader(httpResponseMessage.RequestMessage.Headers, SESSION_TOKEN_HEADER);

            var operationId = SerializeHeader(httpResponseMessage.Headers, OPERATION_ID_HEADER);

            _logger.Write(Log.Exception, ex.Message, ex.Data, operationId, sessionToken, httpResponseMessage.StatusCode);
        }

        protected virtual string SerializeHeader(System.Net.Http.Headers.HttpHeaders headers, string name)
        {
            var value = headers?.Contains(name) ?? false ? string.Join(",", headers.GetValues(name)) : null;

            return $"{name}: {value}";
        }

        protected virtual void OnTime(string timed, HttpResponseMessage httpResponseMessage, TimeSpan time)
        {
            var sessionToken = SerializeHeader(httpResponseMessage.RequestMessage.Headers, SESSION_TOKEN_HEADER);

            var operationId = SerializeHeader(httpResponseMessage.Headers, OPERATION_ID_HEADER);

            var operation = JsonConvert.DeserializeAnonymousType(timed, new { name = string.Empty });

            _logger.Write((time.TotalSeconds > 1) ? Log.Alert : Log.Message, time, operation.name, operationId, sessionToken, httpResponseMessage.StatusCode);
            _logger.Write(Log.Details, timed);
            _logger.Write(Log.Response, httpResponseMessage.Content.ReadAsStringAsync().Result);
        }
    }
}
