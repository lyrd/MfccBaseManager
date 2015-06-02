using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleCreator
{
    static class Constants
    {
        //Коэффициенты окна Хэмминга
        /// <summary>
        ///  alpha = 0,543478260869565
        /// </summary>
        public static readonly double alpha = 25d / 46d; //or 0.54 or 0.53836
        /// <summary>
        ///  beta = 0,456521739130435
        /// </summary>
        public static readonly double beta = (1d - alpha); //or 0.46 or 0.46164  //public static readonly decimal betta = (1m - alpha) / 2m;

        /// <summary>
        ///Количество MFCC коэффициетов (12)
        /// </summary>
        public static readonly byte mfccSize = 12;//12 10

        ///Диапазон частот
        public static readonly short mfccFreqMin = 300;//300
        public static readonly short mfccFreqMax = 4000;//4000 8000

        public static readonly uint sampleRate = 44100;

        /// <summary>
        /// epsilon = 2,22044604925031E-16
        /// </summary>
        public static double Epsilon()
        {
            double epsilon = 1d;
            double tmp = 1d;
            while ((1d + (tmp /= 2d)) != 1d) epsilon = tmp;
            return epsilon;
        }
    }
}
