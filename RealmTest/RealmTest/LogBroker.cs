using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Xamarin.Essentials;

namespace RealmTest
{
    public sealed class LogBroker
    {
#if DEBUG
        private DateTime _dateLastLog;
#endif
        private static LogBroker _instance;
        public static LogBroker Instance => _instance ??= new LogBroker();

        private LogBroker()
        {
#if DEBUG
            _dateLastLog = DateTime.Now;
#endif
        }

        public void TraceError(string message,
            bool logOnMqtt = true,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if (lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
            {
                var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                message = $"[{className}.{callingMethod}:{callingFileLineNumber}] - E - {message}";
            }

            LogOnDebug(message);
        }

        public void TraceWarning(string message,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if (lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
            {
                var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                message = $"[{className}.{callingMethod}:{callingFileLineNumber}] - W - {message}";
            }

            LogOnDebug(message);
        }

        public void TraceInfo(string message,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if (lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
            {
                var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                message = $"[{className}.{callingMethod}:{callingFileLineNumber}] - I - {message}";
            }

            LogOnDebug(message);
        }

        public void TraceDebug(string message,
            bool traceDate = false,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if (lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
            {
                var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                message = $"[{className}.{callingMethod}:{callingFileLineNumber}] - D - {message}";
            }

#if DEBUG
            var dateLog = DateTime.Now;
            string dateText;
            if (traceDate)
            {
                dateText = $"[DateLog: {dateLog.TimeOfDay} - Diff: {(dateLog - _dateLastLog).TotalMilliseconds} mS";
                _dateLastLog = dateLog;
            }
            else
            {
                dateText = $"[DateLog: {dateLog.TimeOfDay}";
            }

            //message = $"{dateText} {message}";
#endif
            LogOnDebug(message);

        }

        private static void LogOnDebug(string message)
        {
            message = AddMainThreadInfo(message);
#if DEBUG
            Debug.WriteLine(message);
#else
            Console.WriteLine(message);
#endif
        }

        private static string AddMainThreadInfo(string message)
        {
            if (MainThread.IsMainThread) return $"[UI][Realm {Utils.RealmCurrentInstances}]{message}";

            ThreadPool.GetMaxThreads(out var maxTh, out _);
            ThreadPool.GetAvailableThreads(out var availableTh, out _);
            return $"[Threads {maxTh - availableTh}][Realm {Interlocked.Read(ref Utils.RealmCurrentInstances)}]{message}";
        }
    }
}