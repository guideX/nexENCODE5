using NAudio.Wave;

namespace nexENCODE_Studio.Visualization
{
    /// <summary>
    /// Audio sample data for visualization
    /// </summary>
    public class AudioSampleData
    {
        public float[] LeftChannel { get; set; } = Array.Empty<float>();
        public float[] RightChannel { get; set; } = Array.Empty<float>();
        public int SampleRate { get; set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Spectrum analysis data (FFT results)
    /// </summary>
    public class SpectrumData
    {
        public float[] FrequencySpectrum { get; set; } = Array.Empty<float>();
        public float[] LeftSpectrum { get; set; } = Array.Empty<float>();
        public float[] RightSpectrum { get; set; } = Array.Empty<float>();
        public int FftSize { get; set; }
        public float MaxFrequency { get; set; }
        public float[] FrequencyLabels { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Peak level data for VU meters
    /// </summary>
    public class PeakLevelData
    {
        public float LeftPeak { get; set; }
        public float RightPeak { get; set; }
        public float LeftRMS { get; set; }
        public float RightRMS { get; set; }
        public bool LeftClipping { get; set; }
        public bool RightClipping { get; set; }
        public float LeftDecayingPeak { get; set; }
        public float RightDecayingPeak { get; set; }
    }

    /// <summary>
    /// Waveform data for display
    /// </summary>
    public class WaveformData
    {
        public float[] WaveformPoints { get; set; } = Array.Empty<float>();
        public int SamplesPerPixel { get; set; }
        public int Width { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    /// <summary>
    /// Visualization configuration options
    /// </summary>
    public class VisualizationOptions
    {
        public int FftSize { get; set; } = 4096;
        public int SpectrumBands { get; set; } = 128;
        public float MinFrequency { get; set; } = 20f;
        public float MaxFrequency { get; set; } = 20000f;
        public bool UseLogScale { get; set; } = true;
        public int SmoothingFactor { get; set; } = 3;
        public float PeakDecayRate { get; set; } = 0.95f;
        public float ClippingThreshold { get; set; } = 0.99f;
    }
}
