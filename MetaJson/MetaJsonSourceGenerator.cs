﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                FindClassesAndInvocationsWaler walk = new FindClassesAndInvocationsWaler(semanticModel, context);
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
                string invocationTypeStr = invocation.TypeArg.Symbol.ToString();

                SerializableClass sc = serializableClasses.FirstOrDefault(c => c.Type.ToString().Equals(invocationTypeStr));
                if (sc == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ClassNotSerializable, invocation.Invocation.GetLocation(), invocationTypeStr));
                    continue;
                }


                sb.Append($@"{SPC}{SPC}public static string Serialize<T>({invocationTypeStr} obj) where T: {invocationTypeStr}");
                sb.Append(@"
        {
");
                GenerateMethodBody(sb, sc);
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

        void GenerateMethodBody(StringBuilder sb, SerializableClass sc)
        {
            // Create nodes
            List<MethodNode> nodes = new List<MethodNode>();
            nodes.Add(new CsharpTabControlNode(+3));
            nodes.Add(new CSharpLineNode("StringBuilder sb = new StringBuilder();"));
            nodes.AddRange(GetClassNodes(sc));
            nodes.Add(new CSharpLineNode("return sb.ToString();"));


            // Fix Json Tabs
            string jsonTab = "";
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i] is JsonTabControlNode jstc)
                {
                    jsonTab = UpdateTab(jsonTab, jstc.Delta);
                    nodes.RemoveAt(i);
                    --i;
                }
                else if (nodes[i] is JsonNode js)
                {
                    js.Value = js.Value.Replace("$t", jsonTab);
                }
            }

            // Merge json nodes
            List<MethodNode> mergedNodes = new List<MethodNode>();
            List<JsonNode> streak = new List<JsonNode>();
            foreach (MethodNode node in nodes)
            {
                if (node is JsonNode js)
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
                mergedNodes.Add(MergeJsonNodes(streak.OfType<JsonNode>()));
            }

            nodes = mergedNodes;

            // Convert json to C#
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i] is JsonNode js)
                {
                    CSharpNode cs = new CSharpLineNode($"sb.Append(\"{js.Value.Replace("\"", "\\\"")}\");");
                    nodes.RemoveAt(i);
                    nodes.Insert(i, cs);
                }
            }

            // Fix C# Tabs
            string csharpTab = "";
            for (int i = 0; i < nodes.Count; ++i)
            {
                if (nodes[i] is CsharpTabControlNode cstc)
                {
                    csharpTab = UpdateTab(csharpTab, cstc.Delta);
                }
                else if (nodes[i] is CSharpNode cs)
                {
                    cs.CSharpCode = cs.CSharpCode.Replace("$t", csharpTab);
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

        IEnumerable<MethodNode> GetClassNodes(SerializableClass sc)
        {
            yield return new JsonNode("{\\n");
            yield return new JsonTabControlNode(+1);

            for (int i = 0; i < sc.Properties.Count; ++i)
            {
                SerializableProperty sp = sc.Properties[i];

                // Json property name is the class' property name
                yield return new JsonNode($"$t\"{sp.Name}\": ");

                // Json property value varies depending on its type
                foreach (MethodNode jsonValueNode in sp.ValueSerializer.GetValueNodes($"obj.{sp.Name}"))
                    yield return jsonValueNode;

                // If last element, don't add a ','
                if (i < sc.Properties.Count - 1)
                    yield return new JsonNode($",\\n");
                else
                    yield return new JsonNode($"\\n");
            }

            yield return new JsonTabControlNode(-1);
            yield return new JsonNode("}");
        }

        JsonNode MergeJsonNodes(IEnumerable<JsonNode> nodes)
        {
            string combined = String.Join("", nodes.Select(n => n.Value));
            return new JsonNode(combined);
        }

        string UpdateTab(string origin, int delta)
        {
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    origin += "    ";
                }
            }
            else if (delta < 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    origin = origin.Substring(origin.Length - 4);
                }
            }
            return origin;
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
        public string ValueType { get; set; }
        public PropertyDeclarationSyntax Declaration { get; set; }
        public SerializablePropertyValue ValueSerializer { get; set; }

    }

    class SerializeInvocation
    {
        public InvocationExpressionSyntax Invocation { get; set; }
        public SymbolInfo TypeArg { get; set; }
    }

    class ClassWalkerState
    {
        public SerializableClass CurrentClass { get; set; } = null;
        public bool IsSerializable => CurrentClass != null;
    }

    class FindClassesAndInvocationsWaler : CSharpSyntaxWalker
    {
        public List<SerializableClass> SerializableClasses { get; set; } = new List<SerializableClass>();
        public List<SerializeInvocation> SerializeInvocations { get; set; } = new List<SerializeInvocation>();

        Stack<ClassWalkerState> _currentClassStack = new Stack<ClassWalkerState>();
        ClassWalkerState _currentClassState;

        private readonly SemanticModel _semanticModel;
        private readonly GeneratorExecutionContext _context;

        public FindClassesAndInvocationsWaler(SemanticModel semanticModel, GeneratorExecutionContext context)
        {
            _semanticModel = semanticModel;
            _context = context;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _currentClassState = new ClassWalkerState();
            bool isSerializable = false;
            foreach (AttributeListSyntax attrList in node.AttributeLists)
            {
                foreach (AttributeSyntax attr in attrList.Attributes)
                {
                    string name = attr.Name.ToString();
                    if (name == "Serialize" || name == "MetaJson.Serialize")
                    {
                        isSerializable = true;
                        break;
                    }
                }
                if (isSerializable)
                    break;
            }

            if (isSerializable)
            {
                INamedTypeSymbol type = _semanticModel.GetDeclaredSymbol(node);
                SerializableClass sc = new SerializableClass()
                {
                    Name = node.Identifier.ValueText,
                    Declaration = node,
                    Type = type
                };
                _currentClassState.CurrentClass = sc;
                SerializableClasses.Add(sc);
            }

            _currentClassStack.Push(_currentClassState);

            base.VisitClassDeclaration(node);

            _currentClassStack.Pop();
            _currentClassState = _currentClassStack.Count > 0 ? _currentClassStack.Peek() : null;
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (_currentClassState.IsSerializable)
            {
                bool isSerializable = false;

                foreach (AttributeListSyntax attributeList in node.AttributeLists)
                {
                    foreach (AttributeSyntax attribute in attributeList.Attributes)
                    {
                        string name = attribute.Name.ToString();
                        if (name == "Serialize" || name == "MetaJson.Serialize")
                        {
                            isSerializable = true;
                            SymbolInfo typeArg = _semanticModel.GetSymbolInfo(node.Type);
                            string typeString = typeArg.Symbol.ToString();
                            SerializablePropertyValue ser = null;
                            if (typeString == "string")
                                ser = new StringSerializablePropertyValue();
                            else if (typeString == "int")
                                ser = new NumSerializablePropertyValue();
                            else
                                ser = new SimpleSerializablePropertyValue();

                            _currentClassState.CurrentClass.Properties.Add(new SerializableProperty() 
                            { 
                                Name = node.Identifier.ValueText,
                                Declaration = node,
                                ValueSerializer = ser
                            });

                            break;
                        }
                    }
                    if (isSerializable)
                        break;
                }
            }
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccessSyntax
                && memberAccessSyntax.Expression.ToString().Equals("MetaJsonSerializer"))
            {
                // Calling MetaJsonSerializer static methods
                if (memberAccessSyntax.Name is GenericNameSyntax generic && generic.Identifier.ValueText.ToString().Equals("Serialize"))
                {
                    if (node.ArgumentList.Arguments.Count == 1 && generic.TypeArgumentList.Arguments.Count == 1)
                    {
                        TypeSyntax type = generic.TypeArgumentList.Arguments.First();
                        SymbolInfo argSymbol = _semanticModel.GetSymbolInfo(type);
                        SerializeInvocations.Add(new SerializeInvocation()
                        {
                            Invocation = node,
                            TypeArg = argSymbol
                        });
                    }
                    else
                    {
                        // error
                    }
                }
            }

            base.VisitInvocationExpression(node);
        }
    }
}
