using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Realms;

namespace RealmTest
{
    public static class RealmProvider
    {
        private static readonly object Padlock = new object();
        private static readonly object InstancesLock = new object();
        private static RealmConfiguration _realmConfiguration;

        public static readonly List<(Realm, string)> RealmInstancesList = new List<(Realm, string)>();

        private const double ConvertToMb = 1024d * 1024d;

        public static Realm GetRealm(
            [CallerMemberName] string callingMethod = ""
            ,[CallerFilePath] string callingFilePath = ""
            )
        {
            if (_realmConfiguration == null)
            {
                lock (Padlock)
                {
                    _realmConfiguration ??= GetRealmConfiguration();
                }
            }

            Realm realm;

            try
            {
                realm = Realm.GetInstance(_realmConfiguration);
                if (realm.IsClosed)
                {
                    LogBroker.Instance.TraceDebug($"Realm - GetInstance returned a closed realm!");
                    realm = Realm.GetInstance(_realmConfiguration);
                    if (realm.IsClosed)
                        LogBroker.Instance.TraceDebug($"Realm - Unable to recover :(");
                    else
                        LogBroker.Instance.TraceDebug($"Realm - Recovered by reinstantiating the realm!");

                }
            }
            catch (Realms.Exceptions.RealmException ex)
            {
                Utils.TraceException(ex,false);

                if (!string.IsNullOrEmpty(ex.Message) && ex.Message.Contains("is less than last set version"))
                {
                    lock (Padlock)
                    {
                        Realm.DeleteRealm(_realmConfiguration);
                        _realmConfiguration = GetRealmConfiguration();
                    }
                }
                realm = Realm.GetInstance(_realmConfiguration);
            }
            AddInstance(callingMethod, callingFilePath, realm);
            return realm;
        }

        private static void AddInstance(
            string callingMethod,
            string callingFilePath,
            Realm realm
        )

        {
            lock (InstancesLock)
            {
                Interlocked.Increment(ref Utils.RealmCurrentInstances);
                TrackRealmInstancesList(callingMethod, callingFilePath, realm);
            }
        }

        public static void RemoveInstance(
            Realm realm
        )
        {
            lock (InstancesLock)
            {
                Interlocked.Decrement(ref Utils.RealmCurrentInstances);

                RealmInstancesList.RemoveAll(tuple => Equals(realm, tuple.Item1));
                RealmInstancesList.RemoveAll(tuple => tuple.Item1.IsClosed);
            }
        }

        private static void TrackRealmInstancesList(string callingMethod, string callingFilePath, Realm realm)
        {
            try
            {
                var callingFunction = callingMethod;
                var lastIndexOfBackslash = callingFilePath.LastIndexOf('\\');
                if (lastIndexOfBackslash == -1)
                    lastIndexOfBackslash = callingFilePath.LastIndexOf('/');
                if (lastIndexOfBackslash > 0)
                {
                    var className = callingFilePath.Substring(lastIndexOfBackslash + 1).Split('.')[0];
                    callingFunction = $"[{className}.{callingMethod}]";
                }

                RealmInstancesList.Add((realm, callingFunction));
            }
            catch (Exception exc)
            {
                LogBroker.Instance.TraceDebug($"[TEST] crash {exc.Message}:{exc.StackTrace}", false);
            }
        }

        private static RealmConfiguration GetRealmConfiguration()
        {
            var config = new RealmConfiguration("realm.db")
            {
                SchemaVersion = 1,
                ShouldDeleteIfMigrationNeeded = false,
                ShouldCompactOnLaunch = (totalBytes, usedBytes) =>
                {
                    return true;
                },

            };

            var path = config.DatabasePath;
            LogBroker.Instance.TraceDebug($"Database path: {path}");
            var version = config.SchemaVersion;
            LogBroker.Instance.TraceDebug($"Database version: {version}");

            return config;
        }


        public static void PrintRealmInstancesList()
        {
            lock (InstancesLock)
            {
                LogBroker.Instance.TraceDebug("[TEST]");

                foreach (var tuple in RealmInstancesList.GroupBy(info => info.Item2)
                .Select(group => new
                {
                    Metric = group.Key,
                    Count = group.Count()
                })
                .OrderBy(x => x.Count))
                {
                    LogBroker.Instance.TraceDebug($"[TEST] RealmInstancesList[{tuple.Metric}] : {tuple.Count}", false);
                }
                LogBroker.Instance.TraceDebug("[TEST]");
            }
        }

        public static bool Compact()
        {
            try
            {
                return Realm.Compact(_realmConfiguration);
            }
            catch (Exception exc)
            {
                Utils.TraceException(exc);
                return false;
            }

        }
    }

}