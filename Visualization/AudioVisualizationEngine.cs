using NAudio.Dsp;
using NAudio.Wave;

namespace nexENCODE_Studio.Visualization
{
    /// <summary>
    /// Core audio visualization engine providing real-time analysis
    /// </summary>
    public class AudioVisualizationEngine : IDisposable
    {
        private readonly VisualizationOptions _options;
        private NAudio.Dsp.Complex[] _fftBuffer;
        private float[] _window;
        private float[] _smoothedSpectrum;
        private float _leftDecayingPeak;
        private float _rightDecayingPeak;
        private bool _disposed;

        public event EventHandler<SpectrumData>? SpectrumUpdated;
        public event EventHandler<PeakLevelData>? PeakLevelsUpdated;
        public event EventHandler<AudioSampleData>? SamplesUpdated;

        public AudioVisualizationEngine(VisualizationOptions? options = null)
        {
            _options = options ?? new VisualizationOptions();
            _fftBuffer = new NAudio.Dsp.Complex[_options.FftSize];
            _window = CreateHannWindow(_options.FftSize);
            _smoothedSpectrum = new float[_options.SpectrumBands];
        }

        /// <summary>
        /// Processes audio samples for visualization
        /// </summary>
        public void ProcessSamples(float[] samples, int channels, int sampleRate)
        {
            if (samples.Length == 0) return;

            var sampleData = ExtractChannelData(samples, channels);
            
            // Calculate peak levels
            var peakData = CalculatePeakLevels(sampleData);
            OnPeakLevelsUpdated(peakData);

            // Perform FFT analysis
            var spectrumData = PerformFFTAnalysis(sampleData, sampleRate);
            OnSpectrumUpdated(spectrumData);

            // Emit sample data
            sampleData.SampleRate = sampleRate;
            OnSamplesUpdated(sampleData);
        }

        /// <summary>
        /// Extracts left and right channel data from interleaved samples
        /// </summary>
        private AudioSampleData ExtractChannelData(float[] samples, int channels)
        {
            var data = new AudioSampleData();

            if (channels == 1)
            {
                // Mono - duplicate to both channels
                data.LeftChannel = samples;
                data.RightChannel = samples;
            }
            else if (channels == 2)
            {
                // Stereo - split channels
                int sampleCount = samples.Length / 2;
                data.LeftChannel = new float[sampleCount];
                data.RightChannel = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    data.LeftChannel[i] = samples[i * 2];
                    data.RightChannel[i] = samples[i * 2 + 1];
                }
            }

            return data;
        }

        /// <summary>
        /// Calculates peak and RMS levels for VU meters
        /// </summary>
        private PeakLevelData CalculatePeakLevels(AudioSampleData sampleData)
        {
            var peakData = new PeakLevelData();

            // Calculate left channel
            float leftMax = 0f;
            float leftSumSquares = 0f;

            foreach (var sample in sampleData.LeftChannel)
            {
                float abs = Math.Abs(sample);
                if (abs > leftMax) leftMax = abs;
                leftSumSquares += sample * sample;
            }

            peakData.LeftPeak = leftMax;
            peakData.LeftRMS = (float)Math.Sqrt(leftSumSquares / sampleData.LeftChannel.Length);
            peakData.LeftClipping = leftMax >= _options.ClippingThreshold;

            // Calculate right channel
            float rightMax = 0f;
            float rightSumSquares = 0f;

            foreach (var sample in sampleData.RightChannel)
            {
                float abs = Math.Abs(sample);
                if (abs > rightMax) rightMax = abs;
                rightSumSquares += sample * sample;
            }

            peakData.RightPeak = rightMax;
            peakData.RightRMS = (float)Math.Sqrt(rightSumSquares / sampleData.RightChannel.Length);
            peakData.RightClipping = rightMax >= _options.ClippingThreshold;

            // Decaying peaks for peak hold display
            _leftDecayingPeak = Math.Max(peakData.LeftPeak, _leftDecayingPeak * _options.PeakDecayRate);
            _rightDecayingPeak = Math.Max(peakData.RightPeak, _rightDecayingPeak * _options.PeakDecayRate);

            peakData.LeftDecayingPeak = _leftDecayingPeak;
            peakData.RightDecayingPeak = _rightDecayingPeak;

            return peakData;
        }

        /// <summary>
        /// Performs FFT analysis for spectrum visualization
        /// </summary>
        private SpectrumData PerformFFTAnalysis(AudioSampleData sampleData, int sampleRate)
        {
            var spectrumData = new SpectrumData
            {
                FftSize = _options.FftSize,
                MaxFrequency = _options.MaxFrequency
            };

            // Prepare FFT input (mix left and right for now)
            int samplesToProcess = Math.Min(_options.FftSize, sampleData.LeftChannel.Length);
            
            for (int i = 0; i < samplesToProcess; i++)
            {
                float mixedSample = (sampleData.LeftChannel[i] + sampleData.RightChannel[i]) / 2f;
                _fftBuffer[i].X = mixedSample * _window[i];
                _fftBuffer[i].Y = 0;
            }

            // Fill remaining with zeros
            for (int i = samplesToProcess; i < _options.FftSize; i++)
            {
                _fftBuffer[i].X = 0;
                _fftBuffer[i].Y = 0;
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(_options.FftSize, 2), _fftBuffer);

            // Convert to magnitude spectrum
            var magnitudes = CalculateMagnitudes(_fftBuffer, sampleRate);

            // Map to frequency bands
            spectrumData.FrequencySpectrum = MapToFrequencyBands(magnitudes, sampleRate);
            spectrumData.FrequencyLabels = GenerateFrequencyLabels();

            return spectrumData;
        }

        /// <summary>
        /// Calculates magnitudes from FFT complex results
        /// </summary>
        private float[] CalculateMagnitudes(NAudio.Dsp.Complex[] fftBuffer, int sampleRate)
        {
            int magnitudeCount = fftBuffer.Length / 2;
            var magnitudes = new float[magnitudeCount];

            for (int i = 0; i < magnitudeCount; i++)
            {
                float real = fftBuffer[i].X;
                float imaginary = fftBuffer[i].Y;
                float magnitude = (float)Math.Sqrt(real * real + imaginary * imaginary);
                
                // Convert to dB
                magnitudes[i] = 20 * (float)Math.Log10(magnitude + 1e-10f);
            }

            return magnitudes;
        }

        /// <summary>
        /// Maps FFT magnitudes to frequency bands
        /// </summary>
        private float[] MapToFrequencyBands(float[] magnitudes, int sampleRate)
        {
            var bands = new float[_options.SpectrumBands];
            float nyquist = sampleRate / 2f;

            for (int i = 0; i < _options.SpectrumBands; i++)
            {
                float frequency;
                
                if (_options.UseLogScale)
                {
                    // Logarithmic frequency scale (better for human hearing)
                    float logMin = (float)Math.Log10(_options.MinFrequency);
                    float logMax = (float)Math.Log10(_options.MaxFrequency);
                    float logFreq = logMin + (logMax - logMin) * i / _options.SpectrumBands;
                    frequency = (float)Math.Pow(10, logFreq);
                }
                else
                {
                    // Linear frequency scale
                    frequency = _options.MinFrequency + 
                        (_options.MaxFrequency - _options.MinFrequency) * i / _options.SpectrumBands;
                }

                // Map frequency to FFT bin
                int bin = (int)(frequency * magnitudes.Length / nyquist);
                bin = Math.Min(bin, magnitudes.Length - 1);

                bands[i] = magnitudes[bin];
            }

            // Apply smoothing
            ApplySmoothing(bands);

            return bands;
        }

        /// <summary>
        /// Applies temporal smoothing to spectrum data
        /// </summary>
        private void ApplySmoothing(float[] currentSpectrum)
        {
            for (int i = 0; i < _options.SpectrumBands; i++)
            {
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * 
                    (_options.SmoothingFactor / (_options.SmoothingFactor + 1f)) +
                    currentSpectrum[i] / (_options.SmoothingFactor + 1f);
                
                currentSpectrum[i] = _smoothedSpectrum[i];
            }
        }

        /// <summary>
        /// Generates frequency labels for spectrum display
        /// </summary>
        private float[] GenerateFrequencyLabels()
        {
            var labels = new float[_options.SpectrumBands];

            for (int i = 0; i < _options.SpectrumBands; i++)
            {
                if (_options.UseLogScale)
                {
                    float logMin = (float)Math.Log10(_options.MinFrequency);
                    float logMax = (float)Math.Log10(_options.MaxFrequency);
                    float logFreq = logMin + (logMax - logMin) * i / _options.SpectrumBands;
                    labels[i] = (float)Math.Pow(10, logFreq);
                }
                else
                {
                    labels[i] = _options.MinFrequency + 
                        (_options.MaxFrequency - _options.MinFrequency) * i / _options.SpectrumBands;
                }
            }

            return labels;
        }

        /// <summary>
        /// Creates a Hann window for FFT
        /// </summary>
        private float[] CreateHannWindow(int size)
        {
            var window = new float[size];
            for (int i = 0; i < size; i++)
            {
                window[i] = 0.5f * (1 - (float)Math.Cos(2 * Math.PI * i / (size - 1)));
            }
            return window;
        }

        /// <summary>
        /// Resets the visualization engine state
        /// </summary>
        public void Reset()
        {
            Array.Clear(_smoothedSpectrum, 0, _smoothedSpectrum.Length);
            _leftDecayingPeak = 0f;
            _rightDecayingPeak = 0f;
        }

        protected virtual void OnSpectrumUpdated(SpectrumData data)
        {
            SpectrumUpdated?.Invoke(this, data);
        }

        protected virtual void OnPeakLevelsUpdated(PeakLevelData data)
        {
            PeakLevelsUpdated?.Invoke(this, data);
        }

        protected virtual void OnSamplesUpdated(AudioSampleData data)
        {
            SamplesUpdated?.Invoke(this, data);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
