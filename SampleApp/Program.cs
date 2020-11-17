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

    [Serialize]
    public class Book
    {
        [Serialize]
        public string Name { get; set; }
        [Serialize]
        public int PageCount { get; set; }
        [Serialize]
        public string Author { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            DummySymbol.DoNothing();

            Person myObject = new Person()
            {
                Name = "Bob",
                Age = 42
            };

            string value = MetaJsonSerializer.Serialize<Person>(myObject);
            Console.WriteLine($"output:");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine(value);
            Console.WriteLine($"--------------------------------");

            Book book = new Book()
            {
                Name = "Some random book",
                PageCount = 300,
                Author = "Someone"
            };

            string value2 = MetaJsonSerializer.Serialize<Book>(book);
            Console.WriteLine(value2);
            Console.WriteLine($"--------------------------------");
        }
    }
}
