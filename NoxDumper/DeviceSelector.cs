using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace NoxDumper
{
    public partial class DeviceSelector : Form
    {
        public string input;
        public string output = "";

        public DeviceSelector()
        {
            InitializeComponent();
        }

        private void DeviceSelector_Load(object sender, EventArgs e)
        {
            StringReader sr = new StringReader(input);
            List<string> lines = new List<string>();
            string line;
            while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
            foreach (string l in lines)
            {
                string[] tokens = Regex.Split(l.Trim(), "\\s+");
                if (tokens[0] == "TCP" && tokens[1].StartsWith("127.0.0.1:62") && tokens[2] == "0.0.0.0:0")
                {
                    int pid = Convert.ToInt32(tokens[4]);
                    Process p = Process.GetProcessById(pid);
                    if (p.ProcessName.ToLower().Contains("nox"))
                        listBox1.Items.Add(l + "\t" + p.ProcessName);
                }                
            }
            if (listBox1.Items.Count != 0)
                listBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DoSelect();
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            DoSelect();
        }

        private void DoSelect()
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            string[] tokens = Regex.Split(listBox1.SelectedItem.ToString().Trim(), "\\s+");
            output = tokens[1];
            this.Close();
        }
    }
}
