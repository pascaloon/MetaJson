using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    abstract class DzJsonNode
    {
        public abstract IEnumerable<MethodNode> GetNodes(DzTreeContext context);
    }

    class DzObjectNode : DzJsonNode
    {
        public string Type { get; }
        public List<(string, DzJsonNode)> Properties = new List<(string, DzJsonNode)>();

        public DzObjectNode(string type)
        {
            Type = type;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}var NULL_SPAN = \"null\".AsSpan();");
            yield return new CSharpLineNode($"{ct}if (json.StartsWith(NULL_SPAN))");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}json = json.Slice(4);");
            yield return new CSharpLineNode($"{ct}return default({Type});");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            string errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected '{{'";
            yield return new CSharpLineNode($"{ct}if (json[0] != '{{') throw new Exception($\"{errmsg}\");");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");

            // Parse object properties

            yield return new CSharpLineNode($"{ct}{Type} obj = new {Type}();");
            yield return new CSharpLineNode($"{ct}while (true)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected object content or '}}'";
            yield return new CSharpLineNode($"{ct}if (json.IsEmpty) throw new Exception($\"{errmsg}\");");
            yield return new CSharpLineNode($"{ct}if (json[0] == '}}') break;");
            
            // property found
            yield return new CSharpLineNode($"{ct}if (json[0] == '\"')");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");
            yield return new CSharpLineNode($"{ct}var name = json.Slice(0, json.IndexOf('\"'));");
            for (int i = 0; i < Properties.Count; i++)
            {
                (string name, DzJsonNode node) = Properties[i];
                string ifType = i == 0 ? "if" : "else if";
                yield return new CSharpLineNode($"{ct}{ifType} (name.SequenceEqual(\"{name}\".AsSpan()))");
                yield return new CSharpLineNode($"{ct}{{");
                ct = context.IndentCSharp(+1);
                // skip {name}"
                yield return new CSharpLineNode($"{ct}json = json.Slice({1 + name.Length});");
                yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
                errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected ':'";
                yield return new CSharpLineNode($"{ct}if (json[0] != ':') throw new Exception($\"{errmsg}\");");
                yield return new CSharpLineNode($"{ct}json = json.Slice(1);");

                foreach (MethodNode methodNode in node.GetNodes(context))
                    yield return methodNode;
                ct = context.IndentCSharp(-1);
                yield return new CSharpLineNode($"{ct}}}");
            }

            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}if (json[0] == ',') json = json.Slice(1);");


            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");

            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");
            yield return new CSharpLineNode($"{ct}return obj;");

        }
    }

    class DzListNode : DzJsonNode
    {
        public string Type { get; }
        public DzJsonNode Property { get; set; }

        public DzListNode(string type)
        {
            Type = type;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}var NULL_SPAN = \"null\".AsSpan();");
            yield return new CSharpLineNode($"{ct}if (json.StartsWith(NULL_SPAN))");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}json = json.Slice(4);");
            yield return new CSharpLineNode($"{ct}return default({Type});");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            string errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected '['";
            yield return new CSharpLineNode($"{ct}if (json[0] != '[') throw new Exception($\"{errmsg}\");");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");

            // Parse list items

            yield return new CSharpLineNode($"{ct}System.Collections.Generic.List<{Type}> obj = new System.Collections.Generic.List<{Type}>();");
            yield return new CSharpLineNode($"{ct}while (true)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected object content or ']'";
            yield return new CSharpLineNode($"{ct}if (json.IsEmpty) throw new Exception($\"{errmsg}\");");
            yield return new CSharpLineNode($"{ct}if (json[0] == ']') break;");

            // list item
            foreach (MethodNode methodNode in Property.GetNodes(context))
                yield return methodNode;

            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}if (json[0] == ',') json = json.Slice(1);");

            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");
            yield return new CSharpLineNode($"{ct}return obj;");

        }
    }

    abstract class DzExpressionNode: DzJsonNode { }

    class DzCallNode : DzExpressionNode
    {
        public string Invocation { get; }

        public DzCallNode(string invocation)
        {
            Invocation = invocation;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            yield return new CSharpNode(Invocation);
        }
    }

    class DzAppendListNode : DzJsonNode
    {
        public string Owner { get; }
        public DzExpressionNode Expression { get; }

        public DzAppendListNode(string owner, DzExpressionNode expression)
        {
            Owner = owner;
            Expression = expression;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpNode($"{ct}{Owner}.Add(");
            foreach (MethodNode node in Expression.GetNodes(context))
                yield return node;
            yield return new CSharpLineNode($");");
        }
    }


    class DzAssignmentNode : DzJsonNode
    {
        public string Owner { get; }
        public DzExpressionNode Expression { get; }

        public DzAssignmentNode(string owner, DzExpressionNode expression)
        {
            Owner = owner;
            Expression = expression;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpNode($"{ct}{Owner} = ");
            foreach (MethodNode node in Expression.GetNodes(context))
                yield return node;
            yield return new CSharpLineNode($";");
        }
    }

    class DzTreeContext
    {
        public DzTreeContext()
        {
        }


        public string CSharpIndent { get; set; } = string.Empty;

        public string IndentCSharp(int delta)
        {
            CSharpIndent = Helpers.Indent(CSharpIndent, delta);
            return CSharpIndent;
        }
    }
}
