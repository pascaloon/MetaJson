using MetaJson;
using System;
using System.Collections.Generic;

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
        public List<Person> Authors { get; set; }
        [Serialize]
        public IList<string> FavoriteQuotes { get; set; }

    }


    class Program
    {
        static void Main(string[] args)
        {
            DummySymbol.DoNothing();

            Person authorA = new Person()
            {
                Name = "Bob",
                Age = 42,
            };
            Person authorB = new Person()
            {
                Name = "Boby",
                Age = 25,
            };
            Person authorC = new Person()
            {
                Name = "Baba",
                Age = 54,
            };

            Book book = new Book()
            {
                Name = "The Great Voyage",
                PageCount = 300,
                Authors = new List<Person> { authorA, authorB, authorC},
                FavoriteQuotes = new List<string> { "This is", " very, very", " awesome!"}
            };

            Console.WriteLine($"output:");
            Console.WriteLine($"--------------------------------");
            string bookJson = MetaJson.MetaJsonSerializer.Serialize<Book>(book);
            Console.WriteLine(bookJson);
            Console.WriteLine($"--------------------------------");
        }
    }
}
