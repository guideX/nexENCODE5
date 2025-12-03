using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services;

namespace nexENCODE_Studio.Examples
{
    /// <summary>
    /// Examples demonstrating CD ripping functionality
    /// </summary>
    public static class CdRippingExamples
    {
        /// <summary>
        /// Example 1: Detect CD drives and check for CD
        /// </summary>
        public static void DetectCdDrivesExample()
        {
            var ripper = new CdRipperService();
            
            Console.WriteLine("=== Detecting CD Drives ===");
            
            // Get all CD drives
            var drives = ripper.GetAvailableCdDrives();
            Console.WriteLine($"Found {drives.Count} CD drive(s):");
            
            foreach (var drive in drives)
            {
                Console.WriteLine($"\nDrive: {drive}:");
                
                ripper.CdDriveLetter = drive;
                
                // Check if CD is present
                bool hasCD = ripper.IsCdPresent();
                Console.WriteLine($"  CD Present: {hasCD}");
                
                if (hasCD)
                {
                    // Get detailed drive info
                    var driveInfo = ripper.GetDriveInfo();
                    Console.WriteLine($"  Ready: {driveInfo.IsReady}");
                    Console.WriteLine($"  Has Audio CD: {driveInfo.HasAudioCD}");
                    Console.WriteLine($"  Track Count: {driveInfo.TrackCount}");
                    Console.WriteLine($"  Media Type: {driveInfo.MediaType}");
                }
            }
        }
        
        /// <summary>
        /// Example 2: Read CD information
        /// </summary>
        public static void ReadCdInformationExample()
        {
            var ripper = new CdRipperService();
            
            // Set drive letter
            ripper.CdDriveLetter = 'D';
            
            Console.WriteLine("=== Reading CD Information ===");
            
            try
            {
                // Read CD info
                var cdInfo = ripper.ReadCdInfo();
                
                Console.WriteLine($"Disc ID: {cdInfo.DiscId}");
                Console.WriteLine($"Total Tracks: {cdInfo.TotalTracks}");
                Console.WriteLine($"Total Duration: {cdInfo.TotalDuration}");
                Console.WriteLine("\nTracks:");
                
                foreach (var track in cdInfo.Tracks)
                {
                    Console.WriteLine($"  {track.TrackNumber:00}. Duration: {track.Duration:mm\\:ss}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 3: Rip a single track
        /// </summary>
        public static async Task RipSingleTrackExample()
        {
            var ripper = new CdRipperService();
            
            // Progress reporting
            ripper.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
                if (e.Error != null)
                    Console.WriteLine($"Error: {e.Error.Message}");
            };
            
            Console.WriteLine("=== Ripping Single Track ===");
            
            try
            {
                // Read CD info
                var cdInfo = ripper.ReadCdInfo();
                
                // Rip track 1
                var track = cdInfo.Tracks[0];
                track.Title = "My Favorite Song";
                track.Artist = "My Favorite Band";
                
                string outputDir = @"C:\Music\CD_Rip";
                string wavFile = await ripper.RipTrackToWavAsync(track, outputDir);
                
                Console.WriteLine($"\nTrack ripped to: {wavFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 4: Rip entire CD with metadata
        /// </summary>
        public static async Task RipEntireCdExample()
        {
            var ripper = new CdRipperService();
            
            // Progress reporting
            ripper.ProgressChanged += (sender, e) =>
            {
                if (e.CurrentTrack != null)
                {
                    Console.WriteLine($"Track {e.CurrentTrack.TrackNumber}: [{e.PercentComplete}%] {e.StatusMessage}");
                }
                else
                {
                    Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
                }
            };
            
            Console.WriteLine("=== Ripping Entire CD ===");
            
            try
            {
                // Read CD info
                var cdInfo = ripper.ReadCdInfo();
                
                // Set album metadata (normally from CDDB/MusicBrainz)
                cdInfo.Artist = "The Beatles";
                cdInfo.Album = "Abbey Road";
                cdInfo.Year = 1969;
                cdInfo.Genre = "Rock";
                
                // Set track names
                string[] trackNames = {
                    "Come Together", "Something", "Maxwell's Silver Hammer",
                    "Oh! Darling", "Octopus's Garden", "I Want You (She's So Heavy)",
                    "Here Comes the Sun", "Because", "You Never Give Me Your Money",
                    "Sun King", "Mean Mr. Mustard", "Polythene Pam",
                    "She Came In Through the Bathroom Window", "Golden Slumbers",
                    "Carry That Weight", "The End", "Her Majesty"
                };
                
                for (int i = 0; i < cdInfo.Tracks.Count && i < trackNames.Length; i++)
                {
                    cdInfo.Tracks[i].Title = trackNames[i];
                    cdInfo.Tracks[i].Artist = cdInfo.Artist;
                    cdInfo.Tracks[i].Album = cdInfo.Album;
                    cdInfo.Tracks[i].Year = cdInfo.Year;
                    cdInfo.Tracks[i].Genre = cdInfo.Genre;
                }
                
                // Rip all tracks
                string outputDir = @"C:\Music\Abbey Road";
                var wavFiles = await ripper.RipAllTracksAsync(cdInfo, outputDir);
                
                Console.WriteLine($"\n? Ripped {wavFiles.Count} tracks successfully!");
                foreach (var file in wavFiles)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 5: Rip CD and encode to MP3 in one operation
        /// </summary>
        public static async Task RipAndEncodeExample()
        {
            var service = new NexEncodeService();
            
            // Progress tracking
            service.ProgressChanged += (sender, e) =>
            {
                string trackInfo = e.CurrentTrack != null 
                    ? $"Track {e.CurrentTrack.TrackNumber}: " 
                    : "";
                Console.WriteLine($"{trackInfo}[{e.PercentComplete}%] {e.StatusMessage}");
            };
            
            Console.WriteLine("=== Rip and Encode CD ===");
            
            try
            {
                // Configure
                service.CdRipper.CdDriveLetter = 'D';
                
                // Read CD
                var cdInfo = service.GetCdInfo();
                
                // Set metadata
                cdInfo.Artist = "Pink Floyd";
                cdInfo.Album = "Dark Side of the Moon";
                cdInfo.Year = 1973;
                cdInfo.Genre = "Progressive Rock";
                
                // Set track names
                string[] tracks = {
                    "Speak to Me", "Breathe", "On the Run", "Time",
                    "The Great Gig in the Sky", "Money", "Us and Them",
                    "Any Colour You Like", "Brain Damage", "Eclipse"
                };
                
                for (int i = 0; i < cdInfo.Tracks.Count && i < tracks.Length; i++)
                {
                    cdInfo.Tracks[i].Title = tracks[i];
                    cdInfo.Tracks[i].Artist = cdInfo.Artist;
                    cdInfo.Tracks[i].Album = cdInfo.Album;
                    cdInfo.Tracks[i].Year = cdInfo.Year;
                    cdInfo.Tracks[i].Genre = cdInfo.Genre;
                }
                
                // Encoding options
                var options = new EncodingOptions
                {
                    Format = AudioFormat.Mp3,
                    Quality = Mp3Quality.High,
                    WriteId3Tags = true
                };
                
                // Rip and encode
                string outputDir = @"C:\Music\Dark Side of the Moon";
                var mp3Files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);
                
                Console.WriteLine($"\n? Complete! {mp3Files.Count} MP3 files created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 6: Preview CD tracks before ripping
        /// </summary>
        public static void PreviewCdTracksExample()
        {
            var ripper = new CdRipperService();
            ripper.CdDriveLetter = 'D';
            
            Console.WriteLine("=== Preview CD Tracks ===");
            
            try
            {
                var cdInfo = ripper.ReadCdInfo();
                
                Console.WriteLine($"Found {cdInfo.TotalTracks} tracks");
                Console.WriteLine("Playing first 10 seconds of each track...\n");
                
                foreach (var track in cdInfo.Tracks)
                {
                    Console.WriteLine($"Playing Track {track.TrackNumber}...");
                    
                    // Play track
                    ripper.PreviewTrack(track.TrackNumber);
                    
                    // Play for 10 seconds
                    Thread.Sleep(10000);
                    
                    // Stop
                    ripper.StopPreview();
                    
                    Console.WriteLine("Stopped.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 7: CD drive operations (eject, close tray)
        /// </summary>
        public static void CdDriveOperationsExample()
        {
            var ripper = new CdRipperService();
            ripper.CdDriveLetter = 'D';
            
            Console.WriteLine("=== CD Drive Operations ===");
            
            try
            {
                // Check if CD is present
                Console.WriteLine("Checking for CD...");
                bool hasCD = ripper.IsCdPresent();
                Console.WriteLine($"CD Present: {hasCD}");
                
                if (hasCD)
                {
                    // Eject CD
                    Console.WriteLine("\nEjecting CD...");
                    ripper.EjectCd();
                    
                    Console.WriteLine("Press Enter to close tray...");
                    Console.ReadLine();
                    
                    // Close tray
                    Console.WriteLine("Closing tray...");
                    ripper.CloseTray();
                    
                    // Wait for CD to be ready
                    Thread.Sleep(3000);
                    
                    Console.WriteLine("Ready!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 8: Rip with cancellation support
        /// </summary>
        public static async Task RipWithCancellationExample()
        {
            var ripper = new CdRipperService();
            ripper.CdDriveLetter = 'D';
            
            // Progress tracking
            ripper.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
            };
            
            Console.WriteLine("=== Rip with Cancellation ===");
            Console.WriteLine("Ripping will start and be cancelled after 10 seconds...\n");
            
            try
            {
                var cdInfo = ripper.ReadCdInfo();
                
                // Create cancellation token
                var cts = new CancellationTokenSource();
                
                // Start ripping
                var ripTask = ripper.RipAllTracksAsync(cdInfo, @"C:\Music\Test", cts.Token);
                
                // Cancel after 10 seconds
                await Task.Delay(10000);
                Console.WriteLine("\n*** CANCELLING RIP ***\n");
                cts.Cancel();
                
                // Wait for completion
                var files = await ripTask;
                
                Console.WriteLine($"Rip cancelled. {files.Count} tracks partially completed.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Rip was cancelled by user.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Example 9: Batch rip multiple CDs
        /// </summary>
        public static async Task BatchRipMultipleCdsExample()
        {
            var service = new NexEncodeService();
            service.CdRipper.CdDriveLetter = 'D';
            
            var options = new EncodingOptions
            {
                Quality = Mp3Quality.High,
                WriteId3Tags = true
            };
            
            Console.WriteLine("=== Batch Rip Multiple CDs ===");
            Console.WriteLine("This will rip multiple CDs in sequence.\n");
            
            int cdNumber = 1;
            
            while (true)
            {
                Console.WriteLine($"\n--- CD #{cdNumber} ---");
                Console.WriteLine("Insert CD and press Enter (or Q to quit)...");
                
                var input = Console.ReadLine();
                if (input?.ToUpper() == "Q")
                    break;
                
                try
                {
                    // Wait for CD to be ready
                    Console.WriteLine("Waiting for CD...");
                    await Task.Delay(2000);
                    
                    // Read CD info
                    var cdInfo = service.GetCdInfo();
                    Console.WriteLine($"Found {cdInfo.TotalTracks} tracks");
                    
                    // Get album info (in real app, would query CDDB)
                    Console.Write("Album Artist: ");
                    cdInfo.Artist = Console.ReadLine() ?? "";
                    
                    Console.Write("Album Name: ");
                    cdInfo.Album = Console.ReadLine() ?? "";
                    
                    // Update tracks
                    foreach (var track in cdInfo.Tracks)
                    {
                        track.Artist = cdInfo.Artist;
                        track.Album = cdInfo.Album;
                    }
                    
                    // Rip and encode
                    string outputDir = Path.Combine(
                        @"C:\Music",
                        $"{cdInfo.Artist} - {cdInfo.Album}"
                    );
                    
                    var files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);
                    
                    Console.WriteLine($"\n? CD #{cdNumber} complete: {files.Count} tracks");
                    
                    // Eject CD
                    service.CdRipper.EjectCd();
                    
                    cdNumber++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Skipping this CD...");
                }
            }
            
            Console.WriteLine($"\n? Batch complete! Ripped {cdNumber - 1} CDs");
        }
    }
}
