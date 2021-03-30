using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace com.system
{
    public class AppSettings
    {
        //protected static Dictionary<string, string> _appSettings;
        protected static Context _appSettings;

        public static IDictionary<string, object> Properties { get { return _appSettings.Properties; } }

        public static string Get(string name)
        {
            _appSettings ??= new Context().Append(Load());

            return _appSettings[name];
        }

        public static T Get<T>(string name, T defaultValue = default, char separator = ',', bool isRequired = false)
        {
            _appSettings ??= new Context().Append(Load());

            //return _appSettings.ContainsKey(key) ? _appSettings[key] : defualtValue;
            return _appSettings.Get(name, defaultValue, separator, isRequired);
        }

        public static T Get<T>(string name, Func<string, T> convert)
        {
            return convert.Invoke(Get(name));
        }

        public static T Get<T>(string name, Func<object, T> convert)
        {
            return convert.Invoke(_appSettings.Properties[name]);
        }

        public static void Append<T>(string name, T value)
        {
            _appSettings.Append(name, value);
        }

        public static Dictionary<string, string> Load()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()//env vars ovveride appsettings.json
                .Build();

#if LOCAL
           return config.GetSection("Local").GetChildren().ToDictionary(x => x.Key, x => x.Value);
#elif DEVLAB
            return config.GetSection("Devlab").GetChildren().ToDictionary(x => x.Key, x => x.Value);
#elif STABLE
            return config.GetSection("Stable").GetChildren().ToDictionary(x => x.Key, x => x.Value);
#elif IG
            return config.GetSection("IG").GetChildren().ToDictionary(x => x.Key, x => x.Value);
#elif ZEE
            return config.GetSection("ZEE").GetChildren().ToDictionary(x => x.Key, x => x.Value);
#else
			return config.GetChildren().ToDictionary(x => x.Key, x => x.Value);
#endif          
        }
    }
}
