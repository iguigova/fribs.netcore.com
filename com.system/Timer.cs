using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace com.system
{
    public static class Timer
    {
		public static T Time<T>(this Func<T> func, Action<T, TimeSpan> onTime, Action<Exception> onException = null)
		{
			var stopwatch = Stopwatch.StartNew();

			var result = default(T);

			try
			{
				result = func();
			}
			catch (Exception ex)
			{
				if (onException != null)
                {
					onException(ex);
                }
				else
                {
					throw;
                }
			}

			stopwatch.Stop();

			onTime?.Invoke(result, stopwatch.Elapsed);

			return result;
		}

        public static void Time(this Action func, Action<TimeSpan> onTime, Action<Exception> onException = null)
        {
            Time(() => { func(); return true; }, (result, elapsed) => { onTime?.Invoke(elapsed); }, onException);
        }

		public static Task<T> Time<T>(this Task<T> task, Stopwatch stopwatch, Action<T, TimeSpan> onTime, Action<Exception> onException = null)
        {
			return task.ContinueWith((t) => 
			{
				stopwatch.Stop();
				onTime?.Invoke(t.Result, stopwatch.Elapsed);
				return t.Result;
			});
        }

		//public static T Time<S, T>(this Func<T> func, S timed, Action<S, T, TimeSpan> onTime, Action<Exception> onException = null)
		//{
		//	return Time(func, (result, elapsed) => { onTime?.Invoke(timed, result, elapsed); }, onException);
		//}
	}
}
