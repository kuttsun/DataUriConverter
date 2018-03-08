using System;
using System.Collections.Generic;
using System.Text;

namespace DataUriConverter
{
    public class Base64
    {
        Encoding enc;

        public Base64(string encStr)
        {
            enc = Encoding.GetEncoding(encStr);
        }

        public string Encode(string str)
        {
            return Encode(enc.GetBytes(str));
        }

        public string Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public string Decode(string str)
        {
            return enc.GetString(Convert.FromBase64String(str));
        }
    }
}
