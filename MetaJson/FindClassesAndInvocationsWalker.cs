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

            ImmutableArray<ISymbol> members = type.GetMembers();


            foreach (ISymbol serializableProperty in type.GetMembers())
            {
                // serializableProperty.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax,

                ITypeSymbol typeSymbol = null;
                if (serializableProperty is IPropertySymbol ps)
                {
                    typeSymbol = ps.Type;
                }
                else if (serializableProperty is IFieldSymbol fs)
                {
                    typeSymbol = fs.Type;
                }
                else
                {
                    continue;
                }

                ImmutableArray<AttributeData> attributes = serializableProperty.GetAttributes();
                if (!attributes.Any(a => a.AttributeClass.ToString().Contains("Serialize")))
                    continue;
                
                SerializableProperty sp = new SerializableProperty()
                {
                    Name = serializableProperty.Name,
                    Type = typeSymbol,
                    CanBeNull = !typeSymbol.IsValueType && !attributes.Any(a => a.AttributeClass.ToString().Contains("NotNull")),
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
                if (memberAccessSyntax.Name is IdentifierNameSyntax id && id.Identifier.ValueText.ToString().Equals("Serialize"))
                {
                    CreateSerializeInvocation(node);
                }
                else if (memberAccessSyntax.Name is IdentifierNameSyntax id2 && id2.Identifier.ValueText.ToString().Equals("Deserialize"))
                {
                    CreateDeserializeInvocation(node);
                }
            }

            base.VisitInvocationExpression(node);
        }

        private void CreateSerializeInvocation(InvocationExpressionSyntax node)
        {
            if (node.ArgumentList.Arguments.Count == 1)
            {
                ArgumentSyntax arg = node.ArgumentList.Arguments.First();
                if (arg.Expression is IdentifierNameSyntax ins)
                {
                    ITypeSymbol argType = null;
                    ISymbol argSymbol = _semanticModel.GetSymbolInfo(ins).Symbol;
                    if (argSymbol is IFieldSymbol ifs)
                    {
                        argType = ifs.Type;
                    } 
                    else if (argSymbol is ILocalSymbol ils)
                    {
                        argType = ils.Type;
                    }
                    else if (argSymbol is IParameterSymbol ips)
                    {
                        argType = ips.Type;
                    }

                    if (argType != null)
                    {
                        SerializeInvocations.Add(new SerializeInvocation()
                        {
                            Invocation = node,
                            TypeArg = argType
                        });
                    }
                    else
                    {
                        // error
                    }

                }
                else
                {
                    // error
                }

            }
            else
            {
                // error
            }
        }

        private void CreateDeserializeInvocation(InvocationExpressionSyntax node)
        {
            if (node.ArgumentList.Arguments.Count == 2)
            {
                ArgumentSyntax secondArg = node.ArgumentList.Arguments[1];
                if (secondArg.RefKindKeyword.ValueText == "out")
                {
                    if (secondArg.Expression is IdentifierNameSyntax ins)
                    {
                        ITypeSymbol argType = null;
                        ISymbol argSymbol = _semanticModel.GetSymbolInfo(ins).Symbol;
                        if (argSymbol is IFieldSymbol ifs)
                        {
                            argType = ifs.Type;
                        }
                        else if (argSymbol is ILocalSymbol ils)
                        {
                            argType = ils.Type;
                        }
                        else if (argSymbol is IParameterSymbol ips)
                        {
                            argType = ips.Type;
                        }

                        if (argType != null)
                        {
                            DeserializeInvocations.Add(new DeserializeInvocation()
                            {
                                Invocation = node,
                                TypeArg = argType
                            });
                        }
                        else
                        {
                            // error
                        }
                    }
                    else if (secondArg.Expression is DeclarationExpressionSyntax des)
                    {
                        ITypeSymbol argType = _semanticModel.GetSymbolInfo(des.Type).Symbol as ITypeSymbol;
                        if (argType != null)
                        {
                            DeserializeInvocations.Add(new DeserializeInvocation()
                            {
                                Invocation = node,
                                TypeArg = argType
                            });
                        }
                        else
                        {
                            // error
                        }
                    }
                    else
                    {
                        // error
                    }
                }
                else
                {
                    // error
                }
            }
            else
            {
                // error
            }
        }
    }
}
