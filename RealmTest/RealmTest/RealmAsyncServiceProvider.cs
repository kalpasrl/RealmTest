using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Realms;
using Swordfish.NET.Collections;

namespace RealmTest
{
    public sealed class ExecutionScheduler
    {
        private ConcurrentObservableCollection<Task> ExecQueue { get; } = new ConcurrentObservableCollection<Task>();
        private object SyncLock { get; } = new object();
        private bool Terminating { get; set; }

        public async Task<T> Exec<T>(Func<Task<T>> op)
        {
            Task<T> t = default;
            T ret = default;

            lock (SyncLock)
            {
                if (!Terminating)
                {
                    t = op.Invoke();
                    ExecQueue.Add(t);
                }
            }
            if (t != null)
            {
                ret = await t.ConfigureAwait(false);
                ExecQueue.Remove(t);
            }
            return ret;
        }

        public async Task Terminate()
        {
            lock (SyncLock)
            {
                Terminating = true;
            }
            await Task.WhenAll(ExecQueue).ConfigureAwait(false);
        }
    }

    public sealed class RealmAsyncServiceProvider : IDisposable
    {
        private static object Padlock { get; } = new object();
        private bool IsDisposing { get; set; }

        private static RealmAsyncServiceProvider _instance;

        public static RealmAsyncServiceProvider Instance
        {
            get
            {
                if (_instance?.IsDisposing != false)
                {
                    lock (Padlock)
                    {
                        if (_instance?.IsDisposing != false)
                        {
                            _instance = new RealmAsyncServiceProvider();
                        }
                    }
                }

                return _instance;
            }
        }

        private AsyncContextThread AcThread { get; }
        private ManualResetEvent RealmReady { get; }
        private ExecutionScheduler Scheduler { get; }
        private Realm Realm { get; set; }

        private RealmAsyncServiceProvider()
        {
            RealmReady =  new ManualResetEvent(false);
            Scheduler = new ExecutionScheduler();
            AcThread = new AsyncContextThread();

            AcThread.Factory.Run(() =>
            {
                Realm = RealmProvider.GetRealm();
                RealmReady.Set();
            });
        }

        private async Task<T> AsyncContextExec<T>(Func<Realm, T> toExec)
        {
            var f = new Func<Task<T>>(async () =>
            {
                RealmReady.WaitOne();

                Task<T> t = default;
                T ret = default;

                try
                {
                    if (toExec == null)
                    {
                        await Task.FromException(new ArgumentException("Execution object is null")).ConfigureAwait(false);
                    }
                    else
                    {
                        t = AcThread.Factory.Run(() =>
                            {
                                if (Realm.IsClosed)
                                {
                                    Realm.TryDispose();
                                    Realm = RealmProvider.GetRealm();
                                }

                                return toExec.Invoke(Realm);
                            });
                    }

                    if (t != null)
                    {
                        ret = await t.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    LogBroker.Instance.TraceDebug("exception caught during async context execution!");

                    if (t?.Exception != null)
                    {
                        var e = t.Exception.Flatten().InnerExceptions;
                        foreach (var x in e)
                        {
                            Utils.TraceException(x);
                        }
                    }
                    else
                    {
                        Utils.TraceException(ex);
                    }
                }
                return ret;
            });

            return await Scheduler.Exec(f).ConfigureAwait(false);
        }

        public async Task<DisposableSubscription> QueueSubscriptionAsync<T>
        (
            Func<Realm, IQueryable<T>> query, NotificationCallbackDelegate<T> callBack

        ) where T : RealmObject
        {
            void DebugWrapper(IRealmCollection<T> sender, ChangeSet error, Exception changes)
            {
                try
                {
                    callBack.Invoke(sender, error, changes);
                }
                catch (Exception exception)
                {
                    LogBroker.Instance.TraceDebug("WARNING: async context exception caught!");
                    Utils.TraceException(exception);
                }
            }

            if (query == null || callBack == null)
            {
                throw new ArgumentException("unable to satisfy request: input query or callback is null");
            }
            var f = new Func<Realm, IDisposable>(r => query.Invoke(r).SubscribeForNotifications(DebugWrapper));

            var t = AsyncContextExec(f);
            var sub = await t.ConfigureAwait(true);

            return new DisposableSubscription(t, sub);
        }

        public async void Dispose()
        {
            lock (Padlock)
            {
                IsDisposing = true;
            }
            await Scheduler.Terminate().ConfigureAwait(false);
            await AcThread.Factory.Run(() => Realm.TryDispose()).ConfigureAwait(false);
            AcThread.TryDispose();
            RealmReady.TryDispose();
            ResetInstance();
        }

        private static void ResetInstance() => _instance = null;
    }

    public sealed class DisposableSubscription : IDisposable
    {
        private readonly Task<IDisposable> _taskDisposable;
        private readonly IDisposable _disposable;


        public DisposableSubscription(Task<IDisposable> taskDisposable, IDisposable disposable)
        {
            _taskDisposable = taskDisposable;
            _disposable = disposable;
        }

        public void Dispose()
        {
            _disposable.TryDispose();
            _taskDisposable.Dispose();
        }
    }
}