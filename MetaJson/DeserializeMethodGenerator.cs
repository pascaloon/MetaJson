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

            nodes.AddRange(DzObjectNode.GetMethodHeaderNodes(treeContext));

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

        private DzJsonNode BuildDzTree(ITypeSymbol symbol, string csObj)
        {
            if (symbol.Kind != SymbolKind.NamedType)
                return null;

            // primitive types
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Int32:
                    return new DzNumNode("int", csObj);
                case SpecialType.System_String:
                    return new DzStringNode(csObj);
                default:
                    break;
            }

            // if is serializable class
            string invocationTypeStr = symbol.ToString();
            SerializableClass foundClass = _knownClasses.FirstOrDefault(c => c.Type.ToString().Equals(invocationTypeStr));
            if (foundClass != null)
            {
                DzObjectNode objectNode = new DzObjectNode(invocationTypeStr, csObj);
                foreach (SerializableProperty sp in foundClass.Properties)
                {
                    DzJsonNode value = BuildDzTree(sp.Symbol.Type, $"{csObj}.{sp.Name}");
                    objectNode.Properties.Add((sp.Name, value));
                }
                return objectNode;
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
