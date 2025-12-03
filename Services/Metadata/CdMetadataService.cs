using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services.Native;

namespace nexENCODE_Studio.Services.Metadata
{
    /// <summary>
    /// Base interface for CD metadata providers
    /// </summary>
    public interface ICdMetadataProvider
    {
        MetadataSource Source { get; }
        Task<CdMetadata?> LookupAsync(string discId, CdInfo cdInfo, CancellationToken cancellationToken = default);
        Task<byte[]?> DownloadCoverArtAsync(string url, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result from metadata lookup with multiple sources
    /// </summary>
    public class MetadataLookupResult
    {
        public bool Success { get; set; }
        public CdMetadata? BestMatch { get; set; }
        public List<CdMetadata> AllMatches { get; set; } = new();
        public string? Error { get; set; }
        public TimeSpan LookupTime { get; set; }
    }

    /// <summary>
    /// Main service for CD metadata lookup across multiple providers
    /// </summary>
    public class CdMetadataService
    {
        private readonly List<ICdMetadataProvider> _providers = new();
        private readonly MetadataLookupOptions _options;

        public event EventHandler<string>? StatusChanged;

        public CdMetadataService(MetadataLookupOptions? options = null)
        {
            _options = options ?? new MetadataLookupOptions();
            InitializeProviders();
        }

        private void InitializeProviders()
        {
            if (_options.UseMusicBrainz)
                _providers.Add(new MusicBrainzProvider(_options));

            if (_options.UseDiscogs && !string.IsNullOrEmpty(_options.DiscogsToken))
                _providers.Add(new DiscogsProvider(_options));

            if (_options.UseGD3)
                _providers.Add(new GD3Provider(_options));

            if (_options.UseFreeDB)
                _providers.Add(new FreeDBProvider(_options));
        }

        /// <summary>
        /// Looks up CD metadata from all enabled providers
        /// </summary>
        public async Task<MetadataLookupResult> LookupCdMetadataAsync(CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            var result = new MetadataLookupResult();
            var startTime = DateTime.Now;

            try
            {
                OnStatusChanged("Starting metadata lookup...");

                // Calculate proper disc ID if not already set
                if (string.IsNullOrEmpty(cdInfo.DiscId))
                {
                    cdInfo.DiscId = CalculateDiscId(cdInfo);
                }

                OnStatusChanged($"Disc ID: {cdInfo.DiscId}");

                // Query all providers in parallel
                var lookupTasks = _providers.Select(async provider =>
                {
                    try
                    {
                        OnStatusChanged($"Querying {provider.Source}...");
                        var metadata = await provider.LookupAsync(cdInfo.DiscId, cdInfo, cancellationToken);
                        
                        if (metadata != null)
                        {
                            OnStatusChanged($"Found match on {provider.Source}");
                            return metadata;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged($"{provider.Source} error: {ex.Message}");
                    }
                    return null;
                }).ToList();

                var results = await Task.WhenAll(lookupTasks);
                result.AllMatches = results.Where(r => r != null).Cast<CdMetadata>().ToList();

                // Select best match based on confidence and completeness
                if (result.AllMatches.Any())
                {
                    result.BestMatch = SelectBestMatch(result.AllMatches);
                    result.Success = true;

                    // Apply metadata to CdInfo
                    ApplyMetadataToCdInfo(cdInfo, result.BestMatch);

                    OnStatusChanged($"Metadata found: {result.BestMatch.Artist} - {result.BestMatch.Album}");

                    // Download cover art if enabled
                    if (_options.DownloadCoverArt && !string.IsNullOrEmpty(result.BestMatch.CoverArtUrl))
                    {
                        OnStatusChanged("Downloading cover art...");
                        var provider = _providers.FirstOrDefault(p => p.Source == result.BestMatch.Source);
                        if (provider != null)
                        {
                            result.BestMatch.CoverArt = await provider.DownloadCoverArtAsync(
                                result.BestMatch.CoverArtUrl, 
                                cancellationToken
                            );
                        }
                    }
                }
                else
                {
                    result.Success = false;
                    result.Error = "No metadata found in any database";
                    OnStatusChanged("No metadata found");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                OnStatusChanged($"Lookup failed: {ex.Message}");
            }

            result.LookupTime = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Selects the best metadata match based on confidence and completeness
        /// </summary>
        private CdMetadata SelectBestMatch(List<CdMetadata> matches)
        {
            return matches
                .OrderByDescending(m => m.Confidence)
                .ThenByDescending(m => m.Tracks.Count(t => !string.IsNullOrEmpty(t.Title)))
                .ThenByDescending(m => !string.IsNullOrEmpty(m.CoverArtUrl))
                .First();
        }

        /// <summary>
        /// Applies metadata to CdInfo object
        /// </summary>
        private void ApplyMetadataToCdInfo(CdInfo cdInfo, CdMetadata metadata)
        {
            cdInfo.Artist = metadata.Artist;
            cdInfo.Album = metadata.Album;
            cdInfo.Year = metadata.Year;
            cdInfo.Genre = metadata.Genre;

            // Apply track metadata
            for (int i = 0; i < Math.Min(cdInfo.Tracks.Count, metadata.Tracks.Count); i++)
            {
                var track = cdInfo.Tracks[i];
                var trackMeta = metadata.Tracks[i];

                track.Title = trackMeta.Title;
                track.Artist = string.IsNullOrEmpty(trackMeta.Artist) ? metadata.Artist : trackMeta.Artist;
                track.Album = metadata.Album;
                track.Year = metadata.Year;
                track.Genre = metadata.Genre;
            }
        }

        /// <summary>
        /// Calculates accurate disc ID for CDDB/MusicBrainz
        /// </summary>
        private string CalculateDiscId(CdInfo cdInfo)
        {
            // CDDB disc ID algorithm
            int n = 0;
            foreach (var track in cdInfo.Tracks)
            {
                int seconds = (int)track.Duration.TotalSeconds;
                while (seconds > 0)
                {
                    n += seconds % 10;
                    seconds /= 10;
                }
            }

            int totalSeconds = (int)cdInfo.Tracks.Sum(t => t.Duration.TotalSeconds);
            int trackCount = cdInfo.Tracks.Count;

            uint discId = (uint)(((n % 0xff) << 24) | (totalSeconds << 8) | trackCount);
            return discId.ToString("x8");
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }
    }
}
