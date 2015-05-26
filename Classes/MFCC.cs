using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.Serialization;

//using size_t = System.UInt64;
using size_t = System.UInt32;

namespace SubtitleCreator
{
    [Serializable]
    public class MFCCException : Exception
    {
        public MFCCException() { }
        public MFCCException(string message) : base(message) { }
        public MFCCException(string message, Exception ex) : base(message) { }
        protected MFCCException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    static class MFCC
    {

        //Выполнение преобразования MFCC
        public static double[] Transform(ref double[] source, uint start, uint finish, byte mfccSize, uint frequency, short freqMin, short freqMax)//short frequency
        {
            uint sampleLength = finish - start + 1;
            //double p2length = Math.Pow(2, Math.Floor(Math.Log(sampleLength, 2)));
            uint p2length = (uint)Math.Pow(2, Math.Floor(Math.Log(sampleLength, 2)));

            double[] temp = new double[p2length];
            Array.Copy(source, start, temp, 0, p2length);

            double[] fourierRaw = FourierTransformFast(ref temp, p2length, true);
            double[,] melFilters = GetMelFilters(mfccSize, p2length, frequency, freqMin, freqMax);

            double[] logPower = CalcPower(ref fourierRaw, p2length, melFilters, mfccSize);
            double[] dctRaw = DctTransform(ref logPower, mfccSize);
    
            return dctRaw;            
        }

        private static double[] FourierTransform(double[] source, uint length, bool useWindow)
        {
            Complex[] fourierCmplxRaw = new Complex[length];
            double[] fourierRaw = new double[length];

            for (uint k = 0; k < length; k++)
            {
                fourierCmplxRaw[k] = new Complex(0d, 0d);

                for (uint n = 0; n < length; n++)
                {
                    double sample = source[n];

                    //По формуле Эйлера: e^(ix) = cos(x) + i*sin(x)
                    double x = -2 * Math.PI * k * n / (double)length;

                    Complex f = sample * new Complex(Math.Cos(x), Math.Sin(x));

                    double w = 1;
                    if (useWindow)
                    {
                        //Окно Хэмминга
                        w = Constants.alpha - Constants.beta * Math.Cos(2 * Math.PI * n / (length - 1));
                    }

                    fourierCmplxRaw[k] += f * w;
                }

                //Магнитуда
                fourierRaw[k] = fourierCmplxRaw[k].Magnitude;
            };

            return fourierRaw;
        }

        private static double[] FourierTransformFast(ref double[] source, uint length, bool useWindow)
        {
            //Расширить длину исходных данных до степени двойки
            uint p2length = length;

            double[] fourierRaw = new double[length];
            Complex[] fourierRawTmp = new Complex[length];

            for (uint i = 0; i < p2length; i++)
            {
                //Каждый элемент - вещественная часть комплексного числа
                if (i < length)
                {
                    fourierRawTmp[i] = new Complex(source[i], 0d);

                    if (useWindow)
                    {
                        fourierRawTmp[i] *= (Constants.alpha - Constants.beta * Math.Cos(2 * Math.PI * i / (length - 1)));
                    }

                }
                else
                {
                    fourierRawTmp[i] = new Complex(0d, 0d);
                }
            }

            //Рекурсивные вычисления
            FourierTransformFastRecursion(ref fourierRawTmp);

            //Магнитуда
            for (uint i = 0; i < length; i++)
            {
                fourierRaw[i] = fourierRawTmp[i].Magnitude;
            }

            return fourierRaw;
        }

        private static void FourierTransformFastRecursion(ref Complex[] data)
        {

            //Выход из рекурсии
            size_t n = (size_t)data.Count();
            if (n <= 1)
            {
                return;
            }

            //Разделение
            Complex[] even = data.AsParallel().Where((s, index) => { return index % 2 == 0; }).ToArray();
            Complex[] odd = data.AsParallel().Where((s, index) => { return index % 2 != 0; }).ToArray();

            FourierTransformFastRecursion(ref even);
            FourierTransformFastRecursion(ref odd);

            //Объединение 
            for (size_t i = 0; i < n / 2; i++)
            {
                Complex t = Complex.FromPolarCoordinates(1.0, -2 * Math.PI * i / n) * odd[i];
                data[i] = even[i] + t;
                data[i + n / 2] = even[i] - t;
            }
        }

        private static double[,] GetMelFilters(byte mfccSize, uint filterLength, uint frequency, short freqMin, short freqMax)//short frequency
        {
            double[] fb = new double[mfccSize + 2];
            fb[0] = FrequencyToMel(freqMin);
            fb[mfccSize + 1] = FrequencyToMel(freqMax);

            for (byte m = 1; m < mfccSize + 1; m++)
            {
                fb[m] = fb[0] + m * (fb[mfccSize + 1] - fb[0]) / (mfccSize + 1);
            }

            for (byte m = 0; m < mfccSize + 2; m++)
            {
                fb[m] = MelToFrequency(fb[m]);

                fb[m] = Math.Floor((filterLength + 1) * fb[m] / (double)frequency);

                //TODO: "FT bin too small" if!(m > 0 && (fb[m] - fb[m-1]) < epsilon);
            }

            double[,] filterBanks = new double[mfccSize, filterLength];

            for (byte m = 1; m < mfccSize + 1; m++)
            {
                for (uint k = 0; k < filterLength; k++)
                {

                    if (fb[m - 1] <= k && k <= fb[m])
                    {
                        filterBanks[m - 1, k] = (k - fb[m - 1]) / (fb[m] - fb[m - 1]);
                    }
                    else if (fb[m] < k && k <= fb[m + 1])// (<= <=) (< <=)
                    {
                        filterBanks[m - 1, k] = (fb[m + 1] - k) / (fb[m + 1] - fb[m]);
                    }
                    else
                    {
                        filterBanks[m - 1, k] = 0;
                    }
                }
            }

            return filterBanks;
        }

        //Вычисление энергии фрейма
        private static double[] CalcPower(ref double[] fourierRaw, uint fourierLength, double[,] melFilters, byte mfccCount)
        {

            double[] logPower = new double[mfccCount];

            for (ushort m = 0; m < mfccCount; m++)
            {
                logPower[m] = 0d;

                for (uint k = 0; k < fourierLength; k++)
                {
                    logPower[m] += melFilters[m, k] * Math.Pow(fourierRaw[k], 2);
                }

                //TODO: "Spectrum power is less than zero" if !(logPower[m] < epsilon());

                logPower[m] = Math.Log(logPower[m]);
            }

            return logPower;
        }

        //Дискретное косинусное преобразование
        private static double[] DctTransform(ref double[] data, uint length)
        {

            double[] dctTransform = new double[length];

            for (ushort n = 0; n < length; n++)
            {
                dctTransform[n] = 0;

                for (ushort m = 0; m < length; m++)
                {
                    dctTransform[n] += data[m] * Math.Cos(Math.PI * n * (m + 1d / 2d) / length);
                }
            }

            return dctTransform;
        }

        private static double FrequencyToMel(double f) { return 1125d * Math.Log(1d + f / 700d); }
        private static double MelToFrequency(double m) { return 700d * (Math.Exp(m / 1125d) - 1); }
    }
}
