using MetaJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SampleApp
{

    [Serialize]
    public class Person
    {
        [NotNull, Serialize]
        public string Name { get; set; }
        [NotNull, Serialize]
        public int Age { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Person person && Equals(person);
        }

        public bool Equals(Person other)
        {
            if (other is null)
                return false;

            if (Name != other.Name)
                return false;

            if (Age != other.Age)
                return false;

            return true;
        }
    }

    [Serialize]
    public class Book
    {
        [NotNull, Serialize]
        public string Name { get; set; }
        [Serialize]
        public int PageCount { get; set; }
        [NotNull, ArrayItemNotNull, Serialize]
        public List<Person> Authors { get; set; }
        [NotNull, ArrayItemNotNull, Serialize]
        public IList<string> FavoriteQuotes { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Book book && Equals(book);
        }

        public bool Equals(Book other)
        {
            if (other is null)
                return false;

            if (Name != other.Name)
                return false;

            if (PageCount != other.PageCount)
                return false;

            if (Authors is null)
            {
                if (other.Authors != null)
                    return false;
            }
            else
            {
                if (other.Authors is null)
                    return false;

                if (Authors.Count != other.Authors.Count)
                    return false;

                for (int i = 0; i < Authors.Count; i++)
                {
                    if (!Authors[i].Equals(other.Authors[i]))
                        return false;
                }
            }

            if (FavoriteQuotes is null)
            {
                if (other.FavoriteQuotes != null)
                    return false;
            }
            else
            {
                if (other.FavoriteQuotes is null)
                    return false;

                if (FavoriteQuotes.Count != other.FavoriteQuotes.Count)
                    return false;

                for (int i = 0; i < FavoriteQuotes.Count; i++)
                {
                    if (!FavoriteQuotes[i].Equals(other.FavoriteQuotes[i]))
                        return false;
                }
            }

            return true;
        }

    }

    [Serialize]
    public class Song
    {
        [NotNull, Serialize]
        public string Name { get; set; }
        [NotNull, Serialize]
        public Person Singer { get; set; }
        [NotNull, Serialize]
        public IList<string> Lyrics { get; set; }
    }

    class Program
    {
        static string BookJsonFilePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..", "SampleOutput.json"));

        static Book TestBook;

        static void CreateBook()
        {
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

            TestBook = new Book()
            {
                Name = "The Great Voyage",
                PageCount = 300,
                Authors = new List<Person> { authorA, authorB, authorC },
                FavoriteQuotes = new List<string> { "This is", " very, very", " awesome!" }
            };
        }

        static void TestSerialization()
        {
            Console.WriteLine("Serializing...");
            string bookJson = MetaJson.MetaJsonSerializer.Serialize<Book>(TestBook);

            Console.WriteLine($"Serialization Output:");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine(bookJson);
            Console.WriteLine($"--------------------------------");

            Console.WriteLine($"Saving '{BookJsonFilePath}'...");
            File.WriteAllText(BookJsonFilePath, bookJson);
        }

        static void TestDeserialization()
        {
            Console.WriteLine($"Loading '{BookJsonFilePath}'...");
            string bookJson = File.ReadAllText(BookJsonFilePath);

            Console.WriteLine("Deserializing...");
            MetaJson.MetaJsonSerializer.Deserialize<Book>(bookJson, out Book book);
            bool isBookEqual = TestBook.Equals(book);
            if (isBookEqual)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SUCCESS !!!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR !!!");
            }

            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            DummySymbol.DoNothing();

            CreateBook();

            TestSerialization();

            TestDeserialization();
        }
    }
}
