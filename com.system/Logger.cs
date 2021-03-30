using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.system
{
    public enum Log
    {
        Timestamp,
        Exception,
        Alert,
        Context,
        Message, 
        Details, 
        Response
    }

    public class Logger
    {
        protected Log[] _verbosity;
        protected Formatting _formatting;

        protected Queue<(Log, object[])> _log { get; private set; }

        protected Func<Log, object[], bool> _onAllow;
        protected Action<string> _onEnqueue;

        public Logger(string verbosity, Formatting formatting, Func<Log, object[], bool> onAllow = null, Action<string> onEnqueue = null)
        {
            _verbosity = ParseVerbosity(verbosity);
            _formatting = formatting;

            _log = new Queue<(Log, object[])>();

            _onAllow = onAllow;
            _onEnqueue = onEnqueue;
        }

        protected Log[] ParseVerbosity(string verbosity)
        {
            if (!string.IsNullOrEmpty(verbosity))
            {
                return verbosity.Split(',').Select(s => Enum.Parse<Log>(s)).ToArray();
            }

            return new Log[] { Log.Exception };
        }

        protected bool Allow(Log log, params object[] message)
        {
            return _verbosity.Contains(log) && (_onAllow?.Invoke(log, message) ?? true);
        }

        public void Write(Log log, params object[] message)
        {
            if (Allow(log, message))
            {
                if (_onEnqueue != null)
                {
                    _onEnqueue?.Invoke(JsonConvert.SerializeObject(ToObj(log, message), _formatting));
                }
                else
                {
                    _log.Enqueue((log, new object[] { DateTime.UtcNow }.Concat(message).ToArray()));
                }                
            }
        }

        protected dynamic ToObj((Log, object[]) record)
        {
            return ToObj(record.Item1, record.Item2);
        }

        protected dynamic ToObj(Log log, object[] message)
        {
            return log switch
            {
                Log.Alert => new { Alert = message },
                Log.Details => new { Details = message },
                Log.Response => new { Response = message },
                Log.Exception => new { Exception = message },
                Log.Message => new { Message = message },
                Log.Context => new { Context = message },
                Log.Timestamp => new { Timestamp = message },
                _ => new { Unspecified = message },
            };
        }

        public bool Has(Log log)
        {
            return _log.Any(s => s.Item1 == log);
        }

        public string Dump(bool reset = false)
        {
            var logs = _log.Select(s => ToObj(s));

            if (reset)
            {
                _log = new Queue<(Log, object[])>();
            }

            return JsonConvert.SerializeObject(logs, _formatting);
        }
    }
}
