namespace nexENCODE_Studio.Models
{
    /// <summary>
    /// Represents CD metadata from online databases
    /// </summary>
    public class CdMetadata
    {
        public string DiscId { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public List<TrackMetadata> Tracks { get; set; } = new();
        public byte[]? CoverArt { get; set; }
        public string CoverArtUrl { get; set; } = string.Empty;
        public MetadataSource Source { get; set; }
        public int Confidence { get; set; } // 0-100
    }

    /// <summary>
    /// Track metadata from online databases
    /// </summary>
    public class TrackMetadata
    {
        public int TrackNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string ISRC { get; set; } = string.Empty;
    }

    /// <summary>
    /// Metadata source enumeration
    /// </summary>
    public enum MetadataSource
    {
        Unknown,
        MusicBrainz,
        Discogs,
        GD3,
        FreeDB,
        CDDB,
        Manual
    }

    /// <summary>
    /// Options for metadata lookup
    /// </summary>
    public class MetadataLookupOptions
    {
        public bool UseMusicBrainz { get; set; } = true;
        public bool UseDiscogs { get; set; } = false;
        public bool UseGD3 { get; set; } = false;
        public bool UseFreeDB { get; set; } = false;
        public bool DownloadCoverArt { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public string UserAgent { get; set; } = "nexENCODE Studio/1.0";
        public string? DiscogsToken { get; set; }
    }
}
