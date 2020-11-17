using MetaJson;
using System;

namespace SampleApp
{
    [Serialize]
    class MyObject
    {
        [Serialize]
        public string StringProperty { get; set; }
        [Serialize]
        public int IntProperty { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            MyObject myObject = new MyObject()
            {
                StringProperty = "StringValue",
                IntProperty = 42
            };

            //string value = MetaJsonSerializer.Serialize(myObject);
            //Console.WriteLine($"result: {value}");
        }
    }
}
