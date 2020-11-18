using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    abstract class MethodNode { }

    class CSharpNode : MethodNode
    {
        public string CSharpCode { get; set; } = String.Empty;

        public CSharpNode(string cSharpCode)
        {
            CSharpCode = cSharpCode;
        }
    }

    class CSharpLineNode : CSharpNode
    {
        public CSharpLineNode(string cSharpCode)
            : base($"{cSharpCode}\r\n")
        {
        }
    }

    class PlainJsonNode : MethodNode
    {
        // Required when for when nodes are merged
        public string CSharpIndent { get; set; } = String.Empty;

        public string Value { get; set; } = String.Empty;

        public PlainJsonNode(string csharpIndent, string value)
        {
            CSharpIndent = csharpIndent;
            Value = value;
        }
    }
}
