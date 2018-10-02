using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DegreeQuest
{
    public static class Util
    {
        public static byte[] stb(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string bts(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }

    }
}
