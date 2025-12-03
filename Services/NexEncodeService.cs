using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Main service orchestrator for nexENCODE Studio operations
    /// </summary>
    public class NexEncodeService
    {
        private readonly CdRipperService _cdRipper;
        private readonly AudioEncoderService _encoder;
        private readonly AudioPlayerService _player;
        private readonly PlaylistService _playlist;
        
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        
        public CdRipperService CdRipper => _cdRipper;
        public AudioEncoderService Encoder => _encoder;
        public AudioPlayerService Player => _player;
        public PlaylistService Playlist => _playlist;
        
        public NexEncodeService()
        {
            _cdRipper = new CdRipperService();
            _encoder = new AudioEncoderService();
            _player = new AudioPlayerService();
            _playlist = new PlaylistService();
            
            // Wire up progress events
            _cdRipper.ProgressChanged += (s, e) => ProgressChanged?.Invoke(s, e);
            _encoder.ProgressChanged += (s, e) => ProgressChanged?.Invoke(s, e);
        }
        
        /// <summary>
        /// Rips a CD and encodes directly to MP3
        /// </summary>
        public async Task<List<string>> RipAndEncodeAsync(
            CdInfo cdInfo,
            string outputDirectory,
            EncodingOptions encodingOptions,
            CancellationToken cancellationToken = default)
        {
            var encodedFiles = new List<string>();
            string tempDir = Path.Combine(Path.GetTempPath(), "nexENCODE_temp");
            
            try
            {
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(outputDirectory);
                
                // Rip all tracks to WAV first
                OnProgressChanged(new ProgressEventArgs
                {
                    CurrentOperation = "Ripping",
                    StatusMessage = "Ripping CD tracks to WAV..."
                });
                
                var wavFiles = await _cdRipper.RipAllTracksAsync(cdInfo, tempDir, cancellationToken);
                
                // Encode to MP3
                OnProgressChanged(new ProgressEventArgs
                {
                    CurrentOperation = "Encoding",
                    StatusMessage = "Encoding WAV files to MP3..."
                });
                
                encodingOptions.OutputDirectory = outputDirectory;
                encodedFiles = await _encoder.BatchConvertWavToMp3Async(wavFiles, cdInfo.Tracks, encodingOptions, cancellationToken);
                
                // Clean up temporary WAV files
                foreach (var wavFile in wavFiles)
                {
                    try { File.Delete(wavFile); } catch { }
                }
                
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = 100,
                    IsComplete = true,
                    StatusMessage = $"Successfully ripped and encoded {encodedFiles.Count} tracks"
                });
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error during rip and encode: {ex.Message}"
                });
                throw;
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { }
            }
            
            return encodedFiles;
        }
        
        /// <summary>
        /// Creates a playlist from ripped tracks
        /// </summary>
        public void CreatePlaylistFromTracks(string playlistPath, List<AudioTrack> tracks)
        {
            _playlist.WriteM3uPlaylist(playlistPath, tracks, extended: true);
            
            OnProgressChanged(new ProgressEventArgs
            {
                StatusMessage = $"Playlist created: {playlistPath}"
            });
        }
        
        /// <summary>
        /// Gets information about the CD in the drive
        /// </summary>
        public CdInfo GetCdInfo()
        {
            return _cdRipper.ReadCdInfo();
        }
        
        /// <summary>
        /// Converts a single file between formats
        /// </summary>
        public async Task<string> ConvertFileAsync(
            string inputPath,
            AudioFormat targetFormat,
            EncodingOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new EncodingOptions { Format = targetFormat };
            options.Format = targetFormat;
            
            string inputExt = Path.GetExtension(inputPath).ToLowerInvariant();
            
            // MP3 to WAV
            if (inputExt == ".mp3" && targetFormat == AudioFormat.Wav)
            {
                return await _encoder.ConvertMp3ToWavAsync(inputPath, null, cancellationToken);
            }
            // WAV to MP3
            else if (inputExt == ".wav" && targetFormat == AudioFormat.Mp3)
            {
                var metadata = _encoder.GetAudioFileInfo(inputPath);
                return await _encoder.ConvertWavToMp3Async(inputPath, metadata, options, cancellationToken);
            }
            else
            {
                throw new NotSupportedException($"Conversion from {inputExt} to {targetFormat} is not yet implemented");
            }
        }
        
        protected virtual void OnProgressChanged(ProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
