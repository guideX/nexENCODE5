using nexENCODE_Studio.Models;
using System.Text;

namespace nexENCODE_Studio.Utilities
{
    /// <summary>
    /// Helper methods for file operations
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Sanitizes a filename by removing invalid characters
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            
            foreach (char c in fileName)
            {
                if (!invalidChars.Contains(c))
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a safe file name from track metadata
        /// </summary>
        public static string GenerateFileName(AudioTrack track, AudioFormat format)
        {
            string baseName;
            
            if (!string.IsNullOrEmpty(track.Artist) && !string.IsNullOrEmpty(track.Title))
            {
                baseName = $"{track.TrackNumber:00} - {track.Artist} - {track.Title}";
            }
            else if (!string.IsNullOrEmpty(track.Title))
            {
                baseName = $"{track.TrackNumber:00} - {track.Title}";
            }
            else
            {
                baseName = $"Track {track.TrackNumber:00}";
            }
            
            string extension = format switch
            {
                AudioFormat.Mp3 => ".mp3",
                AudioFormat.Wav => ".wav",
                AudioFormat.Ogg => ".ogg",
                AudioFormat.Flac => ".flac",
                AudioFormat.Alac => ".m4a",
                _ => ".mp3"
            };
            
            return SanitizeFileName(baseName) + extension;
        }
        
        /// <summary>
        /// Generates a directory name from album info
        /// </summary>
        public static string GenerateAlbumDirectory(CdInfo cdInfo)
        {
            string dirName;
            
            if (!string.IsNullOrEmpty(cdInfo.Artist) && !string.IsNullOrEmpty(cdInfo.Album))
            {
                dirName = $"{cdInfo.Artist} - {cdInfo.Album}";
            }
            else if (!string.IsNullOrEmpty(cdInfo.Album))
            {
                dirName = cdInfo.Album;
            }
            else
            {
                dirName = $"CD_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            
            if (cdInfo.Year > 0)
            {
                dirName = $"{dirName} ({cdInfo.Year})";
            }
            
            return SanitizeFileName(dirName);
        }
        
        /// <summary>
        /// Gets a unique file name if the file already exists
        /// </summary>
        public static string GetUniqueFileName(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;
            
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            
            int counter = 1;
            string newFilePath;
            
            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
                counter++;
            }
            while (File.Exists(newFilePath));
            
            return newFilePath;
        }
        
        /// <summary>
        /// Formats file size to human-readable string
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        
        /// <summary>
        /// Ensures a directory exists, creates it if not
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
    
    /// <summary>
    /// Helper methods for audio operations
    /// </summary>
    public static class AudioHelper
    {
        /// <summary>
        /// Formats time span to MM:SS format
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            return $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
        }
        
        /// <summary>
        /// Formats time span to HH:MM:SS format
        /// </summary>
        public static string FormatDurationLong(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}";
            else
                return FormatDuration(duration);
        }
        
        /// <summary>
        /// Calculates estimated file size for MP3 encoding
        /// </summary>
        public static long EstimateMp3FileSize(TimeSpan duration, Mp3Quality quality)
        {
            int bitrate = (int)quality;
            long bitsPerSecond = bitrate * 1000;
            long bytesPerSecond = bitsPerSecond / 8;
            return (long)(bytesPerSecond * duration.TotalSeconds);
        }
        
        /// <summary>
        /// Calculates estimated file size for WAV
        /// </summary>
        public static long EstimateWavFileSize(TimeSpan duration, int sampleRate = 44100, int channels = 2, int bitsPerSample = 16)
        {
            long bytesPerSecond = sampleRate * channels * (bitsPerSample / 8);
            long dataSize = (long)(bytesPerSecond * duration.TotalSeconds);
            return dataSize + 44; // Add WAV header size
        }
        
        /// <summary>
        /// Determines if a file is a supported audio format
        /// </summary>
        public static bool IsSupportedAudioFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a" or ".wma" or ".aac" => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Gets the audio format from file extension
        /// </summary>
        public static AudioFormat? GetFormatFromExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".mp3" => AudioFormat.Mp3,
                ".wav" => AudioFormat.Wav,
                ".ogg" => AudioFormat.Ogg,
                ".flac" => AudioFormat.Flac,
                ".m4a" => AudioFormat.Alac,
                _ => null
            };
        }
    }
    
    /// <summary>
    /// Helper methods for CD operations
    /// </summary>
    public static class CdHelper
    {
        /// <summary>
        /// Gets all available CD drives
        /// </summary>
        public static List<DriveInfo> GetCdDrives()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.CDRom)
                .ToList();
        }
        
        /// <summary>
        /// Checks if a CD is present in the drive
        /// </summary>
        public static bool IsCdPresent(char driveLetter)
        {
            try
            {
                var drive = new DriveInfo(driveLetter.ToString());
                return drive.IsReady && drive.DriveType == DriveType.CDRom;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the disc ID for CDDB lookup (placeholder)
        /// </summary>
        public static string CalculateDiscId(CdInfo cdInfo)
        {
            // In a real implementation, this would calculate the actual CDDB disc ID
            // based on track offsets and lengths
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
