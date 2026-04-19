using System.Runtime.InteropServices;

namespace nexENCODE_Studio.Services.Native
{
    /// <summary>
    /// CD drive information and status
    /// </summary>
    public class CdDriveInfo
    {
        public char DriveLetter { get; set; }
        public bool IsReady { get; set; }
        public bool HasAudioCD { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public int TrackCount { get; set; }
    }

    /// <summary>
    /// CD track information from TOC (Table of Contents)
    /// </summary>
    public class CdTrackInfo
    {
        public int TrackNumber { get; set; }
        public TimeSpan StartPosition { get; set; }
        public TimeSpan Length { get; set; }
        public bool IsAudio { get; set; }
    }

    /// <summary>
    /// Service for low-level CD drive operations using MCI
    /// </summary>
    internal class CdDriveService : IDisposable
    {
        private readonly char _driveLetter;
        private readonly string _deviceAlias;
        private bool _isOpen;
        private bool _disposed;

        public CdDriveService(char driveLetter)
        {
            _driveLetter = char.ToUpper(driveLetter);
            _deviceAlias = $"cdaudio_{_driveLetter}_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Opens the CD drive for operations
        /// </summary>
        public void Open()
        {
            if (_isOpen)
                return;

            try
            {
                // Validate the drive exists and is a CD-ROM before issuing MCI commands
                var drives = DriveInfo.GetDrives();
                bool found = false;
                foreach (var d in drives)
                {
                    if (char.ToUpper(d.Name[0]) == _driveLetter)
                    {
                        found = true;
                        if (d.DriveType != DriveType.CDRom)
                        {
                            throw new InvalidOperationException($"Drive {_driveLetter}: is not a CD-ROM drive.");
                        }
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException($"Drive {_driveLetter}: not found on this system.");
                }

                // Open the CD drive
                MciNativeMethods.ExecuteCommandNoReturn(
                    $"open {_driveLetter}: type cdaudio alias {_deviceAlias} shareable"
                );

                // Set time format to milliseconds for precise control
                MciNativeMethods.ExecuteCommandNoReturn(
                    $"set {_deviceAlias} time format milliseconds"
                );

                _isOpen = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open CD drive {_driveLetter}:", ex);
            }
        }

        /// <summary>
        /// Closes the CD drive
        /// </summary>
        public void Close()
        {
            if (!_isOpen)
                return;

            try
            {
                MciNativeMethods.ExecuteCommandNoReturn($"close {_deviceAlias}");
                _isOpen = false;
            }
            catch
            {
                // Ignore close errors
            }
        }

        /// <summary>
        /// Checks if a CD is present in the drive
        /// </summary>
        public bool IsCdPresent()
        {
            if (!_isOpen)
                Open();

            try
            {
                var status = MciNativeMethods.ExecuteCommand($"status {_deviceAlias} mode");
                return !string.IsNullOrEmpty(status) && !status.Contains("open", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the number of tracks on the CD
        /// </summary>
        public int GetTrackCount()
        {
            if (!_isOpen)
                Open();

            try
            {
                var result = MciNativeMethods.ExecuteCommand($"status {_deviceAlias} number of tracks");
                return int.TryParse(result.Trim(), out int count) ? count : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets information about a specific track
        /// </summary>
        public CdTrackInfo GetTrackInfo(int trackNumber)
        {
            if (!_isOpen)
                Open();

            var trackInfo = new CdTrackInfo
            {
                TrackNumber = trackNumber,
                IsAudio = true // CD-DA tracks are always audio
            };

            try
            {
                // Get track start position in milliseconds
                var startPosStr = MciNativeMethods.ExecuteCommand(
                    $"status {_deviceAlias} position track {trackNumber}"
                );
                if (int.TryParse(startPosStr.Trim(), out int startMs))
                {
                    trackInfo.StartPosition = TimeSpan.FromMilliseconds(startMs);
                }

                // Get track length in milliseconds
                var lengthStr = MciNativeMethods.ExecuteCommand(
                    $"status {_deviceAlias} length track {trackNumber}"
                );
                if (int.TryParse(lengthStr.Trim(), out int lengthMs))
                {
                    trackInfo.Length = TimeSpan.FromMilliseconds(lengthMs);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get track {trackNumber} info", ex);
            }

            return trackInfo;
        }

        /// <summary>
        /// Gets information about all tracks on the CD
        /// </summary>
        public List<CdTrackInfo> GetAllTracks()
        {
            var tracks = new List<CdTrackInfo>();
            int trackCount = GetTrackCount();

            for (int i = 1; i <= trackCount; i++)
            {
                try
                {
                    tracks.Add(GetTrackInfo(i));
                }
                catch
                {
                    // Skip tracks that can't be read
                }
            }

            return tracks;
        }

        /// <summary>
        /// Plays a CD track (for preview/testing)
        /// </summary>
        public void PlayTrack(int trackNumber)
        {
            if (!_isOpen)
                Open();

            MciNativeMethods.ExecuteCommandNoReturn(
                $"play {_deviceAlias} from {trackNumber} to {trackNumber + 1}"
            );
        }

        /// <summary>
        /// Stops CD playback
        /// </summary>
        public void Stop()
        {
            if (!_isOpen)
                return;

            try
            {
                MciNativeMethods.ExecuteCommandNoReturn($"stop {_deviceAlias}");
            }
            catch
            {
                // Ignore stop errors
            }
        }

        /// <summary>
        /// Ejects the CD
        /// </summary>
        public void Eject()
        {
            if (!_isOpen)
                Open();

            try
            {
                MciNativeMethods.ExecuteCommandNoReturn($"set {_deviceAlias} door open");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to eject CD", ex);
            }
        }

        /// <summary>
        /// Closes the CD tray
        /// </summary>
        public void CloseTray()
        {
            if (!_isOpen)
                Open();

            try
            {
                MciNativeMethods.ExecuteCommandNoReturn($"set {_deviceAlias} door closed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to close CD tray", ex);
            }
        }

        /// <summary>
        /// Gets CD drive information
        /// </summary>
        public CdDriveInfo GetDriveInfo()
        {
            var info = new CdDriveInfo
            {
                DriveLetter = _driveLetter,
                IsReady = false,
                HasAudioCD = false
            };

            try
            {
                Open();
                info.IsReady = IsCdPresent();

                if (info.IsReady)
                {
                    info.TrackCount = GetTrackCount();
                    info.HasAudioCD = info.TrackCount > 0;
                    info.MediaType = "Audio CD";
                }
            }
            catch
            {
                // Drive not ready or no CD
            }

            return info;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                Close();
                _disposed = true;
            }
        }
    }
}
