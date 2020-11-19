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
            sb.Append($@"{SPC}{SPC}public static {invocationTypeStr} Deserialize<T>(string json) where T: {invocationTypeStr}");
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
            TreeContext treeContext = new TreeContext();
            treeContext.IndentCSharp(+3);
            string ct = treeContext.CSharpIndent;

            List<MethodNode> nodes = new List<MethodNode>();
            string invocationTypeStr = invocation.TypeArg.ToString();
            nodes.Add(new CSharpLineNode($"{ct}return new {invocationTypeStr}();"));

            foreach (CSharpNode node in nodes.OfType<CSharpNode>())
            {
                sb.Append(node.CSharpCode);
            }

        }

    }
}
