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
            : base($"$t{cSharpCode}\r\n")
        {
        }
    }

    class CsharpTabControlNode : MethodNode
    {
        public int Delta { get; set; } = 0;

        public CsharpTabControlNode(int delta)
        {
            Delta = delta;
        }
    }

    class JsonNode : MethodNode
    {
        public string Value { get; set; } = String.Empty;

        public JsonNode(string value)
        {
            Value = value;
        }
    }

    class JsonTabControlNode : MethodNode
    {
        public int Delta { get; set; } = 0;

        public JsonTabControlNode(int delta)
        {
            Delta = delta;
        }
    }
}
