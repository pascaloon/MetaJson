using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    public class DeserializationTests
    {
        [Fact]
        public void DeserializePrimitive_string()
        {
            MetaJson.MetaJsonSerializer.Deserialize("\"Hello!\"", out string obj);
            Assert.Equal("Hello!", obj);
        }

        [Fact]
        public void DeserializePrimitive_string_null()
        {
            MetaJson.MetaJsonSerializer.Deserialize("null", out string obj);
            Assert.Null(obj);
        }

        [Fact]
        public void DeserializePrimitive_int()
        {
            MetaJson.MetaJsonSerializer.Deserialize("42", out int obj);
            Assert.Equal(42, obj);
        }

        [Fact]
        public void DeserializeObject_null()
        {
            MetaJson.MetaJsonSerializer.Deserialize("null", out SimpleObj obj);
            Assert.Null(obj);
        }

        [Fact]
        public void DeserializeObject_empty()
        {
            MetaJson.MetaJsonSerializer.Deserialize("{}", out EmptyObj obj);
            Assert.NotNull(obj);
        }

        [Fact]
        public void DeserializeObject_defaultProperties()
        {
            SimpleObj obj = new SimpleObj();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            MetaJson.MetaJsonSerializer.Deserialize(json, out SimpleObj obj2);
            obj2.VerifyPropertiesAreDefaulted();
        }

        [Fact]
        public void DeserializeObject_equality()
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

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            MetaJson.MetaJsonSerializer.Deserialize(json, out SimpleObj obj2);

            obj.VerifyEqualsTo(obj2);

        }
    }
}
