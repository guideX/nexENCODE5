namespace nexENCODE_Studio.Models
{
    /// <summary>
    /// Progress information for encoding/ripping operations
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public int PercentComplete { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public AudioTrack? CurrentTrack { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public Exception? Error { get; set; }
    }
}
