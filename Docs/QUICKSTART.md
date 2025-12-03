# Quick Start Guide - nexENCODE Studio

## Getting Started in 5 Minutes

### 1. Basic Setup

```csharp
using nexENCODE_Studio.Services;
using nexENCODE_Studio.Models;
using nexENCODE_Studio.Configuration;

// Initialize the main service
var nexEncode = new NexEncodeService();

// Load configuration
var config = new ConfigurationManager();
```

### 2. Rip a CD to MP3

```csharp
// Set your CD drive letter
nexEncode.CdRipper.CdDriveLetter = 'D';

// Read CD info
var cdInfo = nexEncode.GetCdInfo();

// Set album metadata (normally from CDDB)
cdInfo.Artist = "The Beatles";
cdInfo.Album = "Abbey Road";
cdInfo.Year = 1969;
cdInfo.Genre = "Rock";

// Set track names
cdInfo.Tracks[0].Title = "Come Together";
cdInfo.Tracks[1].Title = "Something";
// ... etc

// Configure encoding
var options = new EncodingOptions
{
    Format = AudioFormat.Mp3,
    Quality = Mp3Quality.High,  // 256 kbps
    WriteId3Tags = true
};

// Rip and encode
string outputDir = @"C:\Music\Abbey Road";
var files = await nexEncode.RipAndEncodeAsync(cdInfo, outputDir, options);

Console.WriteLine($"Ripped {files.Count} tracks!");
```

### 3. Convert WAV to MP3

```csharp
var encoder = new AudioEncoderService();

// Set up metadata
var track = new AudioTrack
{
    TrackNumber = 1,
    Title = "My Song",
    Artist = "My Band",
    Album = "My Album",
    Year = 2024,
    Genre = "Rock"
};

// Configure encoding
var options = new EncodingOptions
{
    Quality = Mp3Quality.VeryHigh,  // 320 kbps
    WriteId3Tags = true,
    OutputDirectory = @"C:\Music\Output"
};

// Convert
string mp3File = await encoder.ConvertWavToMp3Async(
    @"C:\Music\input.wav", 
    track, 
    options
);
```

### 4. Play an Audio File

```csharp
var player = new AudioPlayerService();

// Load and play
player.PlayFile(@"C:\Music\song.mp3");

// Volume control (0.0 to 1.0)
player.Volume = 0.75f;

// Check status
if (player.IsPlaying)
{
    Console.WriteLine($"Playing: {player.CurrentTime} / {player.TotalTime}");
}

// Pause
player.Pause();

// Resume
player.Play();

// Seek to position
player.Seek(TimeSpan.FromMinutes(1));

// Stop
player.Stop();

// Cleanup
player.Dispose();
```

### 5. Work with Playlists

```csharp
var playlist = new PlaylistService();

// Create tracks list
var tracks = new List<AudioTrack>
{
    new AudioTrack 
    { 
        Title = "Song 1", 
        Artist = "Artist A",
        FilePath = @"C:\Music\song1.mp3"
    },
    new AudioTrack 
    { 
        Title = "Song 2", 
        Artist = "Artist B",
        FilePath = @"C:\Music\song2.mp3"
    }
};

// Save playlist
playlist.WriteM3uPlaylist(@"C:\Music\mylist.m3u", tracks);

// Load playlist
var loaded = playlist.ReadM3uPlaylist(@"C:\Music\mylist.m3u");

// Play all tracks in playlist
var player = new AudioPlayerService();
foreach (var track in loaded)
{
    player.PlayFile(track.FilePath);
    // Wait for playback to complete...
}
```

### 6. Batch Convert Files

```csharp
var encoder = new AudioEncoderService();

// Progress tracking
encoder.ProgressChanged += (s, e) =>
{
    Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
};

// List of files to convert
var wavFiles = Directory.GetFiles(@"C:\Music\WAV", "*.wav").ToList();

// Set up encoding options
var options = new EncodingOptions
{
    Format = AudioFormat.Mp3,
    Quality = Mp3Quality.High,
    OutputDirectory = @"C:\Music\MP3"
};

// Convert all files
var mp3Files = await encoder.BatchConvertWavToMp3Async(
    wavFiles, 
    null,  // No metadata
    options
);

Console.WriteLine($"Converted {mp3Files.Count} files");
```

### 7. Using Configuration

```csharp
var config = new ConfigurationManager();

// Get current settings
var settings = config.Settings;

// Update settings
config.UpdateSetting(s =>
{
    s.DefaultCdDrive = 'E';
    s.DefaultOutputDirectory = @"D:\Music";
    s.DefaultEncodingOptions.Quality = Mp3Quality.VeryHigh;
});

// Use settings with services
nexEncode.CdRipper.CdDriveLetter = settings.DefaultCdDrive;

// Reset to defaults
config.ResetToDefaults();
```

### 8. Get File Information

```csharp
var encoder = new AudioEncoderService();

// Read file info and metadata
var track = encoder.GetAudioFileInfo(@"C:\Music\song.mp3");

Console.WriteLine($"Title: {track.Title}");
Console.WriteLine($"Artist: {track.Artist}");
Console.WriteLine($"Album: {track.Album}");
Console.WriteLine($"Duration: {track.Duration}");
Console.WriteLine($"Size: {track.FileSize / 1024} KB");
```

### 9. Progress Tracking

```csharp
// Subscribe to progress events
nexEncode.ProgressChanged += (sender, args) =>
{
    // Update progress bar
    Console.WriteLine($"Progress: {args.PercentComplete}%");
    
    // Show current operation
    Console.WriteLine($"Status: {args.StatusMessage}");
    
    // Show current track
    if (args.CurrentTrack != null)
    {
        Console.WriteLine($"Track: {args.CurrentTrack.GetDisplayName()}");
    }
    
    // Handle errors
    if (args.Error != null)
    {
        Console.WriteLine($"Error: {args.Error.Message}");
    }
    
    // Check if complete
    if (args.IsComplete)
    {
        Console.WriteLine("Operation completed!");
    }
};
```

### 10. Cancellation Support

```csharp
// Create cancellation token source
var cts = new CancellationTokenSource();

// Start long operation
var task = nexEncode.RipAndEncodeAsync(
    cdInfo, 
    outputDir, 
    options, 
    cts.Token
);

// Cancel after 5 seconds (example)
await Task.Delay(5000);
cts.Cancel();

try
{
    await task;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

## Common Scenarios

### Scenario 1: Rip Entire CD Collection
```csharp
var nexEncode = new NexEncodeService();
var config = new ConfigurationManager();

nexEncode.CdRipper.CdDriveLetter = config.Settings.DefaultCdDrive;

while (true)
{
    Console.WriteLine("Insert CD and press Enter (or Q to quit)...");
    var input = Console.ReadLine();
    if (input?.ToUpper() == "Q") break;
    
    var cdInfo = nexEncode.GetCdInfo();
    // TODO: Look up metadata from CDDB
    
    string albumDir = Path.Combine(
        config.Settings.DefaultOutputDirectory,
        $"{cdInfo.Artist} - {cdInfo.Album}"
    );
    
    await nexEncode.RipAndEncodeAsync(
        cdInfo, 
        albumDir, 
        config.Settings.DefaultEncodingOptions
    );
    
    Console.WriteLine("CD complete! Remove disc.");
}
```

### Scenario 2: Convert Music Library
```csharp
var encoder = new AudioEncoderService();

string sourceDir = @"C:\Music\FLAC";
string targetDir = @"C:\Music\MP3";

var flacFiles = Directory.GetFiles(sourceDir, "*.flac", SearchOption.AllDirectories);

foreach (var flacFile in flacFiles)
{
    // Note: FLAC support needs to be implemented
    // This is just an example structure
    var relativePath = Path.GetRelativePath(sourceDir, flacFile);
    var outputPath = Path.Combine(targetDir, Path.ChangeExtension(relativePath, ".mp3"));
    
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    
    // Convert file...
}
```

### Scenario 3: Create Album Playlist
```csharp
var playlist = new PlaylistService();

string albumDir = @"C:\Music\My Album";
var mp3Files = Directory.GetFiles(albumDir, "*.mp3");

var tracks = new List<AudioTrack>();
foreach (var file in mp3Files)
{
    var encoder = new AudioEncoderService();
    var track = encoder.GetAudioFileInfo(file);
    tracks.Add(track);
}

// Sort by track number
tracks = tracks.OrderBy(t => t.TrackNumber).ToList();

// Save playlist
string playlistPath = Path.Combine(albumDir, "album.m3u");
playlist.WriteM3uPlaylist(playlistPath, tracks);
```

## Tips and Best Practices

1. **Always dispose of players**: Use `using` statements or call `Dispose()` explicitly
2. **Handle progress events**: Keep UI responsive during long operations
3. **Use cancellation tokens**: Allow users to cancel lengthy operations
4. **Validate file paths**: Check if files/directories exist before operations
5. **Handle errors gracefully**: Use try-catch and check `ProgressEventArgs.Error`
6. **Configure quality wisely**: Higher bitrates = larger files
7. **Use metadata**: Always populate track info for better organization
8. **Create playlists**: Keep your music organized with M3U playlists

## Next Steps

- See `UsageExamples.cs` for more detailed examples
- Read `README.md` for complete feature documentation
- Check `PROJECT_STRUCTURE.md` for architecture details
- Implement the GUI using the service layer
- Add CDDB/MusicBrainz integration for automatic metadata
- Implement additional audio formats (OGG, FLAC, ALAC)

## Troubleshooting

### MP3 encoding fails
- Ensure LAME DLL is in the output directory
- Use NAudio.Lame.Binaries package to automatically include it

### CD reading fails
- Check if CD drive letter is correct
- Ensure CD is inserted and readable
- Current implementation is placeholder - full MCI integration needed

### No audio playback
- Check if output device is available
- Verify audio file is not corrupted
- Check volume levels and system audio

### Tags not written
- Ensure WriteId3Tags is true in EncodingOptions
- Check file permissions
- Verify file format supports tags (MP3, FLAC, etc.)

## Support

For issues, feature requests, or contributions, see the main README.md file.
