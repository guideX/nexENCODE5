using System.Text;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services.Metadata
{
    /// <summary>
    /// FreeDB/GNUDB metadata provider (FreeDB replacement)
    /// </summary>
    public class FreeDBProvider : ICdMetadataProvider
    {
        private readonly MetadataLookupOptions _options;
        private const string GNUDB_SERVER = "http://gnudb.gnudb.org";

        public MetadataSource Source => MetadataSource.FreeDB;

        public FreeDBProvider(MetadataLookupOptions options)
        {
            _options = options;
        }

        public async Task<CdMetadata?> LookupAsync(string discId, CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                // Build CDDB query
                var query = BuildCddbQuery(discId, cdInfo);
                var url = $"{GNUDB_SERVER}/~cddb/cddb.cgi?{query}";

                var response = await client.GetStringAsync(url, cancellationToken);
                
                if (response.StartsWith("200") || response.StartsWith("210"))
                {
                    return ParseCddbResponse(response, discId);
                }
            }
            catch
            {
                // GNUDB might be unavailable, fail silently
            }

            return null;
        }

        public async Task<byte[]?> DownloadCoverArtAsync(string url, CancellationToken cancellationToken = default)
        {
            // FreeDB doesn't provide cover art
            return await Task.FromResult<byte[]?>(null);
        }

        /// <summary>
        /// Builds CDDB query string
        /// </summary>
        private string BuildCddbQuery(string discId, CdInfo cdInfo)
        {
            var sb = new StringBuilder();
            
            // Command
            sb.Append("cmd=cddb+query");
            
            // Disc ID
            sb.Append($"+{discId}");
            
            // Number of tracks
            sb.Append($"+{cdInfo.Tracks.Count}");
            
            // Frame offsets (simplified - using seconds * 75)
            int offset = 150; // 2 second lead-in
            foreach (var track in cdInfo.Tracks)
            {
                sb.Append($"+{offset}");
                offset += (int)(track.Duration.TotalSeconds * 75);
            }
            
            // Disc length in seconds
            int discLength = (int)cdInfo.TotalDuration.TotalSeconds;
            sb.Append($"+{discLength}");
            
            // Protocol version
            sb.Append("&hello=user+localhost+nexENCODE+1.0&proto=6");
            
            return sb.ToString();
        }

        /// <summary>
        /// Parses CDDB response
        /// </summary>
        private CdMetadata? ParseCddbResponse(string response, string discId)
        {
            var lines = response.Split('\n');
            if (lines.Length < 2)
                return null;

            var metadata = new CdMetadata
            {
                DiscId = discId,
                Source = MetadataSource.FreeDB,
                Confidence = 80
            };

            // Parse header line (format: "200 category discid Artist / Album")
            var headerParts = lines[0].Split(' ', 3);
            if (headerParts.Length >= 3)
            {
                var titleParts = headerParts[2].Split('/');
                if (titleParts.Length >= 2)
                {
                    metadata.Artist = titleParts[0].Trim();
                    metadata.Album = titleParts[1].Trim();
                }
            }

            // Parse subsequent lines for track information
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                if (line.StartsWith("TTITLE"))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var trackNumStr = parts[0].Replace("TTITLE", "");
                        if (int.TryParse(trackNumStr, out int trackNum))
                        {
                            var title = parts[1].Trim();
                            
                            // Split artist / title if present
                            var titleParts = title.Split('/');
                            var trackMeta = new TrackMetadata
                            {
                                TrackNumber = trackNum + 1,
                                Title = titleParts.Length > 1 ? titleParts[1].Trim() : title,
                                Artist = titleParts.Length > 1 ? titleParts[0].Trim() : metadata.Artist
                            };
                            
                            metadata.Tracks.Add(trackMeta);
                        }
                    }
                }
                else if (line.StartsWith("DYEAR="))
                {
                    var yearStr = line.Replace("DYEAR=", "").Trim();
                    if (int.TryParse(yearStr, out int year))
                    {
                        metadata.Year = year;
                    }
                }
                else if (line.StartsWith("DGENRE="))
                {
                    metadata.Genre = line.Replace("DGENRE=", "").Trim();
                }
            }

            return metadata.Tracks.Any() ? metadata : null;
        }
    }
}
