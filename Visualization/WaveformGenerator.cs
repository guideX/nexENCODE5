using NAudio.Wave;

namespace nexENCODE_Studio.Visualization
{
    /// <summary>
    /// Generates waveform data from audio files for static display
    /// </summary>
    public class WaveformGenerator
    {
        /// <summary>
        /// Generates waveform data for the entire audio file
        /// </summary>
        public static WaveformData GenerateWaveform(string audioFilePath, int width, int samplesPerPixel = 128)
        {
            using var reader = new AudioFileReader(audioFilePath);
            
            var waveformData = new WaveformData
            {
                Width = width,
                SamplesPerPixel = samplesPerPixel,
                StartTime = TimeSpan.Zero,
                EndTime = reader.TotalTime
            };

            // Calculate total samples needed
            long totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
            int pointsNeeded = width;
            int samplesPerPoint = (int)(totalSamples / reader.WaveFormat.Channels / pointsNeeded);

            if (samplesPerPoint < samplesPerPixel)
                samplesPerPoint = samplesPerPixel;

            waveformData.SamplesPerPixel = samplesPerPoint;
            waveformData.WaveformPoints = new float[pointsNeeded];

            // Read and process audio
            var buffer = new float[samplesPerPoint * reader.WaveFormat.Channels];
            int pointIndex = 0;

            while (pointIndex < pointsNeeded)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                // Calculate peak for this segment
                float max = 0f;
                for (int i = 0; i < samplesRead; i++)
                {
                    float abs = Math.Abs(buffer[i]);
                    if (abs > max) max = abs;
                }

                waveformData.WaveformPoints[pointIndex] = max;
                pointIndex++;
            }

            return waveformData;
        }

        /// <summary>
        /// Generates waveform for a specific time range
        /// </summary>
        public static WaveformData GenerateWaveformRange(string audioFilePath, TimeSpan startTime, TimeSpan endTime, int width)
        {
            using var reader = new AudioFileReader(audioFilePath);
            
            // Seek to start position
            long startSample = (long)(startTime.TotalSeconds * reader.WaveFormat.SampleRate);
            reader.Position = startSample * reader.WaveFormat.Channels * (reader.WaveFormat.BitsPerSample / 8);

            var waveformData = new WaveformData
            {
                Width = width,
                StartTime = startTime,
                EndTime = endTime
            };

            // Calculate samples per point
            var duration = endTime - startTime;
            var totalSamples = (int)(duration.TotalSeconds * reader.WaveFormat.SampleRate);
            int samplesPerPoint = totalSamples / width;

            waveformData.SamplesPerPixel = samplesPerPoint;
            waveformData.WaveformPoints = new float[width];

            // Read and process
            var buffer = new float[samplesPerPoint * reader.WaveFormat.Channels];
            
            for (int i = 0; i < width; i++)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                float max = 0f;
                for (int j = 0; j < samplesRead; j++)
                {
                    float abs = Math.Abs(buffer[j]);
                    if (abs > max) max = abs;
                }

                waveformData.WaveformPoints[i] = max;
            }

            return waveformData;
        }

        /// <summary>
        /// Generates detailed waveform with separate channels
        /// </summary>
        public static (float[] left, float[] right) GenerateStereoWaveform(string audioFilePath, int width)
        {
            using var reader = new AudioFileReader(audioFilePath);

            if (reader.WaveFormat.Channels != 2)
                throw new InvalidOperationException("Audio file must be stereo");

            long totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
            int samplesPerPoint = (int)(totalSamples / reader.WaveFormat.Channels / width);

            var leftChannel = new float[width];
            var rightChannel = new float[width];

            var buffer = new float[samplesPerPoint * 2];
            int pointIndex = 0;

            while (pointIndex < width)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                float leftMax = 0f;
                float rightMax = 0f;

                for (int i = 0; i < samplesRead; i += 2)
                {
                    float leftSample = Math.Abs(buffer[i]);
                    float rightSample = Math.Abs(buffer[i + 1]);

                    if (leftSample > leftMax) leftMax = leftSample;
                    if (rightSample > rightMax) rightMax = rightSample;
                }

                leftChannel[pointIndex] = leftMax;
                rightChannel[pointIndex] = rightMax;
                pointIndex++;
            }

            return (leftChannel, rightChannel);
        }
    }
}
