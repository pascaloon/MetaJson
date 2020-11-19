using System;
using System.Collections.Generic;
using System.Text;

namespace MetaJson
{
    static class Helpers
    {
        public static string Indent(string origin, int delta)
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
                for (int i = 0; i < -delta; i++)
                {
                    origin = origin.Remove(0, 4);
                }
            }
            return origin;
        }
    }
}
