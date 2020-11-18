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
        public Person Author { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            DummySymbol.DoNothing();

            Person author = new Person()
            {
                Name = "Bob",
                Age = 42,
            };

            Book book = new Book()
            {
                Name = "The Great Voyage",
                PageCount = 300,
                Author = author
            };

            Console.WriteLine($"output:");
            Console.WriteLine($"--------------------------------");
            string bookJson = MetaJsonSerializer.Serialize<Book>(book);
            Console.WriteLine(bookJson);
            Console.WriteLine($"--------------------------------");
        }
    }
}
