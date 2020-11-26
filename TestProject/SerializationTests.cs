using System;
using System.Collections.Generic;
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
            string json = MetaJson.MetaJsonSerializer.Serialize<SimpleObj>(null as SimpleObj);
            Assert.Equal("null", json);
        }

        [Fact]
        public void SerializeObject_empty()
        {
            EmptyObj emptyObj = new EmptyObj();
            string json = MetaJson.MetaJsonSerializer.Serialize<EmptyObj>(emptyObj);
            Assert.Equal("{\n}", json);
        }

        [Fact]
        public void SerializeObject_defaultProperties()
        {
            SimpleObj obj = new SimpleObj();
            string json = MetaJson.MetaJsonSerializer.Serialize<SimpleObj>(obj);
            obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SimpleObj>(json);
            obj.VerifyPropertiesAreDefaulted();
        }

        [Fact]
        public void SerializeObject_equality()
        {
            SimpleObj obj = new SimpleObj()
            {
                PropertyString = "Value String",
                PropertyInt = 42,
                PropertyObj = new SimpleSubObj { PropertyString = "Subobject String Value" },
                PropertyListString = new List<string> { "String Value 1", "String Value 2", "String Value3" },
                PropertyListInt = new List<int> { 5, 6, 7 },
                PropertyListObj = new List<SimpleSubObj> 
                {
                    new SimpleSubObj { PropertyString = "Subobject String Value 1" },
                    new SimpleSubObj { PropertyString = "Subobject String Value 2" },
                    new SimpleSubObj { PropertyString = "Subobject String Value 3" }
                }
            };
            string json = MetaJson.MetaJsonSerializer.Serialize<SimpleObj>(obj);
            SimpleObj obj2 = Newtonsoft.Json.JsonConvert.DeserializeObject<SimpleObj>(json);

            obj.VerifyEqualsTo(obj2);

        }
    }
}
