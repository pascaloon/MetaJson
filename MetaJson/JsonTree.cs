using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    abstract class JsonNode
    {
        public abstract IEnumerable<MethodNode> GetNodes();
    }

    class ObjectNode : JsonNode
    {

        public ObjectNode()
        {
        }

        public List<(string, JsonNode)> Properties { get; set; } = new List<(string, JsonNode)>();

        public override IEnumerable<MethodNode> GetNodes()
        {
            yield return new PlainJsonNode("{\\n");
            yield return new JsonTabControlNode(+1);

            for (int i = 0; i < Properties.Count; ++i)
            {
                (string name, JsonNode value) = Properties[i];

                // Json property name is the class' property name
                yield return new PlainJsonNode($"$t\"{name}\": ");

                // Json property value varies depending on its type
                foreach (MethodNode valueNode in value.GetNodes())
                    yield return valueNode;

                // If last element, don't add a ','
                if (i < Properties.Count - 1)
                    yield return new PlainJsonNode($",\\n");
                else
                    yield return new PlainJsonNode($"\\n");

            }

            yield return new JsonTabControlNode(-1);
            yield return new PlainJsonNode("$t}");
        }
    }

    class StringNode: JsonNode
    {
        private readonly string _value;

        public StringNode(string value)
        {
            _value = value;
        }

        public override IEnumerable<MethodNode> GetNodes()
        {
            yield return new PlainJsonNode("\"");
            yield return new CSharpLineNode($"sb.Append({_value});");
            yield return new PlainJsonNode("\"");
        }
    }

    class NumericNode : JsonNode
    {
        private readonly string _value;

        public NumericNode(string value)
        {
            _value = value;
        }

        public override IEnumerable<MethodNode> GetNodes()
        {
            yield return new CSharpLineNode($"sb.Append({_value});");
        }
    }
}
