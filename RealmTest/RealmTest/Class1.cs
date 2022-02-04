using Realms;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealmTest
{
    internal class Class1 : RealmObject, IClass
    {
        [PrimaryKey]
        public int MyKey { get; set; }
        public string MyProperty1 { get; set; }
        public string MyProperty2 { get; set; }
        public string MyProperty3 { get; set; }
        public string MyProperty4 { get; set; }
        public string MyProperty5 { get; set; }
        public string MyProperty6 { get; set; }
        public string MyProperty7 { get; set; }
        public string MyProperty8 { get; set; }
        public string MyProperty9 { get; set; }
        public string MyProperty10 { get; set; }

        public IList<SubClass1> SubClasses1 { get; }
        public IList<SubClass2> SubClasses2 { get; }

        public Class1()
        {

        }

        public Class1(IList<SubClass1> sub1, IList<SubClass2> sub2)
        {
            SubClasses1 = sub1;
            SubClasses2 = sub2;
        }
    }
}
