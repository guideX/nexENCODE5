using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services;
using nexENCODE_Studio.Services.Metadata;

namespace nexENCODE_Studio.Examples
{
    /// <summary>
    /// Examples for automatic CD metadata lookup
    /// </summary>
    public static class MetadataLookupExamples
    {
        /// <summary>
        /// Example 1: Automatic metadata lookup when reading CD
        /// </summary>
        public static void AutomaticMetadataLookupExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D',
                AutoLookupMetadata = true // Enabled by default
            };

            Console.WriteLine("=== Automatic Metadata Lookup ===\n");

            // Progress tracking
            ripper.ProgressChanged += (s, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
            };

            try
            {
                // Reading CD will automatically lookup metadata
                var cdInfo = ripper.ReadCdInfo();

                Console.WriteLine($"\n? CD Information:");
                Console.WriteLine($"Artist: {cdInfo.Artist}");
                Console.WriteLine($"Album: {cdInfo.Album}");
                Console.WriteLine($"Year: {cdInfo.Year}");
                Console.WriteLine($"Genre: {cdInfo.Genre}");
                Console.WriteLine($"Tracks: {cdInfo.TotalTracks}");
                Console.WriteLine();

                foreach (var track in cdInfo.Tracks)
                {
                    Console.WriteLine($"  {track.TrackNumber:00}. {track.Artist} - {track.Title} ({track.Duration:mm\\:ss})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 2: Manual metadata lookup with options
        /// </summary>
        public static async Task ManualMetadataLookupExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D',
                AutoLookupMetadata = false // Disable automatic lookup
            };

            // Configure metadata options
            ripper.MetadataOptions = new MetadataLookupOptions
            {
                UseMusicBrainz = true,
                UseFreeDB = true,
                UseDiscogs = false, // Requires API token
                UseGD3 = false, // Requires commercial license
                DownloadCoverArt = true,
                TimeoutSeconds = 30
            };

            Console.WriteLine("=== Manual Metadata Lookup ===\n");

            try
            {
                // Read CD without metadata
                var cdInfo = ripper.ReadCdInfo(lookupMetadata: false);
                Console.WriteLine($"CD Read: {cdInfo.TotalTracks} tracks found");
                Console.WriteLine($"Disc ID: {cdInfo.DiscId}\n");

                // Manually lookup metadata
                Console.WriteLine("Looking up metadata...");
                var result = await ripper.LookupMetadataAsync(cdInfo);

                if (result != null && result.Success)
                {
                    Console.WriteLine($"\n? Metadata Found!");
                    Console.WriteLine($"Source: {result.BestMatch!.Source}");
                    Console.WriteLine($"Confidence: {result.BestMatch.Confidence}%");
                    Console.WriteLine($"Lookup Time: {result.LookupTime.TotalSeconds:F2}s");
                    Console.WriteLine($"Artist: {cdInfo.Artist}");
                    Console.WriteLine($"Album: {cdInfo.Album}");
                    Console.WriteLine($"Year: {cdInfo.Year}");

                    if (result.BestMatch.CoverArt != null)
                    {
                        Console.WriteLine($"Cover Art: Downloaded ({result.BestMatch.CoverArt.Length} bytes)");
                    }

                    Console.WriteLine($"\nAll Sources Found: {result.AllMatches.Count}");
                    foreach (var match in result.AllMatches)
                    {
                        Console.WriteLine($"  - {match.Source}: {match.Album} ({match.Confidence}%)");
                    }
                }
                else
                {
                    Console.WriteLine($"\n? No metadata found");
                    Console.WriteLine($"Error: {result?.Error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 3: Configure specific metadata providers
        /// </summary>
        public static async Task CustomProvidersExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D'
            };

            // Use only MusicBrainz
            ripper.MetadataOptions = new MetadataLookupOptions
            {
                UseMusicBrainz = true,
                UseFreeDB = false,
                UseDiscogs = false,
                UseGD3 = false,
                DownloadCoverArt = true,
                UserAgent = "nexENCODE Studio/1.0 (your@email.com)"
            };

            Console.WriteLine("=== Using Only MusicBrainz ===\n");

            var cdInfo = ripper.ReadCdInfo();
            Console.WriteLine($"Artist: {cdInfo.Artist}");
            Console.WriteLine($"Album: {cdInfo.Album}");
        }

        /// <summary>
        /// Example 4: Using Discogs (requires API token)
        /// </summary>
        public static async Task DiscogsLookupExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D'
            };

            // Configure Discogs
            // Get your token from: https://www.discogs.com/settings/developers
            ripper.MetadataOptions = new MetadataLookupOptions
            {
                UseMusicBrainz = false,
                UseDiscogs = true,
                DiscogsToken = "YOUR_DISCOGS_TOKEN_HERE", // Replace with actual token
                DownloadCoverArt = true
            };

            Console.WriteLine("=== Using Discogs ===\n");

            var cdInfo = ripper.ReadCdInfo();
            
            if (!string.IsNullOrEmpty(cdInfo.Artist))
            {
                Console.WriteLine($"Artist: {cdInfo.Artist}");
                Console.WriteLine($"Album: {cdInfo.Album}");
                Console.WriteLine($"Label: {cdInfo.Tracks.FirstOrDefault()?.Album}");
            }
            else
            {
                Console.WriteLine("No metadata found. Check your Discogs token.");
            }
        }

        /// <summary>
        /// Example 5: Manual metadata entry when auto-lookup fails
        /// </summary>
        public static void ManualMetadataEntryExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D'
            };

            Console.WriteLine("=== Manual Metadata Entry ===\n");

            var cdInfo = ripper.ReadCdInfo();

            if (string.IsNullOrEmpty(cdInfo.Artist))
            {
                Console.WriteLine("Automatic lookup failed. Entering metadata manually...\n");

                // Manually set metadata
                var trackNames = new List<string>
                {
                    "Come Together",
                    "Something",
                    "Maxwell's Silver Hammer",
                    "Oh! Darling",
                    "Octopus's Garden",
                    "I Want You (She's So Heavy)",
                    "Here Comes the Sun",
                    "Because"
                };

                ripper.SetManualMetadata(
                    cdInfo,
                    artist: "The Beatles",
                    album: "Abbey Road",
                    year: 1969,
                    genre: "Rock",
                    trackNames: trackNames
                );

                Console.WriteLine($"? Metadata set manually:");
                Console.WriteLine($"Artist: {cdInfo.Artist}");
                Console.WriteLine($"Album: {cdInfo.Album}");
                Console.WriteLine();

                foreach (var track in cdInfo.Tracks)
                {
                    Console.WriteLine($"  {track.TrackNumber:00}. {track.Title}");
                }
            }
        }

        /// <summary>
        /// Example 6: Compare metadata from multiple sources
        /// </summary>
        public static async Task CompareMetadataSourcesExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D'
            };

            // Enable all available sources
            ripper.MetadataOptions = new MetadataLookupOptions
            {
                UseMusicBrainz = true,
                UseFreeDB = true,
                UseDiscogs = false, // Needs token
                UseGD3 = false, // Needs license
                DownloadCoverArt = false
            };

            Console.WriteLine("=== Comparing Metadata Sources ===\n");

            var cdInfo = ripper.ReadCdInfo(lookupMetadata: false);
            var result = await ripper.LookupMetadataAsync(cdInfo);

            if (result != null && result.AllMatches.Any())
            {
                Console.WriteLine($"Found {result.AllMatches.Count} metadata sources:\n");

                foreach (var match in result.AllMatches.OrderByDescending(m => m.Confidence))
                {
                    Console.WriteLine($"--- {match.Source} (Confidence: {match.Confidence}%) ---");
                    Console.WriteLine($"Artist: {match.Artist}");
                    Console.WriteLine($"Album: {match.Album}");
                    Console.WriteLine($"Year: {match.Year}");
                    Console.WriteLine($"Genre: {match.Genre}");
                    Console.WriteLine($"Tracks: {match.Tracks.Count}");
                    Console.WriteLine();
                }

                Console.WriteLine($"\n? Best Match Selected: {result.BestMatch!.Source}");
            }
        }

        /// <summary>
        /// Example 7: Rip CD with automatic metadata
        /// </summary>
        public static async Task RipWithMetadataExample()
        {
            var service = new NexEncodeService();
            service.CdRipper.CdDriveLetter = 'D';
            service.CdRipper.AutoLookupMetadata = true;

            // Progress tracking
            service.ProgressChanged += (s, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
            };

            Console.WriteLine("=== Rip CD with Auto-Metadata ===\n");

            try
            {
                // Read CD (metadata will be looked up automatically)
                var cdInfo = service.GetCdInfo();

                if (string.IsNullOrEmpty(cdInfo.Artist))
                {
                    Console.WriteLine("No metadata found. Please enter manually:");
                    Console.Write("Artist: ");
                    cdInfo.Artist = Console.ReadLine() ?? "";
                    Console.Write("Album: ");
                    cdInfo.Album = Console.ReadLine() ?? "";
                }

                Console.WriteLine($"\nRipping: {cdInfo.Artist} - {cdInfo.Album}");

                // Configure encoding
                var options = new EncodingOptions
                {
                    Quality = Mp3Quality.High,
                    WriteId3Tags = true
                };

                // Rip and encode with metadata
                string outputDir = Path.Combine(
                    @"C:\Music",
                    $"{cdInfo.Artist} - {cdInfo.Album}"
                );

                var mp3Files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);

                Console.WriteLine($"\n? Complete! {mp3Files.Count} tracks with metadata");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example 8: Save and load cover art
        /// </summary>
        public static async Task CoverArtExample()
        {
            var ripper = new CdRipperService
            {
                CdDriveLetter = 'D'
            };

            ripper.MetadataOptions.DownloadCoverArt = true;

            Console.WriteLine("=== Cover Art Download ===\n");

            var cdInfo = ripper.ReadCdInfo();
            var result = await ripper.LookupMetadataAsync(cdInfo);

            if (result?.Success == true && result.BestMatch?.CoverArt != null)
            {
                // Save cover art to file
                string albumDir = Path.Combine(@"C:\Music", $"{cdInfo.Artist} - {cdInfo.Album}");
                Directory.CreateDirectory(albumDir);

                string coverPath = Path.Combine(albumDir, "cover.jpg");
                await File.WriteAllBytesAsync(coverPath, result.BestMatch.CoverArt);

                Console.WriteLine($"? Cover art saved: {coverPath}");
                Console.WriteLine($"Size: {result.BestMatch.CoverArt.Length / 1024} KB");
            }
            else
            {
                Console.WriteLine("? No cover art found");
            }
        }

        /// <summary>
        /// Example 9: Batch CD ripping with auto-metadata
        /// </summary>
        public static async Task BatchRipWithMetadataExample()
        {
            var service = new NexEncodeService();
            service.CdRipper.CdDriveLetter = 'D';
            service.CdRipper.AutoLookupMetadata = true;

            var options = new EncodingOptions
            {
                Quality = Mp3Quality.High,
                WriteId3Tags = true
            };

            Console.WriteLine("=== Batch Rip with Auto-Metadata ===");
            Console.WriteLine("Insert CDs and press Enter (Q to quit)\n");

            int cdCount = 0;

            while (true)
            {
                Console.Write("\nInsert CD (or Q to quit): ");
                var input = Console.ReadLine();
                if (input?.ToUpper() == "Q")
                    break;

                try
                {
                    // Wait for CD to be ready
                    await Task.Delay(2000);

                    // Read CD with metadata lookup
                    var cdInfo = service.GetCdInfo();

                    if (!string.IsNullOrEmpty(cdInfo.Artist))
                    {
                        Console.WriteLine($"\n? Found: {cdInfo.Artist} - {cdInfo.Album}");

                        // Rip and encode
                        string outputDir = Path.Combine(@"C:\Music", $"{cdInfo.Artist} - {cdInfo.Album}");
                        var files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);

                        Console.WriteLine($"? Ripped {files.Count} tracks");
                        cdCount++;

                        // Eject CD
                        service.CdRipper.EjectCd();
                    }
                    else
                    {
                        Console.WriteLine("? Could not identify CD. Skipping...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine($"\n? Total CDs ripped: {cdCount}");
        }

        /// <summary>
        /// Example 10: Custom metadata service configuration
        /// </summary>
        public static async Task CustomMetadataServiceExample()
        {
            // Create custom metadata service
            var metadataOptions = new MetadataLookupOptions
            {
                UseMusicBrainz = true,
                UseFreeDB = true,
                DownloadCoverArt = true,
                TimeoutSeconds = 60,
                UserAgent = "nexENCODE Studio/1.0 (myemail@example.com)"
            };

            var metadataService = new CdMetadataService(metadataOptions);

            // Subscribe to status updates
            metadataService.StatusChanged += (s, status) =>
            {
                Console.WriteLine($"[Metadata] {status}");
            };

            Console.WriteLine("=== Custom Metadata Service ===\n");

            // Read CD
            var ripper = new CdRipperService { CdDriveLetter = 'D' };
            var cdInfo = ripper.ReadCdInfo(lookupMetadata: false);

            // Lookup with custom service
            var result = await metadataService.LookupCdMetadataAsync(cdInfo);

            if (result.Success)
            {
                Console.WriteLine($"\n? Success!");
                Console.WriteLine($"Album: {cdInfo.Album}");
                Console.WriteLine($"Tracks found: {cdInfo.Tracks.Count(t => !string.IsNullOrEmpty(t.Title))}");
            }
        }
    }
}
