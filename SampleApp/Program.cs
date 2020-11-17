using MetaJson;
using System;

namespace SampleApp
{
    [Serialize]
    public class Person
    {
        [Serialize]
        public string Name { get; set; }
        [Serialize]
        public int Age { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Person myObject = new Person()
            {
                Name = "Bob",
                Age = 42
            };

            DummySymbol.DoNothing();
            string value = MetaJsonSerializer.Serialize<Person>(myObject);
            Console.WriteLine($"result:");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine(value);
            Console.WriteLine($"--------------------------------");
        }
    }
}
