using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaJson
{
    class SerializeMethodGenerator
    {
        private readonly GeneratorExecutionContext _context;
        private readonly IReadOnlyList<SerializableClass> _knownClasses;
        private readonly HashSet<string> _generatedSerializationMethods = new HashSet<string>();

        public SerializeMethodGenerator(GeneratorExecutionContext context, IReadOnlyList<SerializableClass> knownClasses)
        {
            _context = context;
            _knownClasses = knownClasses;
        }

        public void GenerateSerializeMethod(StringBuilder sb, SerializeInvocation invocation)
        {
            string invocationTypeStr = invocation.TypeArg.ToString();
            if (_generatedSerializationMethods.Contains(invocationTypeStr))
                return;
            _generatedSerializationMethods.Add(invocationTypeStr);

            string refprefix = "";
            if (ShouldUseRef(invocation.TypeArg))
                refprefix = "ref ";

            const string SPC = "    ";
            sb.Append($@"{SPC}{SPC}internal static string Serialize({refprefix}{invocationTypeStr} obj)");
            sb.Append(@"
        {
");
            GenerateSerializeMethodBody(sb, invocation);
            sb.Append(@"
        }

");
        }

        private void GenerateSerializeMethodBody(StringBuilder sb, SerializeInvocation invocation)
        {
            // Create nodes
            TreeContext treeContext = new TreeContext();
            treeContext.IndentCSharp(+3);
            string ct = treeContext.CSharpIndent;

            List<MethodNode> nodes = new List<MethodNode>();
            nodes.Add(new CSharpLineNode($"{ct}StringBuilder sb = new StringBuilder();"));

            JsonNode jsonTree = BuildTree(invocation.TypeArg, "obj");
            if (jsonTree is null)
            {
                string invocationTypeStr = invocation.TypeArg.ToString();
                nodes.Add(new CSharpLineNode($"{ct}// Type '{invocationTypeStr}' isn't marked as serializable!"));
            }
            else
            {
                nodes.AddRange(jsonTree.GetNodes(treeContext));
            }

            ct = treeContext.CSharpIndent;
            nodes.Add(new CSharpLineNode($"{ct}return sb.ToString();"));

            // Merge json nodes
            List<MethodNode> mergedNodes = new List<MethodNode>();
            List<PlainJsonNode> streak = new List<PlainJsonNode>();
            foreach (MethodNode node in nodes)
            {
                if (node is PlainJsonNode js)
                {
                    streak.Add(js);
                }
                else
                {
                    if (streak.Count > 0)
                    {
                        mergedNodes.Add(MergeJsonNodes(streak));
                        streak.Clear();
                    }
                    mergedNodes.Add(node);
                }

            }

            if (streak.Count > 0)
            {
                mergedNodes.Add(MergeJsonNodes(streak.OfType<PlainJsonNode>()));
            }

            nodes = mergedNodes;

            // Convert json to C#
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i] is PlainJsonNode js)
                {
                    CSharpNode cs = new CSharpLineNode($"{js.CSharpIndent}sb.Append(\"{js.Value.Replace("\"", "\\\"")}\");");
                    nodes.RemoveAt(i);
                    nodes.Insert(i, cs);
                }
            }

            // Output as string
            foreach (MethodNode node in mergedNodes)
            {
                if (node is CSharpNode cs)
                {
                    sb.Append(cs.CSharpCode);
                }
            }
        }

        private JsonNode BuildTree(ITypeSymbol symbol, string csObj)
        {
            if (symbol.Kind != SymbolKind.NamedType)
                return null;

            // primitive types
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Int32:
                    return new NumericNode(csObj);
                case SpecialType.System_String:
                    return new StringNode(csObj);
                default:
                    break;
            }

            // if is serializable class
            string invocationTypeStr = symbol.ToString();
            SerializableClass foundClass = _knownClasses.FirstOrDefault(c => c.Type.ToString().Equals(invocationTypeStr));
            if (foundClass != null)
            {
                ObjectNode objectNode = new ObjectNode(csObj);
                objectNode.CanBeNull = foundClass.CanBeNull;
                foreach (SerializableProperty sp in foundClass.Properties)
                {
                    JsonNode value = BuildTree(sp.Type, $"{csObj}.{sp.Name}");
                    if (value is NullableNode nn)
                        nn.CanBeNull = sp.CanBeNull;
                    if (value is ListNode ln && ln.ElementType is NullableNode lnn)
                        lnn.CanBeNull = lnn.CanBeNull && sp.ArrayItemCanBeNull;
                    objectNode.Properties.Add((sp.Name, value));
                }
                return objectNode;
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

                ListNode listNode = new ListNode(csObj);
                listNode.ElementType = BuildTree(listType, $"{csObj}[i]");
                return listNode;
            }

            // fallback on enumerables?

            // If reached here, type isn't supported
            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotSerializable, symbol.Locations.First(), invocationTypeStr));
            return null;
        }

        private PlainJsonNode MergeJsonNodes(IEnumerable<PlainJsonNode> nodes)
        {
            string combined = String.Join("", nodes.Select(n => n.Value));
            return new PlainJsonNode(nodes.First().CSharpIndent, combined);
        }

        public void GenerateStubs(StringBuilder sb)
        {
            foreach (SerializableClass sc in _knownClasses)
            {
                string typeStr = sc.Type.ToString();
                if (_generatedSerializationMethods.Contains(typeStr))
                    continue;

                string refprefix = "";
                if (ShouldUseRef(sc.Type))
                    refprefix = "ref ";

                // missing serialization method, create a stub one for intellisense
                const string SPC = "    ";
                sb.AppendLine($@"{SPC}{SPC}internal static string Serialize({refprefix}{typeStr} obj) {{ return String.Empty; }}");

            }
        }

        private bool ShouldUseRef(ITypeSymbol type)
        {
            if (!type.IsValueType)
                return false;
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return false;
            }

            return true;
        }
    }
}
