using System.Text;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services
{
    /// <summary>
    /// Service for reading and writing M3U playlist files
    /// </summary>
    public class PlaylistService
    {
        /// <summary>
        /// Reads an M3U playlist file
        /// </summary>
        public List<AudioTrack> ReadM3uPlaylist(string filePath)
        {
            var tracks = new List<AudioTrack>();
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Playlist file not found", filePath);
            
            string[] lines = File.ReadAllLines(filePath);
            AudioTrack? currentTrack = null;
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                
                // Extended M3U header
                if (trimmedLine.Equals("#EXTM3U", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                // Extended info line: #EXTINF:duration,artist - title
                if (trimmedLine.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                {
                    currentTrack = ParseExtInfLine(trimmedLine);
                }
                // Comment line
                else if (trimmedLine.StartsWith("#"))
                {
                    continue;
                }
                // File path
                else
                {
                    if (currentTrack == null)
                        currentTrack = new AudioTrack();
                    
                    currentTrack.FilePath = trimmedLine;
                    
                    // If the path is relative, make it absolute relative to the playlist directory
                    if (!Path.IsPathRooted(trimmedLine))
                    {
                        string playlistDir = Path.GetDirectoryName(filePath) ?? "";
                        currentTrack.FilePath = Path.GetFullPath(Path.Combine(playlistDir, trimmedLine));
                    }
                    
                    tracks.Add(currentTrack);
                    currentTrack = null;
                }
            }
            
            return tracks;
        }
        
        /// <summary>
        /// Writes tracks to an M3U playlist file
        /// </summary>
        public void WriteM3uPlaylist(string filePath, List<AudioTrack> tracks, bool extended = true)
        {
            var sb = new StringBuilder();
            
            if (extended)
                sb.AppendLine("#EXTM3U");
            
            string playlistDir = Path.GetDirectoryName(filePath) ?? "";
            
            foreach (var track in tracks)
            {
                if (extended)
                {
                    // #EXTINF:duration,artist - title
                    int durationSeconds = (int)track.Duration.TotalSeconds;
                    string info = $"{track.Artist} - {track.Title}";
                    if (string.IsNullOrEmpty(info.Trim('-', ' ')))
                        info = Path.GetFileNameWithoutExtension(track.FilePath);
                    
                    sb.AppendLine($"#EXTINF:{durationSeconds},{info}");
                }
                
                // Try to make path relative to playlist location
                string trackPath = track.FilePath;
                if (Path.IsPathRooted(trackPath) && trackPath.StartsWith(playlistDir, StringComparison.OrdinalIgnoreCase))
                {
                    trackPath = Path.GetRelativePath(playlistDir, trackPath);
                }
                
                sb.AppendLine(trackPath);
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }
        
        /// <summary>
        /// Updates ID3 tags for all files in a playlist
        /// </summary>
        public void UpdatePlaylistTags(string playlistPath, List<AudioTrack> updatedTracks)
        {
            var encoder = new AudioEncoderService();
            
            foreach (var track in updatedTracks)
            {
                if (File.Exists(track.FilePath) && 
                    Path.GetExtension(track.FilePath).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var file = TagLib.File.Create(track.FilePath);
                        file.Tag.Title = track.Title;
                        file.Tag.Performers = new[] { track.Artist };
                        file.Tag.Album = track.Album;
                        file.Tag.Track = (uint)track.TrackNumber;
                        if (track.Year > 0)
                            file.Tag.Year = (uint)track.Year;
                        if (!string.IsNullOrEmpty(track.Genre))
                            file.Tag.Genres = new[] { track.Genre };
                        file.Save();
                    }
                    catch (Exception)
                    {
                        // Continue with other tracks if one fails
                    }
                }
            }
            
            // Update the playlist file with new information
            WriteM3uPlaylist(playlistPath, updatedTracks);
        }
        
        private AudioTrack ParseExtInfLine(string line)
        {
            var track = new AudioTrack();
            
            // Format: #EXTINF:123,Artist - Title
            string content = line.Substring(8); // Remove "#EXTINF:"
            
            int commaIndex = content.IndexOf(',');
            if (commaIndex > 0)
            {
                // Parse duration
                if (int.TryParse(content.Substring(0, commaIndex), out int seconds))
                {
                    track.Duration = TimeSpan.FromSeconds(seconds);
                }
                
                // Parse artist and title
                string info = content.Substring(commaIndex + 1).Trim();
                int dashIndex = info.IndexOf(" - ");
                if (dashIndex > 0)
                {
                    track.Artist = info.Substring(0, dashIndex).Trim();
                    track.Title = info.Substring(dashIndex + 3).Trim();
                }
                else
                {
                    track.Title = info;
                }
            }
            
            return track;
        }
    }
}
