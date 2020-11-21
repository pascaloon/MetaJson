using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaJson
{
    class DeserializeMethodGenerator
    {
        private readonly GeneratorExecutionContext _context;
        private readonly IReadOnlyList<SerializableClass> _knownClasses;

        public DeserializeMethodGenerator(GeneratorExecutionContext context, IReadOnlyList<SerializableClass> knownClasses)
        {
            _context = context;
            _knownClasses = knownClasses;
        }

        public void GenerateDeserializeMethod(StringBuilder sb, DeserializeInvocation invocation)
        {
            string invocationTypeStr = invocation.TypeArg.ToString();
            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}public static {invocationTypeStr} Deserialize<T>(string content) where T: {invocationTypeStr}");
            sb.Append(@"
        {
");
            GenerateDeserializeMethodBody(sb, invocation);
            sb.Append(@"
        }

");
        }

        public void GenerateClassResources(StringBuilder sb)
        {
            if (_deserializeIntRequired)
                WriteDeserializeInt(sb);
            if (_deserializeStringRequired)
                WriteDeserializeString(sb);
            foreach (var kvp in _requiredTypeDeserializers)
            {
                (_, DzObjectNode node) = (kvp.Key, kvp.Value);
                WriteDeserializeObject(sb, node);
            }
        }

        private void GenerateDeserializeMethodBody(StringBuilder sb, DeserializeInvocation invocation)
        {
            DzTreeContext treeContext = new DzTreeContext();
            treeContext.IndentCSharp(+3);
            string ct = treeContext.CSharpIndent;

            List<MethodNode> nodes = new List<MethodNode>();
            string invocationTypeStr = invocation.TypeArg.ToString();
            nodes.Add(new CSharpLineNode($"{ct}ReadOnlySpan<char> json = content.AsSpan();"));
            nodes.Add(new CSharpLineNode($"{ct}if (json.Trim().IsEmpty)"));
            ct = treeContext.IndentCSharp(+1);
            nodes.Add(new CSharpLineNode($"{ct}return null;"));
            ct = treeContext.IndentCSharp(-1);

            nodes.Add(new CSharpLineNode($"{ct}{invocationTypeStr} obj;"));
            DzJsonNode dzTree = BuildDzTree(invocation.TypeArg, "obj");
            nodes.AddRange(dzTree.GetNodes(treeContext));
            nodes.Add(new CSharpLineNode($"{ct}return obj;"));


            //nodes.Add(new CSharpLineNode($"{ct}return new {invocationTypeStr}();"));

            foreach (CSharpNode node in nodes.OfType<CSharpNode>())
            {
                sb.Append(node.CSharpCode);
            }

        }

        private bool _deserializeIntRequired = false;
        private void WriteDeserializeInt(StringBuilder sb)
        {
            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}private static int DeserializeInt(ref string content, ref ReadOnlySpan<char> json)
        {{
            json = json.TrimStart();
            int length = 0;
            while (true)
            {{
                if (length >= json.Length) 
                    throw new Exception(""Expected number"");
                if (!char.IsDigit(json[length]))
                    break;
                ++length;
            }}
            if (length == 0) 
                throw new Exception(""Expected number"");
            var valueStr = json.Slice(0, length).ToString();
            int v = int.Parse(valueStr);
            json = json.Slice(length);
            return v;
        }}

");
        }

        private bool _deserializeStringRequired = false;
        private void WriteDeserializeString(StringBuilder sb)
        {
            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}private static string DeserializeString(ref string content, ref ReadOnlySpan<char> json)
        {{
            json = json.TrimStart();
            if (json[0] != '""')
                throw new Exception(""Expected string"");
            json = json.Slice(1);
            int vLength = json.IndexOf('""');
            string v = json.Slice(0, vLength).ToString();
            json = json.Slice(1 + vLength);
            return v;
        }}

");
        }

        private void WriteDeserializeObject(StringBuilder sb, DzObjectNode objNode)
        {
            string objectTypeStrValid = objNode.Type.Replace(".", "_");

            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}private static {objNode.Type} Deserialize_{objectTypeStrValid}(ref string content, ref ReadOnlySpan<char> json)
        {{
");
            DzTreeContext treeContext = new DzTreeContext();
            treeContext.IndentCSharp(+3);

            foreach (CSharpNode node in objNode.GetNodes(treeContext).OfType<CSharpNode>())
            {
                sb.Append(node.CSharpCode);
            }

            sb.Append($@"
        }}

");
        }


        Dictionary<string, DzObjectNode> _requiredTypeDeserializers = new Dictionary<string, DzObjectNode>();
        private DzJsonNode BuildDzTree(ITypeSymbol symbol, string csObj)
        {
            if (symbol.Kind != SymbolKind.NamedType)
                return null;

            // primitive types
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Int32:
                    _deserializeIntRequired = true;
                    return new DzCallNode(csObj, "DeserializeInt(ref content, ref json)");
                case SpecialType.System_String:
                    _deserializeStringRequired = true;
                    return new DzCallNode(csObj, "DeserializeString(ref content, ref json)");
                default:
                    break;
            }

            // if is serializable class
            string invocationTypeStr = symbol.ToString();
            SerializableClass foundClass = _knownClasses.FirstOrDefault(c => c.Type.ToString().Equals(invocationTypeStr));
            if (foundClass != null)
            {
                if (!_requiredTypeDeserializers.ContainsKey(invocationTypeStr))
                {
                    DzObjectNode objectNode = new DzObjectNode(invocationTypeStr);
                    foreach (SerializableProperty sp in foundClass.Properties)
                    {
                        DzJsonNode value = BuildDzTree(sp.Symbol.Type, $"obj.{sp.Name}");
                        objectNode.Properties.Add((sp.Name, value));
                    }

                    _requiredTypeDeserializers.Add(invocationTypeStr, objectNode);
                }
                string validName = invocationTypeStr.Replace(".", "_");
                return new DzCallNode(csObj, $"Deserialize_{validName}(ref content, ref json)");
            }

            // list, dictionnary, ...

            // fallback on list
            //INamedTypeSymbol enumerable = null;
            //if (symbol.MetadataName.Equals("IList`1"))
            //    enumerable = symbol as INamedTypeSymbol;
            //else
            //    enumerable = symbol.AllInterfaces.FirstOrDefault(i => i.MetadataName.Equals("IList`1"));
            //if (enumerable != null)
            //{
            //    ITypeSymbol listType = enumerable.TypeArguments.First();

            //    ListNode listNode = new ListNode(csObj);
            //    listNode.ElementType = BuildTree(listType, $"{csObj}[i]");
            //    return listNode;
            //}

            // fallback on enumerables?

            // If reached here, type isn't supported
            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotSerializable, symbol.Locations.First(), invocationTypeStr));
            return null;
        }

    }
}
