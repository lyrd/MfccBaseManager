using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OperationWithFiles;
using System.IO;

namespace MfccBaseManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private readonly string settingsFilePath = Directory.GetCurrentDirectory() + "\\Settings.ini";
        private string pathToBase = "";

        private void btnStart_Click(object sender, EventArgs e)
        {
            StreamReader streamReader = new StreamReader(pathToBase, Encoding.UTF8);

            string[] text;

            while (true)
            {
                string temp = streamReader.ReadLine();

                if (temp == null) break;

                text = temp.Split(';');

                MessageBox.Show(temp);
                MessageBox.Show(text[0] + "\r\n" + text[1]);
            }

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            using (IniFile ini = new IniFile(settingsFilePath))
            {
                pathToBase = ini.IniReadValue("PathToBase", "Path");
            }
        }
    }
}
