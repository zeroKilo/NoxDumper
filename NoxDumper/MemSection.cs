using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoxDumper
{
    public class MemSection
    {
        public uint start;
        public uint end;
        public string flags;
        public string desc;
        public MemSection(string info)
        {
            start = Convert.ToUInt32(info.Substring(0, 8), 16);
            end = Convert.ToUInt32(info.Substring(9, 8), 16);
            flags = info.Substring(18, 4);
            if (info.Length > 49)
                desc = info.Substring(49);
            else
                desc = "";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}-{1} {2} {3}", start.ToString("X8"), end.ToString("X8"), flags, desc);
            return sb.ToString();
        }
    }
}
