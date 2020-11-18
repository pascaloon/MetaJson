using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace MetaJson
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor ClassNotSerializable = new DiagnosticDescriptor(
            id: "MJ-001", 
            title: "Class Not Serializable", 
            messageFormat: "Class '{0}' is not found or not set as serializable", 
            category: "MetaJson.Serialization", 
            defaultSeverity: DiagnosticSeverity.Error, 
            isEnabledByDefault: true);
    }

    [Generator]
    public class MetaJsonSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            List<SerializableClass> serializableClasses = new List<SerializableClass>();
            List<SerializeInvocation> serializeInvocations = new List<SerializeInvocation>();
            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = context.Compilation.GetSemanticModel(tree);
                FindClassesAndInvocationsWalker walk = new FindClassesAndInvocationsWalker(semanticModel, context);
                walk.Visit(tree.GetRoot());
                serializableClasses.AddRange(walk.SerializableClasses);
                serializeInvocations.AddRange(walk.SerializeInvocations);
            }

            // Create methods!
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"
using System;
using System.Text;

namespace MetaJson
{
    public static class DummySymbol {public static void DoNothing() {}}

    public static class MetaJsonSerializer
    {"
);

            const string SPC = "    ";
            foreach (SerializeInvocation invocation in serializeInvocations)
            {
                string invocationTypeStr = invocation.TypeArg.ToString();

                sb.Append($@"{SPC}{SPC}public static string Serialize<T>({invocationTypeStr} obj) where T: {invocationTypeStr}");
                sb.Append(@"
        {
");
                GenerateMethodBody(sb, invocation, serializableClasses, context);
                sb.Append(@"
        }

");
            }

            // Class footer
            sb.Append(@"
    }
}"
);
            string generatedFileSource = sb.ToString();
            if (Debugger.IsAttached)
            {
                // ONLY WHEN DEBUGGING WITH DRIVER APP
                Console.WriteLine("Generated Sources:");
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine(generatedFileSource);
                Console.WriteLine("----------------------------------------------------------");
            }
            context.AddSource("MetaJsonSerializer", SourceText.From(generatedFileSource, Encoding.UTF8));
        }

        JsonNode BuildTree(ITypeSymbol symbol, string csObj, List<SerializableClass> knownClasses, GeneratorExecutionContext context)
        {
            if (symbol.Kind != SymbolKind.NamedType)
                return null;


            // primitive types
            string invocationTypeStr = symbol.ToString();
            switch (invocationTypeStr)
            {
                case "int":
                    return new NumericNode(csObj);
                case "string":
                    return new StringNode(csObj);
                default:
                    break;
            }


            

            // if is serializable class
            SerializableClass foundClass = knownClasses.FirstOrDefault(c => c.Type.ToString().Equals(invocationTypeStr));
            if (foundClass != null)
            {
                ObjectNode objectNode = new ObjectNode();
                foreach (SerializableProperty sp in foundClass.Properties)
                {
                    JsonNode value = BuildTree(sp.Symbol.Type, $"{csObj}.{sp.Name}", knownClasses, context);
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
                listNode.ElementType = BuildTree(listType, $"{csObj}[i]", knownClasses, context);
                return listNode;
            }

            // fallback on enumerables?


            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotSerializable, symbol.Locations.First(), invocationTypeStr));
            return null;
        }


        void GenerateMethodBody(StringBuilder sb, SerializeInvocation invocation, List<SerializableClass> knownClasses, GeneratorExecutionContext context)
        {
            // Create nodes
            TreeContext treeContext = new TreeContext();
            treeContext.IndentCSharp(+3);
            string ct = treeContext.CSharpIndent;

            List<MethodNode> nodes = new List<MethodNode>();
            nodes.Add(new CSharpLineNode($"{ct}StringBuilder sb = new StringBuilder();"));

            JsonNode jsonTree = BuildTree(invocation.TypeArg, "obj", knownClasses, context);
            nodes.AddRange(jsonTree.GetNodes(treeContext));
            
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

        PlainJsonNode MergeJsonNodes(IEnumerable<PlainJsonNode> nodes)
        {
            string combined = String.Join("", nodes.Select(n => n.Value));
            return new PlainJsonNode(nodes.First().CSharpIndent, combined);
        }


        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    class SerializableClass
    {
        public string Name { get; set; }
        public ClassDeclarationSyntax Declaration { get; set; }
        public List<SerializableProperty> Properties { get; set; } = new List<SerializableProperty>();
        public INamedTypeSymbol Type { get; set; }
    }

    class SerializableProperty
    {
        public string Name { get; set; }
        public IPropertySymbol Symbol { get; set; }

    }

    class SerializeInvocation
    {
        public InvocationExpressionSyntax Invocation { get; set; }
        public ITypeSymbol TypeArg { get; set; }
    }

    class ClassWalkerState
    {
        public SerializableClass CurrentClass { get; set; } = null;
        public bool IsSerializable => CurrentClass != null;
    }
}
