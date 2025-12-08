using NAudio.Wave;
using nexENCODE_Studio.Models;
using nexENCODE_Studio.Visualization;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Service for playing audio files with visualization support
    /// </summary>
    public class AudioPlayerService : IDisposable
    {
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFileReader;
        private VisualizationSampleProvider? _visualizationProvider;
        private AudioVisualizationEngine? _visualizationEngine;
        private bool _isDisposed;
        
        public event EventHandler? PlaybackStopped;
        public event EventHandler<TimeSpan>? PositionChanged;
        
        /// <summary>
        /// Gets the visualization engine for real-time audio visualization
        /// </summary>
        public AudioVisualizationEngine? VisualizationEngine => _visualizationEngine;
        
        /// <summary>
        /// Gets or sets whether visualization is enabled
        /// </summary>
        public bool VisualizationEnabled { get; set; } = true;
        
        /// <summary>
        /// Gets whether audio is currently playing
        /// </summary>
        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
        
        /// <summary>
        /// Gets whether audio is paused
        /// </summary>
        public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;
        
        /// <summary>
        /// Gets the current position in the audio file
        /// </summary>
        public TimeSpan CurrentTime => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
        
        /// <summary>
        /// Gets the total duration of the current audio file
        /// </summary>
        public TimeSpan TotalTime => _audioFileReader?.TotalTime ?? TimeSpan.Zero;
        
        /// <summary>
        /// Gets or sets the volume (0.0 to 1.0)
        /// </summary>
        public float Volume
        {
            get => _audioFileReader?.Volume ?? 1.0f;
            set
            {
                if (_audioFileReader != null)
                    _audioFileReader.Volume = Math.Clamp(value, 0.0f, 1.0f);
            }
        }
        
        /// <summary>
        /// Loads an audio file for playback with optional visualization
        /// </summary>
        public void Load(string filePath, VisualizationOptions? visualizationOptions = null)
        {
            Stop();
            
            _audioFileReader = new AudioFileReader(filePath);
            _waveOut = new WaveOutEvent();
            
            if (VisualizationEnabled)
            {
                // Set up visualization
                _visualizationEngine = new AudioVisualizationEngine(visualizationOptions);
                _visualizationProvider = new VisualizationSampleProvider(
                    _audioFileReader, 
                    _visualizationEngine
                );
                _waveOut.Init(_visualizationProvider);
            }
            else
            {
                _waveOut.Init(_audioFileReader);
            }
            
            _waveOut.PlaybackStopped += OnPlaybackStopped;
        }
        
        /// <summary>
        /// Starts or resumes playback
        /// </summary>
        public void Play()
        {
            _waveOut?.Play();
        }
        
        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            _waveOut?.Pause();
        }
        
        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            _waveOut?.Stop();
            _audioFileReader?.Dispose();
            _waveOut?.Dispose();
            _audioFileReader = null;
            _waveOut = null;
        }
        
        /// <summary>
        /// Seeks to a specific position in the audio
        /// </summary>
        public void Seek(TimeSpan position)
        {
            if (_audioFileReader != null)
            {
                _audioFileReader.CurrentTime = position;
                PositionChanged?.Invoke(this, position);
            }
        }
        
        /// <summary>
        /// Plays a specific audio file
        /// </summary>
        public void PlayFile(string filePath)
        {
            Load(filePath);
            Play();
        }
        
        /// <summary>
        /// Generates waveform data for the currently loaded file
        /// </summary>
        public WaveformData? GenerateWaveform(int width, int samplesPerPixel = 128)
        {
            if (_audioFileReader == null) return null;
            
            // Save current position
            var currentPosition = _audioFileReader.CurrentTime;
            
            // Generate waveform
            var waveform = WaveformGenerator.GenerateWaveform(
                _audioFileReader.FileName, 
                width, 
                samplesPerPixel
            );
            
            // Restore position
            _audioFileReader.CurrentTime = currentPosition;
            
            return waveform;
        }
        
        /// <summary>
        /// Gets real-time visualization engine
        /// </summary>
        public AudioVisualizationEngine? GetVisualizationEngine()
        {
            return _visualizationEngine;
        }
        
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _visualizationEngine?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
