using Realms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RealmTest
{
    public partial class MainPage : ContentPage
    {
        private const int _maxClasses = 10;
        private const int _maxSubClasses = 10;
        private bool _taskProcess;

        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Exec CRUD operations in a worker thread
        /// </summary>
        private void DoProcess()
        {
            Task.Run(() =>
            {
                _taskProcess = true;
                ClearData();

                Task.Run(AddClass<Class1>).ContinueWith(ModifyClass<Class1>);
                Task.Run(AddClass<Class2>).ContinueWith(ModifyClass<Class2>);
                Task.Run(AddClass<Class3>).ContinueWith(ModifyClass<Class3>);
                Task.Run(AddClass<Class4>).ContinueWith(ModifyClass<Class4>);
                Task.Run(AddClass<Class5>).ContinueWith(ModifyClass<Class5>);
            });
        }

        /// <summary>
        /// Clear all data before starting test
        /// </summary>
        private void ClearData()
        {
            DeleteClass<Class1>();
            DeleteClass<Class2>();
            DeleteClass<Class3>();
            DeleteClass<Class4>();
            DeleteClass<Class5>();
            DeleteSubClass<SubClass1>();
            DeleteSubClass<SubClass2>();
        }

        /// <summary>
        /// Deletes all items of type IClass with a local Realm instance
        /// </summary>
        /// <typeparam name="T">IClass</typeparam>
        private void DeleteSubClass<T>() where T : RealmObject, ISubClass
        {
            var realm = RealmProvider.GetRealm();           
            realm.Write(() => realm.RemoveAll<T>());
            realm.Refresh();
            realm.TryDispose();
        }

        /// <summary>
        /// Deletes all items of type ISubClass with a local Realm instance
        /// </summary>
        /// <typeparam name="T">ISubClass</typeparam>
        /// <param name="t">Task</param>
        private void DeleteClass<T>() where T : RealmObject, IClass
        {
            var realm = RealmProvider.GetRealm();           
            realm.Write(() => realm.RemoveAll<T>());
            realm.Refresh();
            realm.TryDispose();
        }

        /// <summary>
        /// Modifies one property of type IClass with a local Realm instance
        /// </summary>
        /// <typeparam name="T">IClass</typeparam>
        /// <param name="t">Task</param>
        private void ModifyClass<T>(Task t) where T : RealmObject, IClass
        {
            do
            {
                var realm = RealmProvider.GetRealm();
                foreach (var c in realm.All<T>())
                {
                    realm.Write(() =>
                    {
                        c.MyProperty1 = Guid.NewGuid().ToString();
                    });
                }
                realm.Refresh();
                realm.TryDispose();
            } while (_taskProcess);
        }

        /// <summary>
        /// Addes n item of type IClass with a local Realm instance
        /// </summary>
        /// <typeparam name="T">IClass</typeparam>
        private void AddClass<T>() where T : RealmObject, IClass
        {
            for (int i = 1; i <= _maxClasses; i++)
            {
                var realm = RealmProvider.GetRealm();
                realm.Write(() =>
                {
                    T obj = (T)Activator.CreateInstance(typeof(T), PopulateSubClasses<SubClass1>(), PopulateSubClasses<SubClass2>());
                    obj.MyKey = i;
                    obj.MyProperty1 = Guid.NewGuid().ToString();
                    obj.MyProperty2 = Guid.NewGuid().ToString();
                    obj.MyProperty3 = Guid.NewGuid().ToString();
                    obj.MyProperty4 = Guid.NewGuid().ToString();
                    obj.MyProperty5 = Guid.NewGuid().ToString();
                    obj.MyProperty6 = Guid.NewGuid().ToString();
                    obj.MyProperty7 = Guid.NewGuid().ToString();
                    obj.MyProperty8 = Guid.NewGuid().ToString();
                    obj.MyProperty9 = Guid.NewGuid().ToString();
                    obj.MyProperty10 = Guid.NewGuid().ToString();
                    realm.Add<T>(obj, true);
                });
                realm.Refresh();
                realm.TryDispose();
            }
        }

        private int subClassIndex = 1;
        /// <summary>
        /// Creates a list of instance of ISubClass
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private List<T> PopulateSubClasses<T>() where T : RealmObject, ISubClass
        {
            var list = new List<T>();
            for (int i = subClassIndex; i <= subClassIndex + _maxSubClasses; i++)
            {
                T obj = (T)Activator.CreateInstance(typeof(T));
                obj.MyKey = i;
                obj.MyProperty1 = Guid.NewGuid().ToString();
                obj.MyProperty2 = Guid.NewGuid().ToString();
                obj.MyProperty3 = Guid.NewGuid().ToString();
                obj.MyProperty4 = Guid.NewGuid().ToString();
                obj.MyProperty5 = Guid.NewGuid().ToString();
                list.Add(obj);
            }

            subClassIndex += _maxSubClasses;
            return list;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (!_taskProcess)
            {
                (sender as Button).Text = "Stop";
                DoProcess();
            }
            else
            {
                (sender as Button).Text = "Start";
                _taskProcess = false;
            }
        }
    }
}
