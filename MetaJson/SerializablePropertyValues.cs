using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    abstract class SerializablePropertyValue
    {
        abstract public IEnumerable<MethodNode> GetValueNodes(string id);
    }

    class StringSerializablePropertyValue : SerializablePropertyValue
    {
        public override IEnumerable<MethodNode> GetValueNodes(string id)
        {
            yield return new JsonNode("\"");
            yield return new CSharpNode($"$tsb.Append({id});\r\n");
            yield return new JsonNode("\"");
        }
    }

    class NumSerializablePropertyValue : SerializablePropertyValue
    {
        public override IEnumerable<MethodNode> GetValueNodes(string id)
        {
            yield return new CSharpNode($"$tsb.Append({id});\r\n");
        }
    }

    class SimpleSerializablePropertyValue : SerializablePropertyValue
    {
        public override IEnumerable<MethodNode> GetValueNodes(string id)
        {
            yield return new JsonNode("\"");
            yield return new CSharpNode($"$tsb.Append({id}.ToString());\r\n");
            yield return new JsonNode("\"");
        }
    }
}
