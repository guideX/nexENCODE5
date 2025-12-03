using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services;

namespace nexENCODE_Studio
{
    /// <summary>
    /// Example usage of nexENCODE Studio services
    /// </summary>
    public static class UsageExamples
    {
        /// <summary>
        /// Example: Rip a CD to MP3 files
        /// </summary>
        public static async Task RipCdToMp3Example()
        {
            var service = new NexEncodeService();
            
            // Subscribe to progress updates
            service.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
                if (e.Error != null)
                    Console.WriteLine($"Error: {e.Error.Message}");
            };
            
            // Configure encoding options
            var options = new EncodingOptions
            {
                Format = AudioFormat.Mp3,
                Quality = Mp3Quality.High,
                SampleRate = 44100,
                Channels = 2,
                WriteId3Tags = true
            };
            
            // Set the CD drive letter
            service.CdRipper.CdDriveLetter = 'D';
            
            // Read CD information
            var cdInfo = service.GetCdInfo();
            
            // Optionally set CD metadata (in a real app, you'd get this from CDDB/MusicBrainz)
            cdInfo.Artist = "Artist Name";
            cdInfo.Album = "Album Name";
            cdInfo.Year = 2024;
            cdInfo.Genre = "Rock";
            
            // Update track metadata
            for (int i = 0; i < cdInfo.Tracks.Count; i++)
            {
                cdInfo.Tracks[i].Artist = cdInfo.Artist;
                cdInfo.Tracks[i].Album = cdInfo.Album;
                cdInfo.Tracks[i].Year = cdInfo.Year;
                cdInfo.Tracks[i].Genre = cdInfo.Genre;
                cdInfo.Tracks[i].Title = $"Track {i + 1}"; // Would come from CDDB
            }
            
            // Rip and encode
            string outputDir = @"C:\Music\MyRippedCD";
            var mp3Files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);
            
            Console.WriteLine($"Successfully ripped {mp3Files.Count} tracks!");
            
            // Create a playlist
            string playlistPath = Path.Combine(outputDir, "album.m3u");
            service.CreatePlaylistFromTracks(playlistPath, cdInfo.Tracks);
        }
        
        /// <summary>
        /// Example: Convert WAV to MP3
        /// </summary>
        public static async Task ConvertWavToMp3Example()
        {
            var encoder = new AudioEncoderService();
            
            encoder.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
            };
            
            var metadata = new AudioTrack
            {
                TrackNumber = 1,
                Title = "My Song",
                Artist = "My Artist",
                Album = "My Album",
                Year = 2024,
                Genre = "Pop"
            };
            
            var options = new EncodingOptions
            {
                Format = AudioFormat.Mp3,
                Quality = Mp3Quality.High,
                WriteId3Tags = true,
                OutputDirectory = @"C:\Music\Output"
            };
            
            string wavFile = @"C:\Music\source.wav";
            string mp3File = await encoder.ConvertWavToMp3Async(wavFile, metadata, options);
            
            Console.WriteLine($"Converted to: {mp3File}");
        }
        
        /// <summary>
        /// Example: Convert MP3 to WAV
        /// </summary>
        public static async Task ConvertMp3ToWavExample()
        {
            var encoder = new AudioEncoderService();
            
            encoder.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
            };
            
            string mp3File = @"C:\Music\song.mp3";
            string wavFile = await encoder.ConvertMp3ToWavAsync(mp3File);
            
            Console.WriteLine($"Converted to: {wavFile}");
        }
        
        /// <summary>
        /// Example: Play audio file
        /// </summary>
        public static void PlayAudioExample()
        {
            var player = new AudioPlayerService();
            
            player.PlaybackStopped += (sender, e) =>
            {
                Console.WriteLine("Playback finished");
            };
            
            // Load and play
            string audioFile = @"C:\Music\song.mp3";
            player.Load(audioFile);
            player.Volume = 0.8f; // 80% volume
            player.Play();
            
            // Display info
            Console.WriteLine($"Duration: {player.TotalTime}");
            Console.WriteLine($"Playing: {player.IsPlaying}");
            
            // Seek to 30 seconds
            player.Seek(TimeSpan.FromSeconds(30));
            
            // Pause
            player.Pause();
            
            // Resume
            player.Play();
            
            // Stop
            player.Stop();
            
            // Don't forget to dispose
            player.Dispose();
        }
        
        /// <summary>
        /// Example: Work with M3U playlists
        /// </summary>
        public static void PlaylistExample()
        {
            var playlistService = new PlaylistService();
            
            // Create a playlist
            var tracks = new List<AudioTrack>
            {
                new AudioTrack
                {
                    TrackNumber = 1,
                    Title = "Song 1",
                    Artist = "Artist A",
                    Duration = TimeSpan.FromMinutes(3.5),
                    FilePath = @"C:\Music\song1.mp3"
                },
                new AudioTrack
                {
                    TrackNumber = 2,
                    Title = "Song 2",
                    Artist = "Artist B",
                    Duration = TimeSpan.FromMinutes(4.2),
                    FilePath = @"C:\Music\song2.mp3"
                }
            };
            
            // Write playlist
            string playlistPath = @"C:\Music\myplaylist.m3u";
            playlistService.WriteM3uPlaylist(playlistPath, tracks, extended: true);
            
            // Read playlist
            var loadedTracks = playlistService.ReadM3uPlaylist(playlistPath);
            Console.WriteLine($"Loaded {loadedTracks.Count} tracks from playlist");
            
            // Update tags
            loadedTracks[0].Title = "Updated Song Title";
            playlistService.UpdatePlaylistTags(playlistPath, loadedTracks);
        }
        
        /// <summary>
        /// Example: Get audio file information
        /// </summary>
        public static void GetAudioInfoExample()
        {
            var encoder = new AudioEncoderService();
            
            string audioFile = @"C:\Music\song.mp3";
            var track = encoder.GetAudioFileInfo(audioFile);
            
            Console.WriteLine($"Title: {track.Title}");
            Console.WriteLine($"Artist: {track.Artist}");
            Console.WriteLine($"Album: {track.Album}");
            Console.WriteLine($"Duration: {track.Duration}");
            Console.WriteLine($"File Size: {track.FileSize / 1024 / 1024} MB");
        }
        
        /// <summary>
        /// Example: Batch convert multiple files
        /// </summary>
        public static async Task BatchConvertExample()
        {
            var encoder = new AudioEncoderService();
            
            var wavFiles = new List<string>
            {
                @"C:\Music\track01.wav",
                @"C:\Music\track02.wav",
                @"C:\Music\track03.wav"
            };
            
            var metadata = new List<AudioTrack>
            {
                new AudioTrack { TrackNumber = 1, Title = "Track 1", Artist = "Artist" },
                new AudioTrack { TrackNumber = 2, Title = "Track 2", Artist = "Artist" },
                new AudioTrack { TrackNumber = 3, Title = "Track 3", Artist = "Artist" }
            };
            
            var options = new EncodingOptions
            {
                Format = AudioFormat.Mp3,
                Quality = Mp3Quality.VeryHigh,
                OutputDirectory = @"C:\Music\Converted"
            };
            
            var mp3Files = await encoder.BatchConvertWavToMp3Async(wavFiles, metadata, options);
            
            Console.WriteLine($"Converted {mp3Files.Count} files");
        }
    }
}
