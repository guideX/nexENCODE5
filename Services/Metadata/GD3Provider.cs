using System.Text;
using System.Xml.Linq;
using nexENCODE_Studio.Models;

namespace nexENCODE_Studio.Services.Metadata
{
    /// <summary>
    /// GD3 (Gracenote Database 3) metadata provider
    /// Note: Requires GD3/Gracenote API credentials (commercial service)
    /// This is a basic implementation showing the structure
    /// </summary>
    public class GD3Provider : ICdMetadataProvider
    {
        private readonly MetadataLookupOptions _options;
        private const string GD3_ENDPOINT = "https://c[clientId].web.cddbp.net/webapi/xml/1.0/";
        
        // These would need to be obtained from Gracenote
        private string? _clientId;
        private string? _clientTag;
        private string? _userId;

        public MetadataSource Source => MetadataSource.GD3;

        public GD3Provider(MetadataLookupOptions options)
        {
            _options = options;
        }

        public async Task<CdMetadata?> LookupAsync(string discId, CdInfo cdInfo, CancellationToken cancellationToken = default)
        {
            // GD3/Gracenote requires commercial licensing
            // This is a placeholder showing the expected structure
            
            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_userId))
            {
                // No credentials configured
                return null;
            }

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                // Build GD3 XML query
                var query = BuildGd3Query(discId, cdInfo);
                var content = new StringContent(query, Encoding.UTF8, "application/xml");

                var endpoint = GD3_ENDPOINT.Replace("[clientId]", _clientId);
                var response = await client.PostAsync(endpoint, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync(cancellationToken);
                    return ParseGd3Response(xml, discId);
                }
            }
            catch
            {
                // Service unavailable or authentication failed
            }

            return null;
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
                // Ignore errors
            }

            return null;
        }

        /// <summary>
        /// Builds GD3 XML query
        /// </summary>
        private string BuildGd3Query(string discId, CdInfo cdInfo)
        {
            var xml = new XDocument(
                new XElement("QUERIES",
                    new XElement("AUTH",
                        new XElement("CLIENT", _clientId),
                        new XElement("USER", _userId)
                    ),
                    new XElement("QUERY",
                        new XAttribute("CMD", "ALBUM_SEARCH"),
                        new XElement("MODE", "SINGLE_BEST_COVER"),
                        new XElement("TEXT",
                            new XAttribute("TYPE", "TOC"),
                            BuildTocString(cdInfo)
                        )
                    )
                )
            );

            return xml.ToString();
        }

        /// <summary>
        /// Parses GD3 XML response
        /// </summary>
        private CdMetadata? ParseGd3Response(string xml, string discId)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var album = doc.Descendants("ALBUM").FirstOrDefault();

                if (album == null)
                    return null;

                var metadata = new CdMetadata
                {
                    DiscId = discId,
                    Artist = album.Element("ARTIST")?.Value ?? "Unknown Artist",
                    Album = album.Element("TITLE")?.Value ?? "Unknown Album",
                    Year = int.TryParse(album.Element("DATE")?.Value, out var year) ? year : 0,
                    Genre = album.Element("GENRE")?.Value ?? "",
                    Label = album.Element("LABEL")?.Value ?? "",
                    Source = MetadataSource.GD3,
                    Confidence = 90 // GD3/Gracenote is very reliable
                };

                // Get cover art URL
                var coverUrl = album.Element("URL")?.Attribute("TYPE")?.Value;
                if (coverUrl == "COVERART")
                {
                    metadata.CoverArtUrl = album.Element("URL")?.Value ?? "";
                }

                // Get tracks
                var tracks = album.Descendants("TRACK");
                int trackNum = 1;
                foreach (var track in tracks)
                {
                    var trackMeta = new TrackMetadata
                    {
                        TrackNumber = trackNum++,
                        Title = track.Element("TITLE")?.Value ?? $"Track {trackNum}",
                        Artist = track.Element("ARTIST")?.Value ?? metadata.Artist
                    };

                    metadata.Tracks.Add(trackMeta);
                }

                return metadata;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds TOC string for GD3 query
        /// </summary>
        private string BuildTocString(CdInfo cdInfo)
        {
            var sb = new StringBuilder();
            
            // Format: number of tracks + offsets
            sb.Append(cdInfo.Tracks.Count);
            
            int offset = 150; // Lead-in
            foreach (var track in cdInfo.Tracks)
            {
                sb.Append($" {offset}");
                offset += (int)(track.Duration.TotalSeconds * 75);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Configures GD3/Gracenote credentials
        /// </summary>
        public void ConfigureCredentials(string clientId, string clientTag, string userId)
        {
            _clientId = clientId;
            _clientTag = clientTag;
            _userId = userId;
        }
    }
}
