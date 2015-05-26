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

namespace MfccBaseManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private string pathToBase = "mfccBase.mfcc";
        private Dictionary<string, double[]> samplesMFCC = new Dictionary<string, double[]>();
        private string word = "";
        private double[] mfcc;
        private string audioFile = "";

        ErrorProvider errorProvider = new ErrorProvider();

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

        private void btnOpen_Click(object sender, EventArgs e)
        {
            word = tBWord.Text;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Wav Files (*.wav)|*.wav";
            audioFile = tBPath.Text = dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : "err";           
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (tBPath.Text != "" & tBWord.Text != "")
            {
                try
                {
                    WavData.ReadWavDataChunk(audioFile);
                }
                catch (WavException ex)
                {
                    MessageBox.Show(String.Format(ex.Message), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                double[] rawdata = WavData.NornalizeData;
                mfcc = MFCC.Transform(ref rawdata, 0, WavData.SampleNumber, Constants.mfccSize, Constants.sampleRate, Constants.mfccFreqMin, Constants.mfccFreqMax);

                string mfccString = "";
                for (int i = 0; i < mfcc.Length; i++)
                    mfccString += mfcc[i] + "/";
                mfccString = mfccString.Substring(0, mfccString.Length - 1);

                using (StreamWriter streamwriter = new StreamWriter(pathToBase, true, Encoding.UTF8))
                {
                    streamwriter.WriteLine(String.Format("{0};{1}", word, mfccString));
                }
            }
            else
            {
                if (tBPath.Text == "") { errorProvider.SetError(tBPath, "Обязательно для заполнения"); }
                if (tBWord.Text == "") { errorProvider.SetError(tBWord, "Обязательно для заполнения"); }
                if (tBPath.Text == "err") { errorProvider.SetError(tBPath, "Путь не выбран"); }
            }
        }

        private void ReadFromDataBase(ref Dictionary<string, double[]> samplesMFCC, string pathToBase)
        {
            using (StreamReader streamReader = new StreamReader(pathToBase, Encoding.UTF8))
            {
                while (true)
                {
                    string[] line = new string[2];
                    string[] array = new string[12];
                    double[] mfccs = new double[12];

                    string temp = streamReader.ReadLine();

                    if (temp == null) break;

                    line = temp.Split(';');
                    array = line[1].Split('/');

                    for (int i = 0; i < array.Length; i++)
                        Double.TryParse(array[i], out mfccs[i]);

                    samplesMFCC.Add(line[0], mfccs);
                }
            }          
        }


    }
}
