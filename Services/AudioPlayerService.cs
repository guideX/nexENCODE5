using NAudio.Wave;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Service for playing audio files
    /// </summary>
    public class AudioPlayerService : IDisposable
    {
        private IWavePlayer? _waveOut;
        private AudioFileReader? _audioFileReader;
        private bool _isDisposed;
        
        public event EventHandler? PlaybackStopped;
        public event EventHandler<TimeSpan>? PositionChanged;
        
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
        /// Loads an audio file for playback
        /// </summary>
        public void Load(string filePath)
        {
            Stop();
            
            _audioFileReader = new AudioFileReader(filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFileReader);
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
        
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _isDisposed = true;
            }
        }
    }
}
