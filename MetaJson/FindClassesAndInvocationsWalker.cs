using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MetaJson
{
    class FindClassesAndInvocationsWalker : CSharpSyntaxWalker
    {
        public List<SerializableClass> SerializableClasses { get; set; } = new List<SerializableClass>();
        public List<SerializeInvocation> SerializeInvocations { get; set; } = new List<SerializeInvocation>();
        public List<DeserializeInvocation> DeserializeInvocations { get; set; } = new List<DeserializeInvocation>();

        private readonly SemanticModel _semanticModel;
        private readonly GeneratorExecutionContext _context;

        public FindClassesAndInvocationsWalker(SemanticModel semanticModel, GeneratorExecutionContext context)
        {
            _semanticModel = semanticModel;
            _context = context;
        }

        private void VisitClassOrStructDeclaration(TypeDeclarationSyntax node)
        {
            bool isSerializable = false;

            // Start by a quick check
            foreach (AttributeListSyntax attrList in node.AttributeLists)
            {
                foreach (AttributeSyntax attr in attrList.Attributes)
                {
                    string name = attr.Name.ToString();
                    if (name.Contains("Serialize"))
                    {
                        isSerializable = true;
                        break;
                    }
                }
                if (isSerializable)
                    break;
            }

            if (!isSerializable)
            {
                return;
            }

            INamedTypeSymbol type = _semanticModel.GetDeclaredSymbol(node);

            // Can't do Proper check if we generate the attribute
            //isSerializable = type.GetAttributes().Any(a => a.AttributeClass.ToString().Equals("MetaJson.SerializeAttribute"));
            //if (!isSerializable)
            //{
            //    base.VisitClassDeclaration(node);
            //    return;
            //}


            SerializableClass sc = new SerializableClass()
            {
                Name = node.Identifier.ValueText,
                Declaration = node,
                Type = type,
                CanBeNull = !type.IsValueType && !type.GetAttributes().Any(a => a.AttributeClass.ToString().Contains("NotNull"))
            };

            List<IPropertySymbol> serializableProperties = type.GetMembers()
                .OfType<IPropertySymbol>()
                //.Where(p => p.GetAttributes().Any(a => a.AttributeClass.ToString().Equals("MetaJson.SerializeAttribute")))
                .ToList();

            foreach (IPropertySymbol serializableProperty in serializableProperties)
            {
                // serializableProperty.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax,

                ImmutableArray<AttributeData> attributes = serializableProperty.GetAttributes();
                if (!attributes.Any(a => a.AttributeClass.ToString().Contains("Serialize")))
                    continue;
                
                SerializableProperty sp = new SerializableProperty()
                {
                    Name = serializableProperty.Name,
                    Symbol = serializableProperty,
                    CanBeNull = !serializableProperty.Type.IsValueType && !attributes.Any(a => a.AttributeClass.ToString().Contains("NotNull")),
                    ArrayItemCanBeNull = !attributes.Any(a => a.AttributeClass.ToString().Contains("ArrayItemNotNull"))
                };

                sc.Properties.Add(sp);
            }

            SerializableClasses.Add(sc);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            VisitClassOrStructDeclaration(node);
            base.VisitStructDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitClassOrStructDeclaration(node);
            base.VisitClassDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccessSyntax
                && memberAccessSyntax.Expression.ToString().Contains("MetaJsonSerializer"))
            {
                // Calling MetaJsonSerializer static methods
                if (memberAccessSyntax.Name is GenericNameSyntax generic && generic.Identifier.ValueText.ToString().Equals("Serialize"))
                {
                    CreateSerializeInvocation(node, generic);
                }
                else if (memberAccessSyntax.Name is GenericNameSyntax generic2 && generic2.Identifier.ValueText.ToString().Equals("Deserialize"))
                {
                    CreateDeserializeInvocation(node, generic2);
                }
            }

            base.VisitInvocationExpression(node);
        }

        private void CreateSerializeInvocation(InvocationExpressionSyntax node, GenericNameSyntax generic)
        {
            if (node.ArgumentList.Arguments.Count == 1 && generic.TypeArgumentList.Arguments.Count == 1)
            {
                TypeSyntax type = generic.TypeArgumentList.Arguments.First();
                SymbolInfo argSymbol = _semanticModel.GetSymbolInfo(type);
                SerializeInvocations.Add(new SerializeInvocation()
                {
                    Invocation = node,
                    TypeArg = argSymbol.Symbol as ITypeSymbol
                });
            }
            else
            {
                // error
            }
        }

        private void CreateDeserializeInvocation(InvocationExpressionSyntax node, GenericNameSyntax generic)
        {
            if (node.ArgumentList.Arguments.Count == 2  && generic.TypeArgumentList.Arguments.Count == 1)
            {
                TypeSyntax type = generic.TypeArgumentList.Arguments.First();
                SymbolInfo argSymbol = _semanticModel.GetSymbolInfo(type);
                DeserializeInvocations.Add(new DeserializeInvocation()
                {
                    Invocation = node,
                    TypeArg = argSymbol.Symbol as ITypeSymbol
                });
            }
            else
            {
                // error
            }
        }
    }
}
