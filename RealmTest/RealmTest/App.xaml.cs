using Realms;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RealmTest
{
    public partial class App : Application
    {
        Realm _realm;
        private IDisposable _token1;
        private IDisposable _token2;
        private IDisposable _token3;
        private IDisposable _token4;
        private IDisposable _token5;

        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            Initialize();
        }

        protected override void OnSleep()
        {
            Finalize();
        }

        private void Finalize()
        {
            // Disposing all token
            _token1.Dispose();
            _token2.Dispose();
            _token3.Dispose();
            _token4.Dispose();
            _token5.Dispose();

            // Disponse instance of Realm
            _realm.Refresh();
            _realm.Dispose();

            RealmProvider.Compact();
        }

        protected override void OnResume()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Create an instance of Realm that live at entire app lifecycle, in order to simulate our app architecture
            _realm = RealmProvider.GetRealm();

            CreateSubscriptions();
        }

        private void TraceLog<T>(IRealmCollection<T> sender, ChangeSet changes, Exception error)
        {
            if (error != null)
            {
                LogBroker.Instance.TraceDebug($"{typeof(T)}: error {error.Message}");
            }

            LogBroker.Instance.TraceDebug($"{typeof(T)}: {sender.Count} - inserted: {changes?.InsertedIndices.Count()}, modified: {changes?.ModifiedIndices.Count()}, deleted: {changes?.DeletedIndices.Count()}");
        }


        /// <summary>
        /// Creates subscriptions in a Realm class instance
        /// </summary>
        private void CreateSubscriptions()
        {
            _token1 = _realm.All<Class1>().SubscribeForNotifications(TraceLog);
            _token2 = _realm.All<Class2>().SubscribeForNotifications(TraceLog);
            _token3 = _realm.All<Class3>().SubscribeForNotifications(TraceLog);
            _token4 = _realm.All<Class4>().SubscribeForNotifications(TraceLog);
            _token5 = _realm.All<Class5>().SubscribeForNotifications(TraceLog);
        }
    }
}
