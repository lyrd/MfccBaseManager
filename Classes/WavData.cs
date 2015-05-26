using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleCreator
{

    [Serializable]
    public class WavException : Exception
    {
        public WavException() { }
        public WavException(string message) : base(message) { }
        public WavException(string message, Exception ex) : base(message) { }
        protected WavException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    static class WavData
    {
        private static uint sampleNumber;
        private static double[] nornalizeData;
        private static short[] rawData;

        public static uint SampleNumber
        {
            get { return sampleNumber; }
            set { sampleNumber = value; }
        }

        public static double[] NornalizeData
        {
            get { return nornalizeData; }
            set { nornalizeData = value; }
        }

        public static short[] RawData
        {
            get { return rawData; }
            set { rawData = value; }
        }

        public static void ReadWavDataChunk(string _outputAudioFile)
        {
            using (WaveFileReader reader = new WaveFileReader(_outputAudioFile))
            {
                if (reader.WaveFormat.BitsPerSample == 16)
                {
                    byte[] buffer = new byte[reader.Length];
                    int read = reader.Read(buffer, 0, buffer.Length);
                    short[] sampleBuffer = new short[read / 2];
                    Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);

                    sampleNumber = (uint)sampleBuffer.Length;

                    rawData = sampleBuffer;

                    Normalization(ref rawData);
                }
                else
                {
                    throw new WavException("Only works with 16 bit audio");
                }
            }

        }

        private static void Normalization(ref short[] _rawData)
        {
            double[] _nornalizeData = new double[sampleNumber];

            double minData = (double)Math.Abs((double)_rawData.Min());
            double maxData = (double)Math.Abs((double)_rawData.Max());
            double max = Math.Max(minData, maxData);

            for (uint i = 0; i < sampleNumber; i++)
            {
                _nornalizeData[i] = _rawData[i] / max;
            }

            nornalizeData = _nornalizeData;
        }
    }
}
