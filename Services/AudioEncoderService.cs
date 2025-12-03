using NAudio.Wave;
using NAudio.Lame;
using nexENCODE_Studio.Models;
using TagLib;
using System.IO;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Service for encoding audio files to various formats, primarily MP3
    /// </summary>
    public class AudioEncoderService
    {
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        
        /// <summary>
        /// Converts WAV file to MP3
        /// </summary>
        public async Task<string> ConvertWavToMp3Async(string wavFilePath, AudioTrack? metadata, EncodingOptions options, CancellationToken cancellationToken = default)
        {
            if (!System.IO.File.Exists(wavFilePath))
                throw new FileNotFoundException("WAV file not found", wavFilePath);
                
            string outputPath = GetOutputPath(wavFilePath, options);
            
            OnProgressChanged(new ProgressEventArgs
            {
                CurrentOperation = "Encoding",
                StatusMessage = $"Encoding {Path.GetFileName(wavFilePath)} to MP3..."
            });
            
            try
            {
                await Task.Run(() =>
                {
                    using (var reader = new AudioFileReader(wavFilePath))
                    using (var writer = new LameMP3FileWriter(outputPath, reader.WaveFormat, options.GetBitrate()))
                    {
                        byte[] buffer = new byte[reader.WaveFormat.SampleRate * 4];
                        int bytesRead;
                        long totalBytes = reader.Length;
                        long bytesProcessed = 0;
                        
                        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                System.IO.File.Delete(outputPath);
                                return;
                            }
                            
                            writer.Write(buffer, 0, bytesRead);
                            bytesProcessed += bytesRead;
                            
                            int progress = (int)((bytesProcessed * 100) / totalBytes);
                            OnProgressChanged(new ProgressEventArgs
                            {
                                PercentComplete = progress,
                                StatusMessage = $"Encoding: {progress}%"
                            });
                        }
                    }
                    
                    // Write ID3 tags if metadata is provided
                    if (options.WriteId3Tags && metadata != null)
                    {
                        WriteId3Tags(outputPath, metadata);
                    }
                    
                }, cancellationToken);
                
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = 100,
                    IsComplete = true,
                    StatusMessage = "Encoding complete"
                });
                
                return outputPath;
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Encoding error: {ex.Message}"
                });
                throw;
            }
        }
        
        /// <summary>
        /// Converts MP3 file to WAV
        /// </summary>
        public async Task<string> ConvertMp3ToWavAsync(string mp3FilePath, string? outputPath = null, CancellationToken cancellationToken = default)
        {
            if (!System.IO.File.Exists(mp3FilePath))
                throw new FileNotFoundException("MP3 file not found", mp3FilePath);
                
            outputPath ??= Path.ChangeExtension(mp3FilePath, ".wav");
            
            OnProgressChanged(new ProgressEventArgs
            {
                CurrentOperation = "Decoding",
                StatusMessage = $"Converting {Path.GetFileName(mp3FilePath)} to WAV..."
            });
            
            try
            {
                await Task.Run(() =>
                {
                    using (var reader = new Mp3FileReader(mp3FilePath))
                    using (var writer = new WaveFileWriter(outputPath, reader.WaveFormat))
                    {
                        byte[] buffer = new byte[reader.WaveFormat.SampleRate * 4];
                        int bytesRead;
                        long totalBytes = reader.Length;
                        long bytesProcessed = 0;
                        
                        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                System.IO.File.Delete(outputPath);
                                return;
                            }
                            
                            writer.Write(buffer, 0, bytesRead);
                            bytesProcessed += bytesRead;
                            
                            int progress = (int)((bytesProcessed * 100) / totalBytes);
                            OnProgressChanged(new ProgressEventArgs
                            {
                                PercentComplete = progress,
                                StatusMessage = $"Converting: {progress}%"
                            });
                        }
                    }
                }, cancellationToken);
                
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = 100,
                    IsComplete = true,
                    StatusMessage = "Conversion complete"
                });
                
                return outputPath;
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Conversion error: {ex.Message}"
                });
                throw;
            }
        }
        
        /// <summary>
        /// Batch converts multiple WAV files to MP3
        /// </summary>
        public async Task<List<string>> BatchConvertWavToMp3Async(List<string> wavFiles, List<AudioTrack>? metadata, EncodingOptions options, CancellationToken cancellationToken = default)
        {
            var outputFiles = new List<string>();
            
            for (int i = 0; i < wavFiles.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var wavFile = wavFiles[i];
                var trackMetadata = metadata != null && i < metadata.Count ? metadata[i] : null;
                
                OnProgressChanged(new ProgressEventArgs
                {
                    CurrentOperation = "Encoding",
                    StatusMessage = $"Encoding file {i + 1} of {wavFiles.Count}",
                    CurrentTrack = trackMetadata
                });
                
                string outputFile = await ConvertWavToMp3Async(wavFile, trackMetadata, options, cancellationToken);
                outputFiles.Add(outputFile);
            }
            
            return outputFiles;
        }
        
        /// <summary>
        /// Gets information about an audio file
        /// </summary>
        public AudioTrack GetAudioFileInfo(string filePath)
        {
            var track = new AudioTrack
            {
                FilePath = filePath
            };
            
            try
            {
                using (var reader = new AudioFileReader(filePath))
                {
                    track.Duration = reader.TotalTime;
                }
                
                var fileInfo = new FileInfo(filePath);
                track.FileSize = fileInfo.Length;
                
                // Try to read ID3 tags if it's an MP3
                if (Path.GetExtension(filePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    ReadId3Tags(filePath, track);
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error reading file info: {ex.Message}"
                });
            }
            
            return track;
        }
        
        #region Helper Methods
        
        private string GetOutputPath(string inputFile, EncodingOptions options)
        {
            string directory = !string.IsNullOrEmpty(options.OutputDirectory) 
                ? options.OutputDirectory 
                : Path.GetDirectoryName(inputFile) ?? "";
                
            string fileName = Path.GetFileNameWithoutExtension(inputFile);
            string extension = options.Format switch
            {
                AudioFormat.Mp3 => ".mp3",
                AudioFormat.Wav => ".wav",
                AudioFormat.Ogg => ".ogg",
                AudioFormat.Flac => ".flac",
                AudioFormat.Alac => ".m4a",
                _ => ".mp3"
            };
            
            return Path.Combine(directory, fileName + extension);
        }
        
        private void WriteId3Tags(string mp3FilePath, AudioTrack metadata)
        {
            try
            {
                var file = TagLib.File.Create(mp3FilePath);
                file.Tag.Title = metadata.Title;
                file.Tag.Performers = new[] { metadata.Artist };
                file.Tag.Album = metadata.Album;
                file.Tag.Track = (uint)metadata.TrackNumber;
                if (metadata.Year > 0)
                    file.Tag.Year = (uint)metadata.Year;
                if (!string.IsNullOrEmpty(metadata.Genre))
                    file.Tag.Genres = new[] { metadata.Genre };
                file.Save();
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error writing ID3 tags: {ex.Message}"
                });
            }
        }
        
        private void ReadId3Tags(string mp3FilePath, AudioTrack track)
        {
            try
            {
                var file = TagLib.File.Create(mp3FilePath);
                track.Title = file.Tag.Title ?? "";
                track.Artist = file.Tag.FirstPerformer ?? "";
                track.Album = file.Tag.Album ?? "";
                track.TrackNumber = (int)file.Tag.Track;
                track.Year = (int)file.Tag.Year;
                track.Genre = file.Tag.FirstGenre ?? "";
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error reading ID3 tags: {ex.Message}"
                });
            }
        }
        
        #endregion
        
        protected virtual void OnProgressChanged(ProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
