using BreadButter.Management.Model;
using com.system.api.tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack;
using System;
using System.Net.Http;

namespace com.mgmt.api.tests
{
    // https://github.com/microsoft/testfx-docs

    public static class TResponseAsserts
    {
        public static (HttpResponseMessage, TResponse) IsOk<TResponse>(this Assert assert, (HttpResponseMessage, TResponse) result, Action<TResponse> test = null)
        {
            var (httpResponseMessage, response) = assert.IsSuccess(result, test);

            if (httpResponseMessage.RequestMessage.GetDto() is IPagedRequest pagedRequest)
            {
                var r = (dynamic)response;
                
                Assert.IsTrue(r.total_items >= 0);
                Assert.IsTrue(r.total_items == (r.total_pages == 1 ? r.results.Count : r.total_items));

                Assert.IsTrue(r.total_pages > 0);
                Assert.IsTrue(r.total_pages == (r.total_items == 0 ? 1 : r.total_pages));

                Assert.IsTrue(r.results.Count == (r.total_items < r.page_size ? r.total_items : r.page_size));

                Assert.IsTrue(r.current_page == (pagedRequest.page).GetValueOrDefault(1));
                Assert.IsTrue(r.page_size == (pagedRequest.page).GetValueOrDefault(100));
            }

            return (httpResponseMessage, response);
        }
    }
}
