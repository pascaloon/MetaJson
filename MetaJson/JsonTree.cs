using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    abstract class JsonNode
    {
        public abstract IEnumerable<MethodNode> GetNodes(TreeContext context);
    }

    class ObjectNode : JsonNode
    {

        public string Owner { get; }

        public ObjectNode(string owner)
        {
            Owner = owner;
        }

        public List<(string, JsonNode)> Properties { get; set; } = new List<(string, JsonNode)>();

        public override IEnumerable<MethodNode> GetNodes(TreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}if ({Owner} == null)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new PlainJsonNode(ct, "null");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}else");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);

            yield return new PlainJsonNode(ct, "{\\n");
            string jt = context.IndentJson(+1);

            for (int i = 0; i < Properties.Count; ++i)
            {
                (string name, JsonNode value) = Properties[i];

                // Json property name is the class' property name
                yield return new PlainJsonNode(ct, $"{jt}\"{name}\": ");

                // Json property value varies depending on its type
                foreach (MethodNode valueNode in value.GetNodes(context))
                    yield return valueNode;

                // If last element, don't add a ','
                if (i < Properties.Count - 1)
                    yield return new PlainJsonNode(ct, $",\\n");
                else
                    yield return new PlainJsonNode(ct, $"\\n");

            }

            jt = context.IndentJson(-1);
            yield return new PlainJsonNode(ct, $"{jt}}}");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
        }
    }

    class ListNode : JsonNode
    {
        private readonly string _container;

        public ListNode(string container)
        {
            _container = container;
        }

        // All elements of the list are of the same type and therefore are serialized the same
        public JsonNode ElementType { get; set; }


        public override IEnumerable<MethodNode> GetNodes(TreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new PlainJsonNode(ct, "[\\n");
            context.IndentJson(+1);
            string jt = context.JsonIndent;


            yield return new CSharpLineNode($"{ct}for (int i = 0; i < {_container}.Count; ++i)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);

            yield return new PlainJsonNode(ct, $"{jt}");
            foreach (MethodNode node in ElementType.GetNodes(context))
                yield return node;

            yield return new CSharpLineNode($"{ct}if (i != {_container}.Count - 1)");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}sb.AppendLine(\",\");");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}else");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}sb.AppendLine();");
            ct = context.IndentCSharp(-1);

            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");

            jt = context.IndentJson(-1);
            yield return new PlainJsonNode(ct, $"{jt}]");

        }
    }

    class StringNode: JsonNode
    {
        private readonly string _variable;

        public StringNode(string variable)
        {
            _variable = variable;
        }

        public override IEnumerable<MethodNode> GetNodes(TreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}if ({_variable} == null)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new PlainJsonNode(ct, "null");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}else");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);

            yield return new PlainJsonNode(ct, "\"");
            yield return new CSharpLineNode($"{ct}sb.Append({_variable});");
            yield return new PlainJsonNode(ct, "\"");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
        }
    }

    class NumericNode : JsonNode
    {
        private readonly string _variable;

        public NumericNode(string variable)
        {
            _variable = variable;
        }

        public override IEnumerable<MethodNode> GetNodes(TreeContext context)
        {
            string ct = context.CSharpIndent;

            yield return new CSharpLineNode($"{ct}sb.Append({_variable});");
        }
    }


    class TreeContext
    {
        public string CSharpIndent { get; set; } = string.Empty;
        public string JsonIndent { get; set; } = string.Empty;

        public string IndentCSharp(int delta)
        {
            CSharpIndent = Helpers.Indent(CSharpIndent, delta);
            return CSharpIndent;
        }

        public string IndentJson(int delta)
        {
            JsonIndent = Helpers.Indent(JsonIndent, delta);
            return JsonIndent;
        }
    }
}
