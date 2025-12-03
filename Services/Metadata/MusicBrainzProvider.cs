using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services.Metadata
{
    /// <summary>
    /// MusicBrainz metadata provider
    /// </summary>
    public class MusicBrainzProvider : ICdMetadataProvider
    {
        private readonly MetadataLookupOptions _options;
        private readonly HttpClient _httpClient;

        public MetadataSource Source => MetadataSource.MusicBrainz;

        public MusicBrainzProvider(MetadataLookupOptions options)
        {
            _options = options;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }

        public async Task<CdMetadata?> LookupAsync(string discId, CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new Query(_httpClient);
                
                // Try to search for releases by disc ID or TOC
                var searchQuery = BuildSearchQuery(cdInfo);
                var searchResults = await query.FindReleasesAsync(searchQuery, limit: 1);

                if (searchResults?.Results == null || !searchResults.Results.Any())
                    return null;

                var release = searchResults.Results.First().Item;

                var metadata = new CdMetadata
                {
                    DiscId = discId,
                    Artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? "Unknown Artist",
                    Album = release.Title ?? "Unknown Album",
                    Year = release.Date?.Year ?? 0,
                    Country = release.Country ?? "",
                    Barcode = release.Barcode ?? "",
                    Source = MetadataSource.MusicBrainz,
                    Confidence = 90 // MusicBrainz is highly reliable
                };

                // Get genre from release group if available
                if (release.ReleaseGroup?.PrimaryType != null)
                {
                    metadata.Genre = release.ReleaseGroup.PrimaryType.ToString();
                }

                // Get cover art URL (release.Id is Guid, not nullable)
                if (release.Id != Guid.Empty)
                {
                    metadata.CoverArtUrl = $"https://coverartarchive.org/release/{release.Id}/front";
                }

                // Get track information from the first medium
                if (release.Media != null && release.Media.Any())
                {
                    var medium = release.Media.First();
                    if (medium.Tracks != null)
                    {
                        foreach (var track in medium.Tracks)
                        {
                            // track.Length is TimeSpan?, not long?
                            var duration = track.Length ?? TimeSpan.Zero;
                            
                            var trackMeta = new TrackMetadata
                            {
                                TrackNumber = track.Position ?? 0,
                                Title = track.Title ?? $"Track {track.Position}",
                                Artist = track.ArtistCredit?.FirstOrDefault()?.Name ?? metadata.Artist,
                                Duration = duration
                            };

                            metadata.Tracks.Add(trackMeta);
                        }
                    }
                }

                // If we didn't get tracks from the release, try to match by track count
                if (!metadata.Tracks.Any() && cdInfo.Tracks.Any())
                {
                    // Create basic track list with numbers
                    for (int i = 0; i < cdInfo.Tracks.Count; i++)
                    {
                        metadata.Tracks.Add(new TrackMetadata
                        {
                            TrackNumber = i + 1,
                            Title = $"Track {i + 1:00}",
                            Artist = metadata.Artist,
                            Duration = cdInfo.Tracks[i].Duration
                        });
                    }
                }

                return metadata;
            }
            catch (Exception)
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
                client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);

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

        /// <summary>
        /// Builds search query for MusicBrainz
        /// </summary>
        private string BuildSearchQuery(CdInfo cdInfo)
        {
            // Search by track count and total duration
            var trackCount = cdInfo.TotalTracks;
            var duration = (int)cdInfo.TotalDuration.TotalMinutes;

            // If we have artist/album info, use it
            if (!string.IsNullOrEmpty(cdInfo.Artist) && !string.IsNullOrEmpty(cdInfo.Album))
            {
                return $"artist:\"{cdInfo.Artist}\" AND release:\"{cdInfo.Album}\" AND tracks:{trackCount}";
            }

            // Otherwise, search by characteristics
            return $"tracks:{trackCount} AND dur:[{duration - 2} TO {duration + 2}]";
        }
    }
}
