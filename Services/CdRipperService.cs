using NAudio.Wave;
using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services.Native;
using nexENCODE_Studio.Services.Metadata;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Service for reading audio CD tracks using Windows MCI
    /// </summary>
    public class CdRipperService
    {
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        
        private char _cdDriveLetter = 'D';
        private CdMetadataService? _metadataService;
        
        /// <summary>
        /// Gets or sets the CD drive letter
        /// </summary>
        public char CdDriveLetter
        {
            get => _cdDriveLetter;
            set => _cdDriveLetter = char.ToUpper(value);
        }
        
        /// <summary>
        /// Gets or sets whether to automatically lookup metadata
        /// </summary>
        public bool AutoLookupMetadata { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the metadata lookup options
        /// </summary>
        public MetadataLookupOptions MetadataOptions { get; set; } = new MetadataLookupOptions();
        
        /// <summary>
        /// Gets all available CD drives
        /// </summary>
        public List<char> GetAvailableCdDrives()
        {
            var cdDrives = new List<char>();
            
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.DriveType == DriveType.CDRom)
                {
                    cdDrives.Add(drive.Name[0]);
                }
            }
            
            return cdDrives;
        }
        
        /// <summary>
        /// Checks if a CD is present in the drive
        /// </summary>
        public bool IsCdPresent()
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    return cdDrive.IsCdPresent();
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets detailed drive information
        /// </summary>
        public CdDriveInfo GetDriveInfo()
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    return cdDrive.GetDriveInfo();
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error reading drive info: {ex.Message}"
                });
                throw;
            }
        }
        
        /// <summary>
        /// Reads available CD tracks from the drive using MCI with optional metadata lookup
        /// </summary>
        public CdInfo ReadCdInfo(bool lookupMetadata = true)
        {
            var cdInfo = new CdInfo();
            
            try
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    StatusMessage = $"Reading CD from drive {_cdDriveLetter}:..."
                });
                
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    
                    if (!cdDrive.IsCdPresent())
                    {
                        throw new InvalidOperationException($"No CD found in drive {_cdDriveLetter}:");
                    }
                    
                    // Get all tracks from the CD
                    var nativeTracks = cdDrive.GetAllTracks();
                    
                    OnProgressChanged(new ProgressEventArgs
                    {
                        StatusMessage = $"Found {nativeTracks.Count} tracks on CD"
                    });
                    
                    // Convert to our track model
                    foreach (var nativeTrack in nativeTracks)
                    {
                        cdInfo.Tracks.Add(new AudioTrack
                        {
                            TrackNumber = nativeTrack.TrackNumber,
                            Title = $"Track {nativeTrack.TrackNumber:00}",
                            Duration = nativeTrack.Length,
                            Artist = string.Empty,
                            Album = string.Empty
                        });
                    }
                    
                    // Calculate disc ID for CDDB lookup
                    cdInfo.DiscId = CalculateDiscId(nativeTracks);
                }
                
                // Automatic metadata lookup
                if (lookupMetadata && AutoLookupMetadata)
                {
                    OnProgressChanged(new ProgressEventArgs
                    {
                        StatusMessage = "Looking up CD metadata..."
                    });
                    
                    var metadata = LookupMetadataAsync(cdInfo).GetAwaiter().GetResult();
                    if (metadata != null && metadata.Success)
                    {
                        OnProgressChanged(new ProgressEventArgs
                        {
                            StatusMessage = $"Metadata found: {cdInfo.Artist} - {cdInfo.Album}"
                        });
                    }
                    else
                    {
                        OnProgressChanged(new ProgressEventArgs
                        {
                            StatusMessage = "No metadata found - tracks will use default names"
                        });
                    }
                }
                
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = 100,
                    IsComplete = true,
                    StatusMessage = $"CD info read successfully: {cdInfo.TotalTracks} tracks"
                });
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error reading CD: {ex.Message}"
                });
                throw;
            }
            
            return cdInfo;
        }
        
        /// <summary>
        /// Looks up CD metadata from online databases
        /// </summary>
        public async Task<MetadataLookupResult?> LookupMetadataAsync(CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                // Initialize metadata service if not already done
                if (_metadataService == null)
                {
                    _metadataService = new CdMetadataService(MetadataOptions);
                    _metadataService.StatusChanged += (s, status) =>
                    {
                        OnProgressChanged(new ProgressEventArgs
                        {
                            StatusMessage = status
                        });
                    };
                }
                
                // Lookup metadata
                var result = await _metadataService.LookupCdMetadataAsync(cdInfo, cancellationToken);
                
                return result;
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Metadata lookup failed: {ex.Message}"
                });
                return null;
            }
        }
        
        /// <summary>
        /// Manually sets metadata for a CD (when auto-lookup fails)
        /// </summary>
        public void SetManualMetadata(CdInfo cdInfo, string artist, string album, int year, string genre, List<string> trackNames)
        {
            cdInfo.Artist = artist;
            cdInfo.Album = album;
            cdInfo.Year = year;
            cdInfo.Genre = genre;
            
            for (int i = 0; i < Math.Min(cdInfo.Tracks.Count, trackNames.Count); i++)
            {
                cdInfo.Tracks[i].Title = trackNames[i];
                cdInfo.Tracks[i].Artist = artist;
                cdInfo.Tracks[i].Album = album;
                cdInfo.Tracks[i].Year = year;
                cdInfo.Tracks[i].Genre = genre;
            }
        }
        
        /// <summary>
        /// Rips a CD track to WAV format using digital audio extraction
        /// </summary>
        public async Task<string> RipTrackToWavAsync(AudioTrack track, string outputPath, CancellationToken cancellationToken = default)
        {
            string outputFile = Path.Combine(outputPath, $"Track{track.TrackNumber:00}.wav");
            
            OnProgressChanged(new ProgressEventArgs
            {
                CurrentTrack = track,
                CurrentOperation = "Ripping",
                StatusMessage = $"Ripping track {track.TrackNumber}: {track.Title}"
            });
            
            try
            {
                await Task.Run(() =>
                {
                    // Get track info from CD
                    CdTrackInfo? trackInfo = null;
                    using (var cdDrive = new CdDriveService(_cdDriveLetter))
                    {
                        cdDrive.Open();
                        trackInfo = cdDrive.GetTrackInfo(track.TrackNumber);
                    }
                    
                    if (trackInfo == null)
                    {
                        throw new InvalidOperationException($"Could not read track {track.TrackNumber} information");
                    }
                    
                    // Read digital audio data from CD
                    using (var reader = new CdDigitalAudioReader(_cdDriveLetter))
                    {
                        var progress = new Progress<int>(percent =>
                        {
                            OnProgressChanged(new ProgressEventArgs
                            {
                                PercentComplete = percent,
                                CurrentTrack = track,
                                StatusMessage = $"Ripping track {track.TrackNumber}: {percent}%"
                            });
                        });
                        
                        reader.ReadTrackToWav(track.TrackNumber, trackInfo, outputFile, progress, cancellationToken);
                    }
                    
                }, cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested)
                {
                    OnProgressChanged(new ProgressEventArgs
                    {
                        PercentComplete = 100,
                        CurrentTrack = track,
                        IsComplete = true,
                        StatusMessage = $"Successfully ripped track {track.TrackNumber}"
                    });
                }
                
                return outputFile;
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error ripping track {track.TrackNumber}: {ex.Message}"
                });
                throw;
            }
        }
        
        /// <summary>
        /// Rips all tracks from a CD
        /// </summary>
        public async Task<List<string>> RipAllTracksAsync(CdInfo cdInfo, string outputPath, CancellationToken cancellationToken = default)
        {
            var outputFiles = new List<string>();
            
            // Ensure output directory exists
            Directory.CreateDirectory(outputPath);
            
            OnProgressChanged(new ProgressEventArgs
            {
                StatusMessage = $"Starting to rip {cdInfo.TotalTracks} tracks..."
            });
            
            for (int i = 0; i < cdInfo.Tracks.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var track = cdInfo.Tracks[i];
                int overallProgress = (int)((i / (double)cdInfo.Tracks.Count) * 100);
                
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = overallProgress,
                    CurrentTrack = track,
                    StatusMessage = $"Ripping track {i + 1} of {cdInfo.Tracks.Count}"
                });
                
                try
                {
                    string outputFile = await RipTrackToWavAsync(track, outputPath, cancellationToken);
                    outputFiles.Add(outputFile);
                }
                catch (Exception ex)
                {
                    OnProgressChanged(new ProgressEventArgs
                    {
                        Error = ex,
                        StatusMessage = $"Failed to rip track {track.TrackNumber}: {ex.Message}"
                    });
                    
                    // Continue with next track instead of failing completely
                }
            }
            
            if (!cancellationToken.IsCancellationRequested)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    PercentComplete = 100,
                    IsComplete = true,
                    StatusMessage = $"Ripping complete: {outputFiles.Count}/{cdInfo.TotalTracks} tracks ripped successfully"
                });
            }
            
            return outputFiles;
        }
        
        /// <summary>
        /// Plays a CD track for preview (uses MCI playback)
        /// </summary>
        public void PreviewTrack(int trackNumber)
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    cdDrive.PlayTrack(trackNumber);
                    
                    OnProgressChanged(new ProgressEventArgs
                    {
                        StatusMessage = $"Playing track {trackNumber} from CD..."
                    });
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error playing track: {ex.Message}"
                });
            }
        }
        
        /// <summary>
        /// Stops CD playback preview
        /// </summary>
        public void StopPreview()
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    cdDrive.Stop();
                }
            }
            catch
            {
                // Ignore stop errors
            }
        }
        
        /// <summary>
        /// Ejects the CD from the drive
        /// </summary>
        public void EjectCd()
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    cdDrive.Eject();
                    
                    OnProgressChanged(new ProgressEventArgs
                    {
                        StatusMessage = "CD ejected"
                    });
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error ejecting CD: {ex.Message}"
                });
            }
        }
        
        /// <summary>
        /// Closes the CD tray
        /// </summary>
        public void CloseTray()
        {
            try
            {
                using (var cdDrive = new CdDriveService(_cdDriveLetter))
                {
                    cdDrive.Open();
                    cdDrive.CloseTray();
                    
                    OnProgressChanged(new ProgressEventArgs
                    {
                        StatusMessage = "CD tray closed"
                    });
                }
            }
            catch (Exception ex)
            {
                OnProgressChanged(new ProgressEventArgs
                {
                    Error = ex,
                    StatusMessage = $"Error closing tray: {ex.Message}"
                });
            }
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Calculates CDDB disc ID from track information
        /// </summary>
        private string CalculateDiscId(List<CdTrackInfo> tracks)
        {
            if (tracks.Count == 0)
                return Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // CDDB disc ID calculation
            // This is a simplified version - real CDDB uses frame offsets
            int n = 0;
            
            foreach (var track in tracks)
            {
                int seconds = (int)track.StartPosition.TotalSeconds;
                while (seconds > 0)
                {
                    n += seconds % 10;
                    seconds /= 10;
                }
            }
            
            int totalSeconds = (int)tracks.Sum(t => t.Length.TotalSeconds);
            int trackCount = tracks.Count;
            
            // Format: ((n % 0xff) << 24 | t << 8 | trackCount)
            uint discId = (uint)(((n % 0xff) << 24) | (totalSeconds << 8) | trackCount);
            
            return discId.ToString("x8");
        }
        
        #endregion
        
        protected virtual void OnProgressChanged(ProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
