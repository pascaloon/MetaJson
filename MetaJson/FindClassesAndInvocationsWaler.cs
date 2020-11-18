using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace MetaJson
{
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
