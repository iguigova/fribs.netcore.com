using ServiceStack;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.system.api
{
    public interface IClient
    {
        void ResetDefaultRequestHeader(string name, string value = null);

        (HttpResponseMessage, TResponse) Post<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        (HttpResponseMessage, TResponse) Put<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        (HttpResponseMessage, TResponse) Patch<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        (HttpResponseMessage, TResponse) Get<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        (HttpResponseMessage, TResponse) Delete<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<(HttpResponseMessage, TResponse)> PostAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<(HttpResponseMessage, TResponse)> PutAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<(HttpResponseMessage, TResponse)> PatchAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, Func<IReturn<TResponse>, HttpContent> content = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<(HttpResponseMessage, TResponse)> GetAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<(HttpResponseMessage, TResponse)> DeleteAsync<TResponse>(IReturn<TResponse> request, Action<WebServiceException> onException = null, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0) where TResponse : class;

        Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, Uri requestUrl, bool allowAutoRedirect = true, int maxNumRetries = 0, int delayRetriesInSec = 0);

        Action<Exception> OnException { get; set; }
        Action<string, HttpResponseMessage, TimeSpan> OnTime { get; set; }
    }
}
