using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoxDumper
{
    public class ProcInfo
    {
        public string user;
        public uint pid;
        public uint ppid;
        public uint vsize;
        public uint rss;
        public uint wchan;
        public uint pc;
        public string flag;
        public string name;
        public ProcInfo(string info)
        {
            string[] parts = info.Split(' ');
            user = parts[0];
            pid = Convert.ToUInt32(parts[1]);
            ppid = Convert.ToUInt32(parts[2]);
            vsize = Convert.ToUInt32(parts[3]);
            rss = Convert.ToUInt32(parts[4]);
            wchan = Convert.ToUInt32(parts[5], 16);
            pc = Convert.ToUInt32(parts[6], 16);
            flag = parts[7];
            name = parts[8];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} - {1} : {2} {3}", pid, ppid, pc.ToString("X8"), name);
            sb.AppendLine(user);
            return sb.ToString();
        }
    }
}
