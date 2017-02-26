using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace NoxDumper
{
    public partial class Form1 : Form
    {
        public List<ProcInfo> procs = new List<ProcInfo>();
        public List<MemSection> sections = new List<MemSection>();
        public string path = Path.GetDirectoryName(Application.ExecutablePath).Replace("\\\\","\\") + "\\";

        public Form1()
        {
            InitializeComponent();            
        }

        public void RefreshProcesses()
        {
            listBox1.Items.Clear();
            foreach (ProcInfo info in procs)
                listBox1.Items.Add(info.ToString());
        }

        public void RefreshSection()
        {
            listBox2.Items.Clear();
            foreach (MemSection section in sections)
                listBox2.Items.Add(section.ToString());
        }

        public string RunNoxShell(string command)
        {
            return RunShell("nox_adb.exe", "shell " + command);
        }

        public string RunShell(string command, string args)
        {
            Process process = new Process();
            StringBuilder outputStringBuilder = new StringBuilder();
            try
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = args;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.EnableRaisingEvents = false;
                process.OutputDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                process.ErrorDataReceived += (sender, eventArgs) => outputStringBuilder.AppendLine(eventArgs.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var processExited = process.WaitForExit(10000);
                var output = outputStringBuilder.ToString();
                if (processExited == false) 
                    process.Kill();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                process.Close();
            }
            string result = outputStringBuilder.ToString();
            log.AppendText(">" + command + " " + args + "\r\n");
            log.AppendText(result.Replace("\r\n\r\n","\r\n"));
            int maxsize = 10000;
            if (log.Text.Length > maxsize)
                log.Text = log.Text.Substring(log.Text.Length - maxsize, maxsize);
            return result;
        }

        private void refreshProcessListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            procs = new List<ProcInfo>();
            string[] commands = new string[] { "ps -t", " ps" };
            foreach (string command in commands)
            {
                string result = RunNoxShell(command);
                int len = result.Length;
                while (true)
                {
                    result = result.Replace("  ", " ");
                    if (result.Length != len)
                        len = result.Length;
                    else
                        break;
                }
                StringReader sr = new StringReader(result);
                string line;
                while ((line = sr.ReadLine()) != null)
                    if (!line.StartsWith("USER") && line.Trim() != "")
                        procs.Add(new ProcInfo(line));
            }
            while (true)
            {
                bool found = false;
                for (int i = 0; i < procs.Count - 1; i++)
                {
                    if (procs[i + 1].pid < procs[i].pid)
                    {
                        found = true;
                        ProcInfo tmp = procs[i];
                        procs[i] = procs[i + 1];
                        procs[i + 1] = tmp;
                    }
                    if (procs[i + 1].pid == procs[i].pid)
                    {
                        procs.RemoveAt(i);
                        found = true;
                    }
                }
                if (!found)
                    break;
            }
            RefreshProcesses();
        }

        private void getMemoryMapOfProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            ProcInfo info = procs[n];
            string result = RunNoxShell("cat /proc/" + info.pid + "/maps");
            sections = new List<MemSection>();
            StringReader sr = new StringReader(result);
            string line;
            sections = new List<MemSection>();
            while ((line = sr.ReadLine()) != null)
                if (line.Trim() != "")
                    sections.Add(new MemSection(line));
            RefreshSection();
        }

        private void dumpSectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            int m = listBox2.SelectedIndex;
            if (n == -1 || m == -1)
                return;
            if (File.Exists("dump.bin"))
                File.Delete("dump.bin");
            ProcInfo info = procs[n];
            MemSection section = sections[m];
            uint size = section.end - section.start;
            RunNoxShell("mkdir /data/data/com.wv.noxdumper/");
            RunNoxShell("dd if=/proc/" + info.pid + "/mem of=/data/data/com.wv.noxdumper/dump bs=1 count=" + size + " skip=" + section.start);
            RunShell("nox_adb.exe", "pull /data/data/com.wv.noxdumper/dump \"" + path + "dump.bin\"");
            if (File.Exists("dump.bin"))
            {
                hb1.ByteProvider = new DynamicByteProvider(File.ReadAllBytes("dump.bin"));
                File.Delete("dump.bin");
            }
            else
                hb1.ByteProvider = new DynamicByteProvider(new byte[0]);
        }

        private void startNOXADBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(RunShell("nox_adb.exe", "connect 127.0.0.1:62001"));
        }

        private void saveDumpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hb1.ByteProvider == null || hb1.ByteProvider.Length == 0)
                return;
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.*|*.*";
            d.FileName = "";
            int n = listBox2.SelectedIndex;
            if (n != -1)
                d.FileName = sections[n].desc;
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MemoryStream m = new MemoryStream();
                for (long i = 0; i < hb1.ByteProvider.Length; i++)
                    m.WriteByte(hb1.ByteProvider.ReadByte(i));
                File.WriteAllBytes(d.FileName, m.ToArray());
                MessageBox.Show("Done.");
            }
        }
    }
}
