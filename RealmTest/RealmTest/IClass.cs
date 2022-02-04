using System;
using System.Collections.Generic;
using System.Text;

namespace RealmTest
{
    internal interface IClass
    {
        int MyKey { get; set; }
        string MyProperty1 { get; set; }
        string MyProperty2 { get; set; }
        string MyProperty3 { get; set; }
        string MyProperty4 { get; set; }
        string MyProperty5 { get; set; }
        string MyProperty6 { get; set; }
        string MyProperty7 { get; set; }
        string MyProperty8 { get; set; }
        string MyProperty9 { get; set; }
        string MyProperty10 { get; set; }
        IList<SubClass1> SubClasses1 { get; }
        IList<SubClass2> SubClasses2 { get; }
    }
}
