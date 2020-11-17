using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MetaJson
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor SerializableClass = new DiagnosticDescriptor(
            id: "MJ-001", 
            title: "Class Serialization Available", 
            messageFormat: "Class '{0}' is serializable", 
            category: "MetaJson.Serializables", 
            defaultSeverity: DiagnosticSeverity.Warning, 
            isEnabledByDefault: true);
        public static readonly DiagnosticDescriptor SerializableProperty = new DiagnosticDescriptor(
           id: "MJ-002",
           title: "Property Serialization Available",
           messageFormat: "Property '{0}' is serializable",
           category: "MetaJson.Serializables",
           defaultSeverity: DiagnosticSeverity.Warning,
           isEnabledByDefault: true);
    }

    [Generator]
    public class MetaJsonSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            List<SerializableClass> serializableClasses = new List<SerializableClass>();
            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                FindClassesAndInvocationsWaler walk = new FindClassesAndInvocationsWaler();
                walk.Visit(tree.GetRoot());
                serializableClasses.AddRange(walk.SerializableClasses);
            }

            foreach (SerializableClass sc in serializableClasses)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SerializableClass, sc.Declaration.Identifier.GetLocation(), sc.Name));
                foreach (SerializableProperty sp in sc.Properties)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SerializableProperty, sp.Declaration.Identifier.GetLocation(), sp.Name));
                }
            }
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
    }

    class SerializableProperty
    {
        public string Name { get; set; }
        public string ValueType { get; set; }
        public PropertyDeclarationSyntax Declaration { get; set; }

    }

    abstract class SerializablePropertyType
    {
        abstract public string GetStringValue(string id);
    }

    class StringSerializablePropertyType : SerializablePropertyType
    {
        public override string GetStringValue(string id) => id;
    }

    class SimpleSerializablePropertyType : SerializablePropertyType
    {
        public override string GetStringValue(string id) => $"{id}.ToString()";
    }

    class ClassWalkerState
    {
        public SerializableClass CurrentClass { get; set; } = null;
        public bool IsSerializable => CurrentClass != null;
    }

    class FindClassesAndInvocationsWaler : CSharpSyntaxWalker
    {
        public List<SerializableClass> SerializableClasses { get; set; } = new List<SerializableClass>();

        Stack<ClassWalkerState> _currentClassStack = new Stack<ClassWalkerState>();
        ClassWalkerState _currentClassState;

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
                SerializableClass sc = new SerializableClass()
                {
                    Name = node.Identifier.ValueText,
                    Declaration = node
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
                            _currentClassState.CurrentClass.Properties.Add(new SerializableProperty() 
                            { 
                                Name = node.Identifier.ValueText,
                                Declaration = node
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
    }
}
