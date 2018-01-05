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
        public string path = Path.GetDirectoryName(Application.ExecutablePath)+ "\\";
        public string proc_filter = "";
        public string sect_filter = "";

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
                if (section.ToString().Contains(sect_filter))
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
            GetProcessList();
        }

        private void getMemoryMapOfProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetMemoryMap();
        }

        public void GetMemoryMap()
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
                if (line.Trim() != "" && line.Contains(sect_filter))
                    sections.Add(new MemSection(line));
            RefreshSection();
        }

        public void GetProcessList()
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
                    if (!line.StartsWith("USER") && line.Trim() != "" && line.Contains(proc_filter))
                    {
                        try
                        {
                            procs.Add(new ProcInfo(line));
                        }
                        catch (Exception ex)
                        {
                            File.WriteAllText("error.log", line + "\n\n" + ex.Message);
                        }
                    }
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

        private void dumpSectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            int m = listBox2.SelectedIndex;
            if (n == -1 || m == -1)
                return;
            if (File.Exists("dump.bin"))
                File.Delete("dump.bin");
            DumpSection(n, m);
            if (File.Exists("dump.bin"))
            {
                hb1.ByteProvider = new DynamicByteProvider(File.ReadAllBytes("dump.bin"));
                File.Delete("dump.bin");
            }
            else
                hb1.ByteProvider = new DynamicByteProvider(new byte[0]);
        }

        private void DumpSection(int p, int s)
        {
            ProcInfo info = procs[p];
            MemSection section = sections[s];
            uint size = section.end - section.start;
            RunNoxShell("mkdir /data/data/com.wv.noxdumper/");
            RunNoxShell("dd if=/proc/" + info.pid + "/mem of=/data/data/com.wv.noxdumper/dump bs=1 count=" + size + " skip=" + section.start);
            RunShell("nox_adb.exe", "pull /data/data/com.wv.noxdumper/dump \"" + path + "dump.bin\"");
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            proc_filter = textBox1.Text;
            GetProcessList();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            sect_filter = textBox2.Text;
            GetMemoryMap();
        }

        public string CleanName(string name)
        {
            return name.Replace("\\", "_")
                       .Replace("/", "_")
                       .Replace("<", "_")
                       .Replace(">", "_")
                       .Replace("?", "_")
                       .Replace("@", "_")
                       .Replace(":", "_")
                       .Replace("*", "_")
                       .Replace("\"", "_")
                       .Replace(":", "_")
                       .Replace("|", "_");
        }

        private void dumpAllSectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1) return;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pb1.Maximum = sections.Count;
                actionToolStripMenuItem.Enabled = false;
                for (int i = 0; i < sections.Count; i++)
                {
                    string name = "sec" + i.ToString("D4") + "_" + CleanName(sections[i].desc);
                    try
                    {
                        if (File.Exists("dump.bin"))
                            File.Delete("dump.bin");
                        DumpSection(n, i);
                        if (File.Exists("dump.bin"))
                            File.Move("dump.bin", fbd.SelectedPath + "\\" + name);
                        pb1.Value = i;
                        Application.DoEvents();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error for section #" + i + " '" + name + "'!\n" + ex.Message);
                    }
                }
                actionToolStripMenuItem.Enabled = true;
                pb1.Value = 0;
            }
        }
    }
}
