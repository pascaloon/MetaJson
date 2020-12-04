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
        private readonly HashSet<string> _generatedDeserializationMethods = new HashSet<string>();

        public DeserializeMethodGenerator(GeneratorExecutionContext context, IReadOnlyList<SerializableClass> knownClasses)
        {
            _context = context;
            _knownClasses = knownClasses;
        }

        public void GenerateDeserializeMethod(StringBuilder sb, DeserializeInvocation invocation)
        {
            string invocationTypeStr = invocation.TypeArg.ToString();
            if (_generatedDeserializationMethods.Contains(invocationTypeStr))
                return;
            _generatedDeserializationMethods.Add(invocationTypeStr);

            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}internal static void Deserialize(string content, out {invocationTypeStr} obj)");
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
                (_, DzJsonNode node) = (kvp.Key, kvp.Value);
                switch (node)
                {
                    case DzObjectNode objNode:
                        WriteDeserializeObject(sb, objNode);
                        break;
                    case DzListNode listNode:
                        WriteDeserializeList(sb, listNode);
                        break;
                }
            }
        }

        private void GenerateDeserializeMethodBody(StringBuilder sb, DeserializeInvocation invocation)
        {
            DzTreeContext treeContext = new DzTreeContext();
            treeContext.IndentCSharp(+3);
            string ct = treeContext.CSharpIndent;

            List<MethodNode> nodes = new List<MethodNode>();
            string invocationTypeStr = invocation.TypeArg.ToString();
            DzJsonNode dzTree = BuildDzTree(invocation.TypeArg, "obj");
            if (dzTree is null)
            {
                nodes.Add(new CSharpLineNode($"{ct}obj = default({invocationTypeStr});"));
                nodes.Add(new CSharpLineNode($"{ct}// Type '{invocationTypeStr}' isn't marked as serializable!"));
            }
            else
            {
                nodes.Add(new CSharpLineNode($"{ct}ReadOnlySpan<char> json = content.AsSpan().TrimStart();"));
                nodes.Add(new CSharpLineNode($"{ct}if (json.IsEmpty)"));
                nodes.Add(new CSharpLineNode($"{ct}{{"));
                ct = treeContext.IndentCSharp(+1);
                nodes.Add(new CSharpLineNode($"{ct}obj = default({invocationTypeStr});"));
                nodes.Add(new CSharpLineNode($"{ct}return;"));
                ct = treeContext.IndentCSharp(-1);
                nodes.Add(new CSharpLineNode($"{ct}}}"));

                nodes.Add(new CSharpNode($"{ct}obj = "));
                nodes.AddRange(dzTree.GetNodes(treeContext));
                nodes.Add(new CSharpLineNode($";"));
            }


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
                if (length >= json.Length || !char.IsDigit(json[length]))
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
            if (json.StartsWith(""null"".AsSpan()))
            {{
                json = json.Slice(4);
                return null;
            }}
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

        private void WriteDeserializeList(StringBuilder sb, DzListNode listNode)
        {
            string objectTypeStrValid = listNode.Type.Replace(".", "_").Replace("<", "_").Replace(">", "");

            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}private static System.Collections.Generic.List<{listNode.Type}> DeserializeList_{objectTypeStrValid}(ref string content, ref ReadOnlySpan<char> json)
        {{
");
            DzTreeContext treeContext = new DzTreeContext();
            treeContext.IndentCSharp(+3);

            foreach (CSharpNode node in listNode.GetNodes(treeContext).OfType<CSharpNode>())
            {
                sb.Append(node.CSharpCode);
            }

            sb.Append($@"
        }}

");
        }

        Dictionary<string, DzJsonNode> _requiredTypeDeserializers = new Dictionary<string, DzJsonNode>();
        private DzJsonNode BuildDzTree(ITypeSymbol symbol, string csObj)
        {
            if (symbol.Kind != SymbolKind.NamedType)
                return null;

            // primitive types
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Int32:
                    _deserializeIntRequired = true;
                    return new DzCallNode("DeserializeInt(ref content, ref json)");
                case SpecialType.System_String:
                    _deserializeStringRequired = true;
                    return new DzCallNode("DeserializeString(ref content, ref json)");
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
                        DzJsonNode value = BuildDzTree(sp.Type, $"obj.{sp.Name}");
                        if (value is DzExpressionNode expr)
                        {
                            DzAssignmentNode assignment = new DzAssignmentNode($"obj.{sp.Name}", expr);
                            objectNode.Properties.Add((sp.Name, assignment));
                        }
                        else throw new Exception("Expected expression for object property assignment!");
                    }

                    _requiredTypeDeserializers.Add(invocationTypeStr, objectNode);
                }
                string validName = invocationTypeStr.Replace(".", "_");
                return new DzCallNode($"Deserialize_{validName}(ref content, ref json)");
            }

            // list, dictionnary, ...

            // fallback on list
            INamedTypeSymbol enumerable = null;
            if (symbol.MetadataName.Equals("IList`1"))
                enumerable = symbol as INamedTypeSymbol;
            else
                enumerable = symbol.AllInterfaces.FirstOrDefault(i => i.MetadataName.Equals("IList`1"));
            if (enumerable != null)
            {
                ITypeSymbol listType = enumerable.TypeArguments.First();

                DzListNode listNode = new DzListNode(listType.ToString());
                DzJsonNode value = BuildDzTree(listType, $"obj");
                if (value is DzExpressionNode expr)
                {
                    listNode.Property = new DzAppendListNode($"obj", expr);
                }
                else throw new Exception("Expected expression for object list append!");

                _requiredTypeDeserializers.Add(invocationTypeStr, listNode);

                string objectTypeStrValid = listNode.Type.Replace(".", "_").Replace("<", "_").Replace(">", "");
                return new DzCallNode($"DeserializeList_{objectTypeStrValid}(ref content, ref json)");
            }

            // fallback on enumerables?

            // If reached here, type isn't supported
            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotSerializable, symbol.Locations.First(), invocationTypeStr));
            return null;
        }

        public void GenerateStubs(StringBuilder sb)
        {
            foreach (SerializableClass sc in _knownClasses)
            {
                string typeStr = sc.Type.ToString();
                if (_generatedDeserializationMethods.Contains(typeStr))
                    continue;

                // missing serialization method, create a stub one for intellisense
                const string SPC = "    ";
                sb.AppendLine($@"{SPC}{SPC}internal static void Deserialize(string content, out {typeStr} obj) {{obj = default({typeStr});}}");

            }
        }

    }
}
