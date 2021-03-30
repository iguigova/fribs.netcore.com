using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.Text;
using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace com.system.api.extensions
{
    public static class Extensions
    {
        private static readonly Regex _passwordPattern = new Regex(@"(password)=([^&]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public  static string SanitizePassword(this string text)
        {
            return _passwordPattern.Replace(text, "$1=*****");
        }

        public static HttpContent ToStringContent<TResponse>(IReturn<TResponse> request)
        {
            return new StringContent(JsonConvert.SerializeObject(request.GetDto()), Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        public static HttpContent FormUrlEncodedContent<TResponse>(IReturn<TResponse> request)
        {
            return new FormUrlEncodedContent(request.GetDto().ToStringDictionary());
        }

        public static DateTime ToDateTime(this string s)
        {
            return DateTime.Parse(s).ToUniversalTime();
        }

        public static string ToStringISO8601(this DateTime dt)
        {
            var utc = DateTime.SpecifyKind(dt, DateTimeKind.Utc); // EF is unable to store that the date is UTC. When dates are pulled from the DB, they are unspecified

            return utc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
