using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace com.system
{
    public class Context
    {
        protected Dictionary<string, object> _properties;

        public Dictionary<string, object> Properties
        {
            get
            {
                return _properties ??= new Dictionary<string, object>();
            }
        }

        public Context(Dictionary<string, object> properties = null)
        {
            Append(properties);
        }

        public void Append<T>(string name, T value)
        {
            if (!Properties.ContainsKey(name))
            {
                Properties.Add(name, value);
            }
            else
            {
                Properties[name] = value;
            }
        }

        public Context Append<T>(Dictionary<string, T> properties)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    Append(property.Key, property.Value);
                }
            }

            return this;
        }

        public Context Append(string[] properties)
        {
            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i].Split("=");
                    Append(property[0], property[1]);
                }
            }

            return this;
        }
        
        protected object Get(string name)
        {
            return (Properties.ContainsKey(name)) ? Properties[name] : null;
        }

        public string this[string name]
        {
            get { return Get(name)?.ToString(); }
        }

        public T Get<T>(string name, T defaultValue = default, char separator = ',', bool isRequired = false)
        {
            var value = Get(name);

            if (value != null)
            {
                // https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value.ToString());
                }

                if (typeof(T) == typeof(string[]))
                {
                    return (T)Convert.ChangeType(value.ToString().Split(separator), typeof(T));
                }

                if (typeof(T) == typeof(List<string>))
                {
                    return (T)Convert.ChangeType(new List<string>(value.ToString().Split(separator)), typeof(T));
                }

                if (typeof(T).IsGenericType)
                {
                    return JsonConvert.DeserializeObject<T>(value.ToString());
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            if (isRequired)
            {
                throw new Exception($"Setting {name} is not defined.");
            }

            return defaultValue;
        }

        public string Dump()
        {
            return JsonConvert.SerializeObject(Properties);
        }
    }
}
