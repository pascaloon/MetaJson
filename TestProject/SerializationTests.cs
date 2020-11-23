using System;
using Xunit;

namespace TestProject
{
    public class SerializationTests
    {
        [Fact]
        public void SerializePrimitive_string()
        {
            string json = MetaJson.MetaJsonSerializer.Serialize<String>("Hello!");
            Assert.Equal("\"Hello!\"", json);
        }

        [Fact]
        public void SerializePrimitive_string_null()
        {
            string json = MetaJson.MetaJsonSerializer.Serialize<String>(null);
            Assert.Equal("null", json);
        }

        [Fact]
        public void SerializePrimitive_int()
        {
            string json = MetaJson.MetaJsonSerializer.Serialize<int>(42);
            Assert.Equal("42", json);
        }

        [Fact]
        public void SerializeObject_null()
        {
            SimpleObj nullObject = null;
            string json = MetaJson.MetaJsonSerializer.Serialize<SimpleObj>(nullObject);
            Assert.Equal("null", json);
        }

        [Fact]
        public void SerializeObject_empty()
        {
            EmptyObj emptyObj = new EmptyObj();
            string json = MetaJson.MetaJsonSerializer.Serialize<EmptyObj>(emptyObj);
            Assert.Equal("{\n}", json);
        }
    }

    [MetaJson.Serialize]
    class SimpleObj
    {
        [MetaJson.Serialize]
        public string PropertyA { get; set; }
    }

    [MetaJson.Serialize]
    class EmptyObj
    {
    }

}
