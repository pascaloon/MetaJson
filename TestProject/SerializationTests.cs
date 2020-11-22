using System;
using Xunit;

namespace TestProject
{
    public class SerializationTests
    {
        [Fact]
        public void Dummy()
        {
            Assert.True(true);
        }

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
    }
}
