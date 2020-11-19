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
        public string Owner { get; }
        public List<(string, DzJsonNode)> Properties = new List<(string, DzJsonNode)>();

        public DzObjectNode(string type, string owner)
        {
            Type = type;
            Owner = owner;
        }

        public static IEnumerable<MethodNode> GetMethodHeaderNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}var NULL_SPAN = \"null\".AsSpan();");
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}if (json.SequenceEqual(NULL_SPAN))");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}{Owner} = null;");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}else");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            string errmsg = "Invalid JSON at position: {content.Length - json.Length}. Expected '{{'";
            yield return new CSharpLineNode($"{ct}if (json[0] != '{{') throw new Exception($\"{errmsg}\");");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");

            // Parse object properties

            yield return new CSharpLineNode($"{ct}{Owner} = new {Type}();");
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




            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");


        }
    }


    class DzNumNode : DzJsonNode
    {
        public string Type { get; }
        public string Owner { get; }

        public DzNumNode(string type, string owner)
        {
            Type = type;
            Owner = owner;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}int length = 0;");
            yield return new CSharpLineNode($"{ct}while (true)");
            yield return new CSharpLineNode($"{ct}{{");
            ct = context.IndentCSharp(+1);
            yield return new CSharpLineNode($"{ct}if (length >= json.Length) throw new Exception(\"Expected number\");");
            yield return new CSharpLineNode($"{ct}if (!char.IsDigit(json[length])) break;");
            yield return new CSharpLineNode($"{ct}++length;");
            ct = context.IndentCSharp(-1);
            yield return new CSharpLineNode($"{ct}}}");
            yield return new CSharpLineNode($"{ct}var valueStr = json.Slice(0, length).ToString();");
            yield return new CSharpLineNode($"{ct}{Owner} = {Type}.Parse(valueStr);");
            yield return new CSharpLineNode($"{ct}json = json.Slice(length);");
        }
    }

    class DzStringNode : DzJsonNode
    {
        public string Owner { get; }

        public DzStringNode(string owner)
        {
            Owner = owner;
        }

        public override IEnumerable<MethodNode> GetNodes(DzTreeContext context)
        {
            string ct = context.CSharpIndent;
            yield return new CSharpLineNode($"{ct}json = json.TrimStart();");
            yield return new CSharpLineNode($"{ct}if (json[0] != '\"') throw new Exception(\"Expected string\");");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1);");
            yield return new CSharpLineNode($"{ct}int vLength = json.IndexOf('\"');");
            yield return new CSharpLineNode($"{ct}{Owner} = json.Slice(0, vLength).ToString();");
            yield return new CSharpLineNode($"{ct}json = json.Slice(1 + vLength);");
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
