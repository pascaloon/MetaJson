using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaJson;
using System.IO;
using System.Reflection;

namespace BenchmarkProject
{
    //public sealed partial class SerializeAttribute { }

    [Serialize]
    public class Book
    {
        [Serialize] public string Title { get; set; }
        [Serialize] public List<Person> Authors { get; set; }
        [Serialize] public List<Chapter> Chapters { get; set; }
        [Serialize] public int TotalPageCount { get; set; }
        [Serialize] public int Price { get; set; }
    }

    [Serialize]
    public class Person
    {
        [Serialize] public string Name { get; set; }
        [Serialize] public int Age { get; set; }
        [Serialize] public string County { get; set; }
    }

    [Serialize]
    public class Chapter
    {
        [Serialize] public string Name { get; set; }
        [Serialize] public int PageBegin { get; set; }
        [Serialize] public int PageEnd { get; set; }
    }

    public static class Utils
    {
        public static Book GenerateBook(int chapterCount)
        {
            Book book = new Book()
            {
                Title = "The Great Voyage",
                Authors = new List<Person>
                {
                    new Person { Name = "Author A", Age = 53, County = "United States" },
                },
                Chapters = new List<Chapter>(),
                TotalPageCount = 300,
                Price = 50
            };

            Random r = new Random();
            for (int i = 0; i < chapterCount; i++)
            {
                book.Chapters.Add(new Chapter()
                {
                    Name = GetRandomString(30, r),
                    PageBegin = r.Next(0, 100),
                    PageEnd = r.Next(0, 100),
                });
            }

            return book;
        }

        private static string GetRandomString(int size, Random r)
        {
            char[] str = new char[size];
            for (int i = 0; i < 12; i++)
            {
                str[i] = (char)r.Next('a', 'z' + 1);
            }

            return new string(str);
        }
    }


    [MemoryDiagnoser]
    public class SerializationBenchmark
    {
        [Params(1, 10, 100, 1000)]
        public int ChaptersCount { get; set; }

        private Book _testBook;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _testBook = Utils.GenerateBook(ChaptersCount);
        }

        [Benchmark(Baseline = true)]
        public string Serialize_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_testBook);
        }

        [Benchmark]
        public string Serialize_MetaJson()
        {
            return MetaJson.MetaJsonSerializer.Serialize(_testBook);
        }
    }

    [MemoryDiagnoser]
    public class DeserializationBenchmark
    {
        [Params(1, 10, 100, 1000)]
        public int ChaptersCount { get; set; }

        private string _jsonContent;

        [GlobalSetup]
        public void GlobalSetup()
        {
            Book book = Utils.GenerateBook(ChaptersCount);
            _jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(book);
        }

        [Benchmark(Baseline = true)]
        public Book Deserialize_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Book>(_jsonContent);
        }

        [Benchmark]
        public Book Deserialize_MetaJson()
        {
            MetaJson.MetaJsonSerializer.Deserialize(_jsonContent, out Book book);
            return book;
        }
    }
}
