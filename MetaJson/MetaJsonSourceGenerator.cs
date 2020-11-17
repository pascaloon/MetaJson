using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(""{"");
");

                for (int i = 0; i <sc.Properties.Count; ++i)
                {
                    SerializableProperty prop = sc.Properties[i];
                    sb.Append($@"
            sb.Append(""    \""{prop.Name}\"": "");
");
                    sb.Append($"{SPC}{SPC}{SPC}");
                    sb.Append(prop.ValueSerializer.GetStringValue($"obj.{prop.Name}"));

                    //sb.Append($"{SPC}{SPC}{SPC}{SPC}\"{prop.Name}\": obj.{prop.Name}");
                    if (i < sc.Properties.Count - 1)
                        sb.Append($@"
            sb.AppendLine("","");
");
                    else
                        sb.Append($@"
            sb.AppendLine();
");

                }

                sb.AppendLine(@"
            sb.Append(""}"");
            return sb.ToString();
        }
");
            }

            // Class footer
            sb.AppendLine(@"
    }
}"
);
            context.AddSource("MetaJsonSerializer", SourceText.From(sb.ToString(), Encoding.UTF8));


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
        public SerializablePropertyType ValueSerializer { get; set; }

    }

    class SerializeInvocation
    {
        public InvocationExpressionSyntax Invocation { get; set; }
        public SymbolInfo TypeArg { get; set; }
    }

    abstract class SerializablePropertyType
    {
        abstract public string GetStringValue(string id);
    }

    class StringSerializablePropertyType : SerializablePropertyType
    {
        public override string GetStringValue(string id) => $"sb.Append(\"\\\"\");sb.Append({id});sb.Append(\"\\\"\");";
    }

    class NumSerializablePropertyType : SerializablePropertyType
    {
        public override string GetStringValue(string id) => $"sb.Append({id});";
    }

    class SimpleSerializablePropertyType : SerializablePropertyType
    {
        public override string GetStringValue(string id) => $"sb.Append(\"\\\"\");sb.Append({id}.ToString());sb.Append(\"\\\"\");";
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
                            SerializablePropertyType ser = null;
                            if (typeString == "string")
                                ser = new StringSerializablePropertyType();
                            else if (typeString == "int")
                                ser = new NumSerializablePropertyType();
                            else
                                ser = new SimpleSerializablePropertyType();

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
