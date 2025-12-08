using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace nexENCODE_Studio.Visualization
{
    /// <summary>
    /// Sample provider that captures audio data for visualization
    /// </summary>
    public class VisualizationSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly AudioVisualizationEngine _visualizationEngine;
        private readonly int _notificationCount;
        private int _samplesSinceLastNotification;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public VisualizationSampleProvider(ISampleProvider source, AudioVisualizationEngine visualizationEngine, int notificationCount = 2048)
        {
            _source = source;
            _visualizationEngine = visualizationEngine;
            _notificationCount = notificationCount;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            if (samplesRead > 0)
            {
                // Accumulate samples
                _samplesSinceLastNotification += samplesRead;

                // When we have enough samples, send to visualization engine
                if (_samplesSinceLastNotification >= _notificationCount)
                {
                    var samplesToProcess = new float[samplesRead];
                    Array.Copy(buffer, offset, samplesToProcess, 0, samplesRead);

                    _visualizationEngine.ProcessSamples(
                        samplesToProcess,
                        WaveFormat.Channels,
                        WaveFormat.SampleRate
                    );

                    _samplesSinceLastNotification = 0;
                }
            }

            return samplesRead;
        }
    }
}
