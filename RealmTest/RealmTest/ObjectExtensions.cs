using ExpressMapper;
using Realms;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Essentials;

namespace RealmTest
{
    public static class ObjectExtensions
    {
        // ReSharper disable once UnusedMethodReturnValue.Global
        public static bool TryDispose(this IDisposable obj,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            try
            {
                if (obj != null)
                {
                    #region Dispose Realm Instance
                    if (obj is Realm re)
                    {
                        var disposed = false;
                        try
                        {
                            if (!re.IsClosed)
                            {
                                re.Dispose();
                                disposed = true;

                                RealmProvider.RemoveInstance(re);
                            }
                        }
                        catch (Exception)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    if (!re.IsClosed)
                                    {
                                        re.Dispose();
                                        disposed = true;

                                       RealmProvider.RemoveInstance(re);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Utils.TraceTryDisposeException(ex, callingMethod, callingFilePath, callingFileLineNumber);
                                    disposed = false;
                                }
                            });
                        }
                        return disposed;
                    }
                    #endregion

                    //Object is not Realm Instance
                    obj.Dispose();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Utils.TraceTryDisposeException(ex, callingMethod, callingFilePath, callingFileLineNumber);
            }

            return false;
        }

    }

    public static class RealmExtension
    {
        /* detach an object from its realm, making it non thread confined */
        public static RealmObject ToUnmanaged(this RealmObject obj)
        {
            if (obj == null)
            {
                LogBroker.Instance.TraceWarning("Realm object is null");
                return null;
            }

            switch (obj)
            {
                case Class1 m:
                    return Mapper.Map<Class1, Class1>(m);
                case Class2 m:
                    return Mapper.Map(m, new Class2());
                case Class3 m:
                    return Mapper.Map<Class3, Class3>(m);
                case Class4 m:
                    return Mapper.Map<Class4, Class4>(m);
                case Class5 m:
                    return Mapper.Map(m, new Class5());
                default:
                    LogBroker.Instance.TraceWarning($"Unhandled input type: unable to map object {obj} to unmanaged counterpart");
                    return null;
            }
        }
    }

}
