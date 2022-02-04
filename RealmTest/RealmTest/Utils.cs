using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Essentials;

namespace RealmTest
{
    public static class Utils
    {
        public static bool IsInFactoryResetModality { get; set; }
        public static long RealmCurrentInstances = 0;
        public static X509Certificate2 CaCertificate { get; set; }

        private static ConcurrentDictionary<string, int> AllocatedCurrent { get; } = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<string, int> AllocatedOld { get; set; }

        public static event EventHandler LogoutRequested;

        public static void Dealloc(string type)
        {
            if (AllocatedCurrent.ContainsKey(type))
            {
                AllocatedCurrent[type]--;

                if (AllocatedCurrent[type] == 0)
                {
                    AllocatedCurrent.TryRemove(type, out _);
                }
            }
            else
            {
                LogBroker.Instance.TraceDebug($"negative refs => {type}");
            }
        }

        public static void Alloc(string type)
        {
            if (AllocatedCurrent.ContainsKey(type))
            {
                AllocatedCurrent[type]++;
            }
            else
            {
                AllocatedCurrent.TryAdd(type, 1);
            }
        }

        public static void DumpAllocs()
        {
            GC.Collect();

            var str = $"total allocs => {AllocatedCurrent.Select(x => x.Value).Sum()}";
            LogBroker.Instance.TraceDebug(str);

            foreach (var x in AllocatedCurrent)
            {
                var key = x.Key;
                var newRefs = x.Value;
                var delta = "";

                if (AllocatedOld != null && AllocatedOld.TryGetValue(key, out int oldRefs))
                {
                    var diff = newRefs - oldRefs;
                    var perc = diff * 100 / oldRefs;
                    delta += perc + "%";
                }

                str = $"ID: {key} - REFC: {newRefs} ({delta})";
                LogBroker.Instance.TraceDebug(str);
            }

            AllocatedOld = new ConcurrentDictionary<string, int>(AllocatedCurrent);
        }

        public static bool IsMethodOnMainThread(string methodName)
        {
            if (MainThread.IsMainThread)
            {
#if DEBUG
//                Task.Run(() => Debug.WriteLine($"{methodName} is on MainThread"));
#endif
                return true;
            }
#if DEBUG
//            Task.Run(() => Debug.WriteLine($"{methodName} is NOT on MainThread. Thread id: {Environment.CurrentManagedThreadId}"));
#endif
            return false;
        }

        public static List<List<int>> GroupByContiguity(IReadOnlyList<int> source)
        {
            if (source == null)
            {
                throw new ArgumentException("source list cannot be null");
            }

            var groups = new List<List<int>>();

            for (var i = 0; i < source.Count; i++)
            {
                var curr = source[i];

                if (i == 0)
                {
                    var g = new List<int> { curr };
                    groups.Add(g);
                }
                else
                {
                    var prev = source[i - 1];
                    if (curr - prev == 1)
                    {
                        groups.Last()?.Add(curr);
                    }
                    else
                    {
                        var g = new List<int> { curr };
                        groups.Add(g);
                    }
                }
            }
            return groups;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);
            var method = sf.GetMethod();

            return $"CLASS: {method.DeclaringType} METHOD: {method.Name}";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetStackTrace()
        {
            var sb = new StringBuilder("");
            var st = new StackTrace();
            for (var i = 0; i < st.FrameCount; i++)
            {
                var sf = st.GetFrame(i);
                var method = sf.GetMethod();
                sb.Append("CLASS: ")
                    .Append(method.DeclaringType)
                    .Append(" METHOD: ")
                    .Append(method.Name)
                    .Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        /* rounds a numerical value in string format to 1 decimal digit double format */
        public static double RoundedValue(string val)
        {
            double ret = -1;

            if (val == null) return ret;

            Regex numValue = new Regex(@"-*\d+(\.\d+)?");

            var valueMatch = numValue.Match(val);

            if (valueMatch.Success)
            {
                var matchVal = valueMatch.Value;

                if (matchVal.Contains("."))
                {
                    matchVal = matchVal.Substring(0,matchVal.IndexOf(".")+2);
                }

                ret = double.Parse(matchVal, CultureInfo.InvariantCulture);
            }

            return ret;
        }

        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source.Select((x, i) => new { Index = i, Value = x })
                         .GroupBy(x => x.Index / chunkSize)
                         .Select(x => x.Select(v => v.Value).ToList())
                         .ToList();
        }

        public static double ToCelsius (double fahrenheit)
        {
            return (fahrenheit - 32) / 1.8;
        }

        public static double ToFahrenheit (double celsius)
        {
            return (celsius * 1.8) + 32;
        }

        public static double ToFahrenheitStep(double celsius)
        {
            return celsius * 1.8;
        }


        /// <summary>
        /// To be used only inside TryDispose()
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="callingMethod"></param>
        /// <param name="callingFilePath"></param>
        /// <param name="callingFileLineNumber"></param>
        public static void TraceTryDisposeException(Exception ex,
            string callingMethod,
            string callingFilePath,
            int callingFileLineNumber)
        {
            if (ex is NullReferenceException) return;

            var className = callingFilePath;

            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if(lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
                className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];

            LogBroker.Instance.TraceWarning($"Managed Exception in {className}.{callingMethod}:{callingFileLineNumber} -> {ex}");
        }

        public static void TraceException(Exception ex,
            bool logOnMqtt = true,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var className = callingFilePath;

            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if(lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
                className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];

            LogBroker.Instance.TraceWarning($"Managed Exception in {className}.{callingMethod}:{callingFileLineNumber} -> {ex}");
        }

        public static void TraceUnmanagedException(Exception ex,
            bool logOnMqtt = true,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var className = callingFilePath;

            var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
            if(lastIndexOfBackslash == -1)
                lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
            if (lastIndexOfBackslash > 0)
                className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];

            LogBroker.Instance.TraceWarning($"Unmanaged Exception in {className}.{callingMethod}:{callingFileLineNumber} -> {ex}");
        }

        public static void InvokeLogout()
        {
            LogoutRequested?.Invoke(null, EventArgs.Empty);
        }
    }
}