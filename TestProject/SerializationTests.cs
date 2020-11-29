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
            string simpleString = "Hello!";
            string json = MetaJson.MetaJsonSerializer.Serialize(simpleString);
            Assert.Equal($"\"{simpleString}\"", json);
        }

        [Fact]
        public void SerializePrimitive_string_null()
        {
            string nullString = null;
            string json = MetaJson.MetaJsonSerializer.Serialize(nullString);
            Assert.Equal("null", json);
        }

        [Fact]
        public void SerializePrimitive_int()
        {
            int value = 42;
            string json = MetaJson.MetaJsonSerializer.Serialize(value);
            Assert.Equal($"{value}", json);
        }

        [Fact]
        public void SerializeObject_null()
        {
            SimpleObj nullSimpleObj = null;
            string json = MetaJson.MetaJsonSerializer.Serialize(nullSimpleObj);
            Assert.Equal("null", json);
        }

        [Fact]
        public void SerializeObject_empty()
        {
            EmptyObj emptyObj = new EmptyObj();
            string json = MetaJson.MetaJsonSerializer.Serialize(emptyObj);
            Assert.Equal("{\n}", json);
        }

        [Fact]
        public void SerializeObject_defaultProperties()
        {
            SimpleObj obj = new SimpleObj();
            string json = MetaJson.MetaJsonSerializer.Serialize(obj);
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
            string json = MetaJson.MetaJsonSerializer.Serialize(obj);
            SimpleObj obj2 = Newtonsoft.Json.JsonConvert.DeserializeObject<SimpleObj>(json);

            obj.VerifyEqualsTo(obj2);

        }
    }
}
