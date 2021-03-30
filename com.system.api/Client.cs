using System;
using ServiceStack;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Net;
using System.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using com.system.api.extensions;

namespace com.system.api
{
    public class Client : IClient
    {        
        protected HttpClient _client;
        protected HttpClient _302Client;

        protected JsonConverter[] _converters;

        protected int _retries;

        public Client(string baseUrl, Action<string, HttpResponseMessage, TimeSpan> onTime = null, Action<Exception> onException = null, params JsonConverter[] converters)
        {
            _client = InitHttpClient(baseUrl, true);
            _302Client = InitHttpClient(baseUrl, false);

            _converters = converters;

            OnTime = onTime;
            OnException = onException;
        }

        private HttpClient InitHttpClient(string baseUrl, bool allowAutoRedirect)
        {
            var client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = allowAutoRedirect }) { BaseAddress = new Uri(baseUrl) };

            client.DefaultRequestHeaders.Add(HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json);
            //client.DefaultRequestHeaders.Add(HttpRequestHeader.UserAgent.ToString(), "LogonLabs/1.1+ (https://logonlabs.com/)");

            return client;
        }

        protected void ResetDefaultRequestHeader(HttpClient client, string name, string value = null)
        {
            if (client.DefaultRequestHeaders.Contains(name))
            {
                client.DefaultRequestHeaders.Remove(name);
            }

            if (!string.IsNullOrEmpty(value))
            {
                client.DefaultRequestHeaders.Add(name, value);
            }
        }

        public void ResetDefaultRequestHeader(string name, string value = null)
        {
            ResetDefaultRequestHeader(_client, name, value);
            ResetDefaultRequestHeader(_302Client, name, value);
        }

        public (HttpResponseMessage, TResponse) Post<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return PostAsync(request, onException, content, allowAutoRedirect, maxNumRetries, delayRetriesInSec).Result;
        }

        public (HttpResponseMessage, TResponse) Put<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return PutAsync(request, onException, content, allowAutoRedirect, maxNumRetries, delayRetriesInSec).Result;
        }

        public (HttpResponseMessage, TResponse) Patch<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return PatchAsync(request, onException, content, allowAutoRedirect, maxNumRetries, delayRetriesInSec).Result;
        }

        public (HttpResponseMessage, TResponse) Get<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return GetAsync(request, onException, allowAutoRedirect, maxNumRetries, delayRetriesInSec).Result;
        }

        public (HttpResponseMessage, TResponse) Delete<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return DeleteAsync(request, onException, allowAutoRedirect, maxNumRetries, delayRetriesInSec).Result;
        }

        public Task<(HttpResponseMessage, TResponse)> PostAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return Log(() => (allowAutoRedirect ? _client : _302Client).PostAsync(request.ToPostUrl(), content?.Invoke(request) ?? extensions.Extensions.ToStringContent(request)), request, onException, maxNumRetries, delayRetriesInSec);
        }

        public Task<(HttpResponseMessage, TResponse)> PutAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return Log(() => (allowAutoRedirect ? _client : _302Client).PutAsync(request.ToPutUrl(), content?.Invoke(request) ?? extensions.Extensions.ToStringContent(request)), request, onException, maxNumRetries, delayRetriesInSec);
        }

        public Task<(HttpResponseMessage, TResponse)> PatchAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return Log(() => (allowAutoRedirect ? _client : _302Client).PatchAsync(request.ToUrl(HttpMethod.Patch.ToString()), content?.Invoke(request) ?? extensions.Extensions.ToStringContent(request)), request, onException, maxNumRetries, delayRetriesInSec);
        }

        public Task<(HttpResponseMessage, TResponse)> GetAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return Log(() => (allowAutoRedirect ? _client : _302Client).GetAsync(request.ToGetUrl()), request, onException, maxNumRetries, delayRetriesInSec);
        }

        public Task<(HttpResponseMessage, TResponse)> DeleteAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            return Log(() => (allowAutoRedirect ? _client : _302Client).DeleteAsync(request.ToDeleteUrl()), request, onException, maxNumRetries, delayRetriesInSec);
        }

        public Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri requestUrl, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0)
        {
            return Extensions.Retry(() => (allowAutoRedirect ? _client : _302Client).SendAsync(new HttpRequestMessage(httpMethod, requestUrl)), (httpResponseMessage) => { return HandleException(httpResponseMessage); }, maxNumRetries, delayRetriesInSec);
        }

        public Action<Exception> OnException { get; set; }
        public Action<string, HttpResponseMessage, TimeSpan> OnTime { get; set; }

        private Task<(HttpResponseMessage, TResponse)> Log<TResponse>(Func<Task<HttpResponseMessage>> call, IReturn<TResponse> request, Action<WebServiceException> onException = null, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class
        {
            var dto = request.GetDto().GetType();
            var timed = JsonConvert.SerializeObject(new { name = dto.Name, dto, request });

            var stopwatch = Stopwatch.StartNew();

            return call
                .Retry((httpResponseMessage) => { return HandleException(httpResponseMessage, onException); }, maxNumRetries, delayRetriesInSec)
                .Time(stopwatch, (httpResponseMessage, elapsed) => { OnTime?.Invoke(timed, httpResponseMessage, elapsed); })
                .ContinueWith<(HttpResponseMessage, TResponse)>((task) =>
                {
                    var httpResponseMessage = task.Result;
                    var responseBody = httpResponseMessage.Content.ReadAsStringAsync().Result;

                    if (httpResponseMessage.StatusCode == HttpStatusCode.Found // 302 Redirected
                        || (httpResponseMessage.StatusCode == HttpStatusCode.OK && responseBody.Contains("<html")))
                    {
                        return (httpResponseMessage, default);
                    }

                    HandleException(httpResponseMessage, onException, responseBody);

                    return (httpResponseMessage, JsonConvert.DeserializeObject<TResponse>(responseBody, _converters));
                });
        }

        private bool HandleException(HttpResponseMessage httpResponseMessage, Action<WebServiceException> onException = null, string responseBody = null)
        {
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                responseBody ??= httpResponseMessage.Content.ReadAsStringAsync().Result;

                var ex = new WebServiceException(httpResponseMessage.ReasonPhrase)
                {
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    ResponseBody = responseBody
                };

                ex.Data.Add("request", httpResponseMessage.RequestMessage.SerializeToString().SanitizePassword());
                ex.Data.Add("response", responseBody.SerializeToString());
                ex.Data.Add("httpResponseMessage", httpResponseMessage);

                (onException ?? OnException)?.Invoke(ex);

                return true;
            }

            return false;
        }
    }
}
