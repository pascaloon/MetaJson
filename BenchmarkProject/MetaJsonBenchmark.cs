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


    [MemoryDiagnoser]
    public class SerializationBenchmark
    {
        private Book _testBook;
        public SerializationBenchmark()
        {
            _testBook = new Book()
            {
                Title = "The Great Voyage",
                Authors = new List<Person>
                {
                    new Person { Name = "Author A", Age = 53, County = "United States" },
                    new Person { Name = "Author B", Age = 47, County = "Canada" },
                    new Person { Name = "Author C", Age = 47, County = "Canada" },
                    new Person { Name = "Author D", Age = 47, County = "Canada" },
                    new Person { Name = "Author E", Age = 47, County = "Canada" },
                    new Person { Name = "Author F", Age = 47, County = "Canada" },
                    new Person { Name = "Author G", Age = 47, County = "Canada" },
                    new Person { Name = "Author H", Age = 47, County = "Canada" },
                    new Person { Name = "Author I", Age = 47, County = "Canada" },
                    new Person { Name = "Author J", Age = 47, County = "Canada" },
                    new Person { Name = "Author K", Age = 47, County = "Canada" },
                    new Person { Name = "Author L", Age = 47, County = "Canada" },
                    new Person { Name = "Author M", Age = 47, County = "Canada" },
                    new Person { Name = "Author N", Age = 47, County = "Canada" },
                    new Person { Name = "Author O", Age = 47, County = "Canada" },
                    new Person { Name = "Author P", Age = 47, County = "Canada" },
                    new Person { Name = "Author Q", Age = 47, County = "Canada" },
                },
                Chapters = new List<Chapter>
                {
                    new Chapter { Name = "Chapter 1", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 2", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 3", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 4", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 5", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 6", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 7", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 8", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 9", PageBegin = 5, PageEnd = 10},
                    new Chapter { Name = "Chapter 10", PageBegin = 5, PageEnd = 10},
                },
                TotalPageCount = 300,
                Price = 50
            };
        }

        [Benchmark(Baseline = true)]
        public string Serialize_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_testBook);
        }

        [Benchmark]
        public string Serialize_MetaJson()
        {
            return MetaJson.MetaJsonSerializer.Serialize<Book>(_testBook);
        }
    }


    [MemoryDiagnoser]
    public class DeserializationBenchmark
    {
        private string _jsonContent;
        public DeserializationBenchmark()
        {
            string filePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\..\..\..", "DeserializationSample.json"));
            _jsonContent = File.ReadAllText(filePath);
        }

        [Benchmark(Baseline = true)]
        public Book Deserialize_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Book>(_jsonContent);
        }

        [Benchmark]
        public Book Deserialize_MetaJson()
        {
            return MetaJson.MetaJsonSerializer.Deserialize<Book>(_jsonContent);
        }
    }
}
