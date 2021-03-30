using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.system
{
    public static class Extensions
    {
        public static async Task<T> Retry<T>(this Func<Task<T>> tasker, Func<T, bool> doRetry = null, int maxNumRetries = 0, int delayRetriesInSec = 0)
        {
            // https://docs.microsoft.com/en-us/azure/architecture/patterns/retry

            if (delayRetriesInSec > 0)
            {
                // Wait to retry the operation.
                // Consider calculating an exponential delay here and
                // using a strategy best suited for the operation and fault.
                await Task.Delay(TimeSpan.FromSeconds(delayRetriesInSec));
            }

            var s = await tasker();

            if (maxNumRetries > 0 && (doRetry?.Invoke(s) ?? true))
            {
                s = await Retry(tasker, doRetry, --maxNumRetries, delayRetriesInSec);
            }

            return s;
        }

        public static (string, T) GetKeyValueByType<T>(this IDictionary data)
        {
            foreach (DictionaryEntry dictionaryEntry in data)
            {
                if (dictionaryEntry.Value is T value)
                {
                    return (dictionaryEntry.Key.ToString(), value);
                }
            }

            return (null, default);
        }

        public static T PopValueByType<T>(this IDictionary data)
        {
            var (key, value) = GetKeyValueByType<T>(data);

            if (key != null)
            {
                data.Remove(key);
            }

            return value;
        }

        public static bool? ToBool(this string value)
        {
            return bool.TryParse(value, out bool result) ? result : (bool?)null;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> set, params T[] items)
        {
            return set.Concat(items);
        }
    }
}
