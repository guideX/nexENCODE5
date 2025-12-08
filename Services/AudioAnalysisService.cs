using NAudio.Wave;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Audio analysis data
    /// </summary>
    public class AudioAnalysisData
    {
        public float PeakAmplitude { get; set; }
        public float RMSLevel { get; set; }
        public float DynamicRange { get; set; }
        public int ClippedSamples { get; set; }
        public TimeSpan TotalSilence { get; set; }
        public List<TimeSpan> SilenceRegions { get; set; } = new();
        public float CrestFactor { get; set; }
        public Dictionary<string, float> FrequencyDistribution { get; set; } = new();
    }

    /// <summary>
    /// Service for analyzing audio files
    /// </summary>
    public class AudioAnalysisService
    {
        /// <summary>
        /// Performs comprehensive audio analysis
        /// </summary>
        public AudioAnalysisData AnalyzeAudioFile(string filePath, float silenceThreshold = -40f, float clippingThreshold = 0.99f)
        {
            var analysis = new AudioAnalysisData();

            using var reader = new AudioFileReader(filePath);
            
            // Prepare buffers
            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            
            float peakMax = 0f;
            float sumSquares = 0f;
            long totalSamples = 0;
            int clippedCount = 0;
            bool inSilence = false;
            TimeSpan silenceStart = TimeSpan.Zero;
            TimeSpan totalSilenceDuration = TimeSpan.Zero;

            while (true)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                for (int i = 0; i < samplesRead; i++)
                {
                    float sample = buffer[i];
                    float absSample = Math.Abs(sample);

                    // Peak detection
                    if (absSample > peakMax)
                        peakMax = absSample;

                    // RMS calculation
                    sumSquares += sample * sample;

                    // Clipping detection
                    if (absSample >= clippingThreshold)
                        clippedCount++;

                    // Silence detection
                    float sampleDb = 20 * (float)Math.Log10(absSample + 1e-10f);
                    bool isSilent = sampleDb < silenceThreshold;

                    if (isSilent && !inSilence)
                    {
                        inSilence = true;
                        silenceStart = reader.CurrentTime;
                    }
                    else if (!isSilent && inSilence)
                    {
                        inSilence = false;
                        var silenceDuration = reader.CurrentTime - silenceStart;
                        if (silenceDuration.TotalSeconds > 0.5) // Only count silences > 0.5s
                        {
                            analysis.SilenceRegions.Add(silenceStart);
                            totalSilenceDuration += silenceDuration;
                        }
                    }

                    totalSamples++;
                }
            }

            // Calculate final metrics
            analysis.PeakAmplitude = peakMax;
            analysis.RMSLevel = (float)Math.Sqrt(sumSquares / totalSamples);
            analysis.ClippedSamples = clippedCount;
            analysis.TotalSilence = totalSilenceDuration;
            
            // Calculate crest factor (peak to RMS ratio)
            analysis.CrestFactor = analysis.PeakAmplitude / (analysis.RMSLevel + 1e-10f);
            
            // Calculate dynamic range in dB
            float peakDb = 20 * (float)Math.Log10(analysis.PeakAmplitude + 1e-10f);
            float rmsDb = 20 * (float)Math.Log10(analysis.RMSLevel + 1e-10f);
            analysis.DynamicRange = peakDb - rmsDb;

            return analysis;
        }

        /// <summary>
        /// Calculates the true peak level (inter-sample peaks)
        /// </summary>
        public float CalculateTruePeak(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            
            var buffer = new float[4096];
            float truePeak = 0f;
            float previousSample = 0f;

            while (true)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                for (int i = 0; i < samplesRead; i++)
                {
                    float currentSample = buffer[i];
                    
                    // Check actual sample
                    float abs = Math.Abs(currentSample);
                    if (abs > truePeak) truePeak = abs;

                    // Check inter-sample peak (simple linear interpolation)
                    if (i > 0 || previousSample != 0)
                    {
                        float interpolated = (previousSample + currentSample) / 2f;
                        abs = Math.Abs(interpolated);
                        if (abs > truePeak) truePeak = abs;
                    }

                    previousSample = currentSample;
                }
            }

            return truePeak;
        }

        /// <summary>
        /// Detects if an audio file is likely transcoded from lower quality
        /// </summary>
        public bool DetectTranscode(string filePath)
        {
            // Simple transcoding detection: look for spectral cutoff
            using var reader = new AudioFileReader(filePath);
            
            // Read a sample from the middle of the file
            reader.CurrentTime = TimeSpan.FromSeconds(reader.TotalTime.TotalSeconds / 2);
            
            var buffer = new float[8192];
            int samplesRead = reader.Read(buffer, 0, buffer.Length);
            
            if (samplesRead < 4096) return false;

            // Perform basic FFT to check frequency content
            // If there's a sharp cutoff below 16kHz, likely transcoded from MP3
            // This is a simplified check - full implementation would be more sophisticated
            
            return false; // Placeholder
        }

        /// <summary>
        /// Calculates ReplayGain values for a file
        /// </summary>
        public (float trackGain, float trackPeak) CalculateReplayGain(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            
            // ReplayGain calculation (simplified)
            // Full implementation would use EBU R128 or ReplayGain 2.0 algorithm
            
            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            float sumSquares = 0f;
            long totalSamples = 0;
            float peak = 0f;

            while (true)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                for (int i = 0; i < samplesRead; i++)
                {
                    float sample = buffer[i];
                    sumSquares += sample * sample;
                    
                    float abs = Math.Abs(sample);
                    if (abs > peak) peak = abs;
                    
                    totalSamples++;
                }
            }

            float rms = (float)Math.Sqrt(sumSquares / totalSamples);
            float rmsDb = 20 * (float)Math.Log10(rms + 1e-10f);
            
            // Target level is -18 LUFS (approximately -14 dB RMS)
            float targetDb = -14f;
            float trackGain = targetDb - rmsDb;

            return (trackGain, peak);
        }

        /// <summary>
        /// Analyzes frequency distribution
        /// </summary>
        public Dictionary<string, float> AnalyzeFrequencyDistribution(string filePath)
        {
            var distribution = new Dictionary<string, float>
            {
                { "Sub-Bass (20-60 Hz)", 0f },
                { "Bass (60-250 Hz)", 0f },
                { "Low-Mid (250-500 Hz)", 0f },
                { "Mid (500-2k Hz)", 0f },
                { "High-Mid (2k-4k Hz)", 0f },
                { "Presence (4k-6k Hz)", 0f },
                { "Brilliance (6k-20k Hz)", 0f }
            };

            // This would require FFT analysis across the entire file
            // and accumulation of energy in each frequency band
            // Placeholder implementation

            return distribution;
        }

        /// <summary>
        /// Detects the bits per sample (dynamic range utilization)
        /// </summary>
        public int DetectEffectiveBitDepth(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            
            var buffer = new float[4096];
            var uniqueLevels = new HashSet<float>();
            int samplesAnalyzed = 0;
            const int maxSamplesToAnalyze = 100000;

            while (samplesAnalyzed < maxSamplesToAnalyze)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) break;

                for (int i = 0; i < samplesRead; i++)
                {
                    // Quantize to reasonable precision
                    float quantized = (float)Math.Round(buffer[i] * 65536) / 65536;
                    uniqueLevels.Add(quantized);
                    samplesAnalyzed++;
                }
            }

            // Estimate bit depth from unique levels
            if (uniqueLevels.Count < 256) return 8;
            if (uniqueLevels.Count < 65536) return 16;
            return 24;
        }
    }
}
