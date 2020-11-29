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
            // Find declared serializable classes and (de)serialization invocations
            List<SerializableClass> serializableClasses = new List<SerializableClass>();
            List<SerializeInvocation> serializeInvocations = new List<SerializeInvocation>();
            List<DeserializeInvocation> deserializeInvocations = new List<DeserializeInvocation>();
            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                SemanticModel semanticModel = context.Compilation.GetSemanticModel(tree);
                FindClassesAndInvocationsWalker walk = new FindClassesAndInvocationsWalker(semanticModel, context);
                walk.Visit(tree.GetRoot());
                serializableClasses.AddRange(walk.SerializableClasses);
                serializeInvocations.AddRange(walk.SerializeInvocations);
                deserializeInvocations.AddRange(walk.DeserializeInvocations);
            }

            // Start C# generation!
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"
using System;
using System.Diagnostics;
using System.Text;

namespace MetaJson
{
    internal static class DummySymbol {public static void DoNothing() {}}

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class SerializeAttribute: Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    internal sealed class NotNull: Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class ArrayItemNotNull: Attribute { }

    internal static class MetaJsonSerializer
    {"
);
            sb.AppendLine(@"
        #region SERIALIZATION
");

            // Serialize method definitions
            SerializeMethodGenerator smg = new SerializeMethodGenerator(context, serializableClasses);
            foreach (SerializeInvocation invocation in serializeInvocations)
            {
                smg.GenerateSerializeMethod(sb, invocation);
            }

            smg.GenerateStubs(sb);

            sb.AppendLine(@"
        #endregion
");
            sb.AppendLine(@"
        #region DESERIALIZATION
");


            // Deserialize method definitions
            DeserializeMethodGenerator dsmg = new DeserializeMethodGenerator(context, serializableClasses);
            foreach (DeserializeInvocation invocation in deserializeInvocations)
            {
                dsmg.GenerateDeserializeMethod(sb, invocation);
            }

            dsmg.GenerateStubs(sb);
            dsmg.GenerateClassResources(sb);

            sb.AppendLine(@"
        #endregion");

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
            context.AddSource("MetaJsonSerializer.g", SourceText.From(generatedFileSource, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }

    class SerializableClass
    {
        public string Name { get; set; }
        public TypeDeclarationSyntax Declaration { get; set; }
        public List<SerializableProperty> Properties { get; set; } = new List<SerializableProperty>();
        public INamedTypeSymbol Type { get; set; }
        public bool CanBeNull { get; set; }
    }

    class SerializableProperty
    {
        public string Name { get; set; }
        public ITypeSymbol Type { get; set; }
        public bool CanBeNull { get; set; }
        public bool ArrayItemCanBeNull { get; set; }
    }

    class SerializeInvocation
    {
        public InvocationExpressionSyntax Invocation { get; set; }
        public ITypeSymbol TypeArg { get; set; }
    }

    class DeserializeInvocation
    {
        public InvocationExpressionSyntax Invocation { get; set; }
        public ITypeSymbol TypeArg { get; set; }
    }
}
