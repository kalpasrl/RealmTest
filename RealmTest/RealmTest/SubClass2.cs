using Realms;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealmTest
{
    internal class SubClass2 : RealmObject, ISubClass
    {
        [PrimaryKey]
        public int MyKey { get; set; }
        public string MyProperty1 { get; set; }
        public string MyProperty2 { get; set; }
        public string MyProperty3 { get; set; }
        public string MyProperty4 { get; set; }
        public string MyProperty5 { get; set; }
    }
}
