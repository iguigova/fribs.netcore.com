using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack;
using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;

namespace com.system.api.tests
{
    // https://dev.to/franndotexe/mstest-v2---new-old-kid-on-the-block

    public static class AssertionExtensions
    {
        public static For<T> For<T>(this Assert _, T instance)
        {
            return new For<T>(instance);
        }
    }

    public class For<T>
    {
        private readonly T _instanceUnderTest;

        public For(T instanceUnderTest)
        {
            _instanceUnderTest = instanceUnderTest;
        }

        public void IsTrue(Expression<Func<T, bool>> assertionExpression)
        {
            if (assertionExpression.Compile().Invoke(_instanceUnderTest)) { return; }

            throw new AssertFailedException($"Assertion failed for expression '{assertionExpression}'.");
        }
    }

    public static class HttpResponseMessageAsserts
    {
        // https://www.meziantou.net/mstest-v2-create-new-asserts.htm, https://github.com/Microsoft/testfx-docs/blob/master/RFCs/002-Framework-Extensibility-Custom-Assertions.md

        public static (HttpResponseMessage, TResponse) IsRedirect<TResponse>(this Assert _, (HttpResponseMessage, TResponse) result)
        {
            var (httpResponseMessage, _) = result;

            if (httpResponseMessage.StatusCode != HttpStatusCode.Redirect || (httpResponseMessage.Headers.Location == null))
            {
                throw new AssertFailedException($"Returned {httpResponseMessage.StatusCode} {httpResponseMessage.Headers.Location}");
            }

            return result;
        }

        public static (HttpResponseMessage, TResponse) IsSuccess<TResponse>(this Assert _, (HttpResponseMessage, TResponse) result, Action<TResponse> test = null)
        {
            var (httpResponseMessage, response) = result;

            if (!httpResponseMessage.IsSuccessStatusCode || (response?.ToJson().Contains("error") ?? false))
            {
                throw new AssertFailedException($"Returned {httpResponseMessage.StatusCode} {response.ToJson()}");
            }

            test?.Invoke(response);

            return result;
        }

        public static (HttpResponseMessage, TResponse) HasError<TResponse>(this Assert _, (HttpResponseMessage, TResponse) result)
        {
            var (httpResponseMessage, response) = result;

            if (httpResponseMessage.IsSuccessStatusCode || !(response?.ToJson().Contains("error") ?? false))
            {
                throw new AssertFailedException($"Returned {httpResponseMessage.StatusCode} {response.ToJson()}");
            }

            return result;
        }
    }
}
