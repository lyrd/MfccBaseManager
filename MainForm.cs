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
using SubtitleCreator;
using System.Diagnostics;

namespace MfccBaseManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private string pathToBase = "mfccBase.mfcc";
        private string pathToTempBase = "BaseTemp.mfcc";
        private Dictionary<string, double[]> samplesMFCC = new Dictionary<string, double[]>();
        private string word = "";
        private double[] mfcc;
        private string[] audioFiles;
        double[] rawdata;

        ErrorProvider errorProvider = new ErrorProvider();

        Stopwatch stopwatch = new Stopwatch();

        delegate void SetCallBackrogressBarSpeed(int speed);

        public void setProgressBarSpeed(int speed)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetCallBackrogressBarSpeed d = new SetCallBackrogressBarSpeed(setProgressBarSpeed);
                this.Invoke(d, speed);
            }
            else
            {
                this.progressBar1.MarqueeAnimationSpeed = 1;
            }
        }

        private string Transliteration(string source)
        {
            Dictionary<string, string> letters = new Dictionary<string, string>();

            string[,] setOfLetters ={ { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я", "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" },
                                    { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "j", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "h", "c", "ch", "sh", "sch", "j", "i", "j", "e", "yu", "ya", "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "J", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "H", "C", "Ch", "Sh", "Sch", "J", "I", "J", "E", "Yu", "Ya" }};

            for (byte i = 0; i < setOfLetters.GetLength(1); i++)
                letters.Add(setOfLetters[0, i], setOfLetters[1, i]);

            foreach (KeyValuePair<string, string> pair in letters)
            {
                source = source.Replace(pair.Key, pair.Value);
            }

            return source;
        }

        private List<string> GetWords(string pathToTempBase)
        {
            List<string> listOfWords = new List<string>();

            using (StreamReader streamReader = new StreamReader(pathToTempBase, Encoding.UTF8))
            {
                while (true)
                {
                    string[] line = new string[2];
                    string[] array = new string[Constants.mfccSize];
                    double[] mfccs = new double[Constants.mfccSize];

                    string temp = streamReader.ReadLine();

                    if (temp == null) break;

                    line = temp.Split(';');
                    array = line[1].Split('/');

                    if (listOfWords.Contains(line[0]))
                    {
                        continue;
                    }
                    else
                    {
                        listOfWords.Add(line[0]);
                    }
                }
            }

            return listOfWords;
        }

        private void Average(List<string> words, string pathToBase, string pathToTempBase)
        {
            List<double[]> mfccsList = new List<double[]>();
            double[] resultArray = new double[Constants.mfccSize];

            foreach (string word in words)
            {
                mfccsList.Clear();
                Array.Clear(resultArray, 0, resultArray.Length);

                using (StreamReader streamReader = new StreamReader(pathToTempBase, Encoding.UTF8))
                {
                    while (true)
                    {
                        string[] line = new string[2];
                        string[] array = new string[Constants.mfccSize];
                        double[] mfccs = new double[Constants.mfccSize];

                        string temp = streamReader.ReadLine();

                        if (temp == null) break;

                        if (temp.Contains(word + ";"))
                        {
                            line = temp.Split(';');
                            array = line[1].Split('/');

                            for (int i = 0; i < array.Length; i++)
                                Double.TryParse(array[i], out mfccs[i]);

                            mfccsList.Add(mfccs);
                        }
                    }
                }

                for (int i = 0; i < Constants.mfccSize; i++)
                {
                    double sum = 0;

                    foreach (double[] list in mfccsList)
                    {
                        sum += list[i];
                    }

                    resultArray[i] = sum / mfccsList.Count;
                }

                using (StreamWriter streamwriter = new StreamWriter(pathToBase, true, Encoding.UTF8))
                {
                    streamwriter.WriteLine(String.Format("{0};{1}", word, DoubleToString(resultArray)));
                }
            }
        }

        private double[] StringToDouble(string str)
        {
            double[] array = new double[Constants.mfccSize];
            string[] part = new string[2];
            string[] arrayStr = new string[Constants.mfccSize];

            part = str.Split(';');
            arrayStr = part[1].Split('/');

            for (int i = 0; i < array.Length; i++)
                Double.TryParse(arrayStr[i], out array[i]);

            return array;
        }

        private string DoubleToString(double[] array)
        {
            string str = "";
            for (int i = 0; i < array.Length; i++)
                str += array[i] + "/";
            str = str.Substring(0, str.Length - 1);
            return str;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            errorProvider.Clear();
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Wav Files (*.wav)|*.wav";
            audioFiles = dialog.ShowDialog() == DialogResult.OK ? dialog.FileNames : new string[] { "err" };

            foreach (string path in audioFiles)
                tBPath.Text += path + "\r\n";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (tBPath.Text != "" & tBWord.Text != "")
            {
                word = tBWord.Text;
                progressBar1.Style = ProgressBarStyle.Marquee;
                stopwatch.Start();
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                if (tBPath.Text == "") { errorProvider.SetError(tBPath, "Обязательно для заполнения"); }
                if (tBWord.Text == "") { errorProvider.SetError(tBWord, "Обязательно для заполнения"); }
                if (tBPath.Text == "err") { errorProvider.SetError(tBPath, "Путь не выбран"); }
            }
        }

        private void averageTSMI_Click(object sender, EventArgs e)
        {
            Average(GetWords(pathToTempBase), pathToBase, pathToTempBase);
        }

        private void tBWord_MouseClick(object sender, MouseEventArgs e)
        {
            tBPath.Text = "";
            tBWord.Text = "";
            stopwatch.Reset();
            samplesMFCC.Clear();
            word = "";
            errorProvider.Clear();
            toolStripStatusLabel1.Text = "";

            progressBar1.Value = 0;

            try
            {
                Array.Clear(mfcc, 0, mfcc.Length);
                Array.Clear(rawdata, 0, rawdata.Length);
            }
            catch { }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            for (int j = 0; j < audioFiles.Length; j++)
            {
                try
                {
                    WavData.ReadWavDataChunk(audioFiles[j]);
                }
                catch (WavException ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                setProgressBarSpeed(1);

                rawdata = WavData.NornalizeData;

                mfcc = MFCC.Transform(ref rawdata, 0, WavData.SampleNumber, Constants.mfccSize, Constants.sampleRate, Constants.mfccFreqMin, Constants.mfccFreqMax);

                string mfccString = "";
                for (int i = 0; i < mfcc.Length; i++)
                    mfccString += mfcc[i] + "/";

                mfccString = mfccString.Substring(0, mfccString.Length - 1);

                using (StreamWriter streamwriter = new StreamWriter(pathToTempBase, true, Encoding.UTF8))
                {
                    streamwriter.WriteLine(String.Format("{0};{1}", word, mfccString));
                }
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Cursor = Cursors.Default;
            TimeSpan interval = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            toolStripStatusLabel1.Text = String.Format("Затрачено времени: {0}:{1}:{2}.{3}", interval.Hours, interval.Minutes, interval.Seconds, interval.Milliseconds);
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.Value = 100;
        }
    }
}
