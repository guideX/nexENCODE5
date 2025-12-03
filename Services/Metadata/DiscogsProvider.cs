using System.Text.Json;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services.Metadata
{
    /// <summary>
    /// Discogs metadata provider
    /// Requires API token from https://www.discogs.com/settings/developers
    /// </summary>
    public class DiscogsProvider : ICdMetadataProvider
    {
        private readonly MetadataLookupOptions _options;
        private const string DISCOGS_API = "https://api.discogs.com";

        public MetadataSource Source => MetadataSource.Discogs;

        public DiscogsProvider(MetadataLookupOptions options)
        {
            _options = options;
        }

        public async Task<CdMetadata?> LookupAsync(string discId, CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_options.DiscogsToken))
                return null;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
                client.DefaultRequestHeaders.Add("Authorization", $"Discogs token={_options.DiscogsToken}");

                // Search by artist and album if we have some metadata
                var searchQuery = BuildSearchQuery(cdInfo);
                var searchUrl = $"{DISCOGS_API}/database/search?q={Uri.EscapeDataString(searchQuery)}&type=release&format=CD";

                var response = await client.GetStringAsync(searchUrl, cancellationToken);
                var searchResult = JsonSerializer.Deserialize<DiscogsSearchResult>(response);

                if (searchResult?.results == null || searchResult.results.Length == 0)
                    return null;

                // Get details for the first result
                var release = searchResult.results[0];
                var detailsUrl = release.resource_url;
                var detailsResponse = await client.GetStringAsync(detailsUrl, cancellationToken);
                var details = JsonSerializer.Deserialize<DiscogsRelease>(detailsResponse);

                if (details == null)
                    return null;

                var metadata = new CdMetadata
                {
                    DiscId = discId,
                    Artist = details.artists?[0].name ?? "Unknown Artist",
                    Album = details.title ?? "Unknown Album",
                    Year = details.year,
                    Label = details.labels?[0].name ?? "",
                    Country = details.country ?? "",
                    Barcode = details.identifiers?.FirstOrDefault(i => i.type == "Barcode")?.value ?? "",
                    Source = MetadataSource.Discogs,
                    Confidence = 85
                };

                // Get genre
                if (details.genres != null && details.genres.Length > 0)
                {
                    metadata.Genre = details.genres[0];
                }
                else if (details.styles != null && details.styles.Length > 0)
                {
                    metadata.Genre = details.styles[0];
                }

                // Get cover art
                if (details.images != null && details.images.Length > 0)
                {
                    metadata.CoverArtUrl = details.images[0].uri;
                }

                // Get track information
                if (details.tracklist != null)
                {
                    int trackNum = 1;
                    foreach (var track in details.tracklist)
                    {
                        if (track.type_ == "track")
                        {
                            var trackMeta = new TrackMetadata
                            {
                                TrackNumber = trackNum++,
                                Title = track.title ?? $"Track {trackNum}",
                                Artist = track.artists?[0].name ?? metadata.Artist,
                                Duration = ParseDuration(track.duration)
                            };

                            metadata.Tracks.Add(trackMeta);
                        }
                    }
                }

                return metadata;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> DownloadCoverArtAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);

                var response = await client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }
            }
            catch
            {
                // Ignore download errors
            }

            return null;
        }

        private string BuildSearchQuery(CdInfo cdInfo)
        {
            // If we have some metadata already, use it
            if (!string.IsNullOrEmpty(cdInfo.Artist) && !string.IsNullOrEmpty(cdInfo.Album))
            {
                return $"{cdInfo.Artist} {cdInfo.Album}";
            }

            // Otherwise search by track count and duration
            return $"tracks:{cdInfo.TotalTracks}";
        }

        private TimeSpan ParseDuration(string? duration)
        {
            if (string.IsNullOrEmpty(duration))
                return TimeSpan.Zero;

            // Format: "4:23" or "4:23:12"
            var parts = duration.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
                {
                    return TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));
                }
            }

            return TimeSpan.Zero;
        }

        #region Discogs API Models

        private class DiscogsSearchResult
        {
            public DiscogsSearchItem[]? results { get; set; }
        }

        private class DiscogsSearchItem
        {
            public string? resource_url { get; set; }
            public string? title { get; set; }
        }

        private class DiscogsRelease
        {
            public string? title { get; set; }
            public int year { get; set; }
            public string? country { get; set; }
            public DiscogsArtist[]? artists { get; set; }
            public DiscogsLabel[]? labels { get; set; }
            public string[]? genres { get; set; }
            public string[]? styles { get; set; }
            public DiscogsTrack[]? tracklist { get; set; }
            public DiscogsImage[]? images { get; set; }
            public DiscogsIdentifier[]? identifiers { get; set; }
        }

        private class DiscogsArtist
        {
            public string? name { get; set; }
        }

        private class DiscogsLabel
        {
            public string? name { get; set; }
        }

        private class DiscogsTrack
        {
            public string? type_ { get; set; }
            public string? title { get; set; }
            public string? duration { get; set; }
            public DiscogsArtist[]? artists { get; set; }
        }

        private class DiscogsImage
        {
            public string? uri { get; set; }
        }

        private class DiscogsIdentifier
        {
            public string? type { get; set; }
            public string? value { get; set; }
        }

        #endregion
    }
}
