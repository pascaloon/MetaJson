using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    [MetaJson.Serialize]
    class SimpleObj
    {
        [MetaJson.Serialize]
        public string PropertyString { get; set; }

        [MetaJson.Serialize]
        public int PropertyInt { get; set; }

        [MetaJson.Serialize]
        public SimpleSubObj PropertyObj { get; set; }

        [MetaJson.Serialize]
        public List<string> PropertyListString { get; set; }

        [MetaJson.Serialize]
        public List<int> PropertyListInt { get; set; }

        [MetaJson.Serialize]
        public List<SimpleSubObj> PropertyListObj { get; set; }

        public void VerifyPropertiesAreDefaulted()
        {
            Assert.Equal(default, PropertyString);
            Assert.Equal(default, PropertyInt);
            Assert.Equal(default, PropertyObj);
            Assert.Equal(default, PropertyListString);
            Assert.Equal(default, PropertyListInt);
            Assert.Equal(default, PropertyListObj);
        }

        public void VerifyEqualsTo(SimpleObj obj2)
        {
            Assert.NotNull(obj2);

            Assert.Equal(PropertyString, obj2.PropertyString);
            Assert.Equal(PropertyInt, obj2.PropertyInt);

            Assert.NotNull(obj2.PropertyObj);
            Assert.Equal(PropertyObj.PropertyString, obj2.PropertyObj.PropertyString);

            Assert.NotNull(obj2.PropertyListString);
            Assert.Equal(3, obj2.PropertyListString.Count);
            Assert.Equal(PropertyListString[0], obj2.PropertyListString[0]);
            Assert.Equal(PropertyListString[1], obj2.PropertyListString[1]);
            Assert.Equal(PropertyListString[2], obj2.PropertyListString[2]);

            Assert.NotNull(obj2.PropertyListInt);
            Assert.Equal(3, obj2.PropertyListInt.Count);
            Assert.Equal(PropertyListInt[0], obj2.PropertyListInt[0]);
            Assert.Equal(PropertyListInt[1], obj2.PropertyListInt[1]);
            Assert.Equal(PropertyListInt[2], obj2.PropertyListInt[2]);

            Assert.NotNull(obj2.PropertyListObj);
            Assert.Equal(3, obj2.PropertyListObj.Count);
            Assert.Equal(PropertyListObj[0].PropertyString, obj2.PropertyListObj[0].PropertyString);
            Assert.Equal(PropertyListObj[1].PropertyString, obj2.PropertyListObj[1].PropertyString);
            Assert.Equal(PropertyListObj[2].PropertyString, obj2.PropertyListObj[2].PropertyString);
        }
    }

    [MetaJson.Serialize]
    class SimpleSubObj
    {
        [MetaJson.Serialize]
        public string PropertyString { get; set; }
    }

    [MetaJson.Serialize]
    class EmptyObj
    {
    }
}
