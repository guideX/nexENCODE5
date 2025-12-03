namespace nexENCODE_Studio.Models
{
    /// <summary>
    /// Represents CD album information
    /// </summary>
    public class CdInfo
    {
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public string DiscId { get; set; } = string.Empty;
        public List<AudioTrack> Tracks { get; set; } = new();
        public int TotalTracks => Tracks.Count;
        
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(
            Tracks.Sum(t => t.Duration.TotalSeconds));
    }
}
