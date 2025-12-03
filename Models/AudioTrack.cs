namespace nexENCODE_Studio.Models
{
    /// <summary>
    /// Represents an audio track with metadata
    /// </summary>
    public class AudioTrack
    {
        public int TrackNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public TimeSpan Duration { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        
        /// <summary>
        /// Gets the track as a formatted string
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Title))
                return $"{TrackNumber:00}. {Artist} - {Title}";
            return $"Track {TrackNumber:00}";
        }
    }
}
