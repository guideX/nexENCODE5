# nexENCODE Studio

A modern .NET 9 recreation of the classic 90's audio encoding software, built with C# and Windows Forms.

## Features

### Core Audio Operations
- **CD Ripping**: Rip audio CDs to WAV format
- **MP3 Encoding**: Convert WAV files to MP3 with configurable quality settings
- **MP3 Decoding**: Convert MP3 files back to WAV
- **Audio Playback**: Play various audio formats (MP3, WAV, etc.)
- **Batch Processing**: Convert multiple files at once

### Metadata Management
- **ID3 Tag Support**: Read and write ID3v1 and ID3v2 tags
- **M3U Playlists**: Create, read, and edit M3U playlist files
- **Automatic Tagging**: Support for automatic CD information lookup (CDDB/MusicBrainz integration ready)

### Supported Formats
- **Input**: CD Audio (CDDA), WAV, MP3
- **Output**: MP3, WAV
- **Planned**: OGG Vorbis, FLAC, ALAC

## Architecture

The application is built with a clean separation of concerns:

### Models (`Models/`)
- `AudioTrack.cs` - Represents an audio track with metadata
- `CdInfo.cs` - Represents CD album information
- `EncodingOptions.cs` - Configuration for audio encoding
- `ProgressEventArgs.cs` - Progress reporting for operations

### Services (`Services/`)
- `CdRipperService.cs` - CD ripping functionality
- `AudioEncoderService.cs` - Audio encoding/decoding (MP3, WAV)
- `AudioPlayerService.cs` - Audio playback
- `PlaylistService.cs` - M3U playlist management
- `NexEncodeService.cs` - Main orchestration service

## Dependencies

- **NAudio** (2.2.1) - Audio processing and playback
- **NAudio.Lame** (2.1.0) - MP3 encoding via LAME encoder
- **TagLibSharp** (2.3.0) - ID3 tag reading/writing
- **.NET 9.0** - Target framework

## Usage Examples

### Rip CD to MP3

```csharp
var service = new NexEncodeService();

// Configure encoding
var options = new EncodingOptions
{
    Format = AudioFormat.Mp3,
    Quality = Mp3Quality.High,
    WriteId3Tags = true
};

// Set CD drive
service.CdRipper.CdDriveLetter = 'D';

// Get CD info
var cdInfo = service.GetCdInfo();

// Set metadata
cdInfo.Artist = "Artist Name";
cdInfo.Album = "Album Name";

// Rip and encode
var mp3Files = await service.RipAndEncodeAsync(cdInfo, @"C:\Music\Output", options);
```

### Convert WAV to MP3

```csharp
var encoder = new AudioEncoderService();

var metadata = new AudioTrack
{
    Title = "My Song",
    Artist = "My Artist",
    Album = "My Album"
};

var options = new EncodingOptions
{
    Quality = Mp3Quality.High,
    WriteId3Tags = true
};

string mp3File = await encoder.ConvertWavToMp3Async("input.wav", metadata, options);
```

### Play Audio

```csharp
var player = new AudioPlayerService();

player.Load("song.mp3");
player.Volume = 0.8f;
player.Play();

// Control playback
player.Pause();
player.Seek(TimeSpan.FromSeconds(30));
player.Play();
player.Stop();
```

### Work with Playlists

```csharp
var playlistService = new PlaylistService();

// Create playlist
var tracks = new List<AudioTrack> { /* your tracks */ };
playlistService.WriteM3uPlaylist("playlist.m3u", tracks, extended: true);

// Read playlist
var loadedTracks = playlistService.ReadM3uPlaylist("playlist.m3u");

// Update tags
playlistService.UpdatePlaylistTags("playlist.m3u", loadedTracks);
```

## Encoding Quality Settings

| Quality | Bitrate | Description |
|---------|---------|-------------|
| Low | 128 kbps | Smaller file size, lower quality |
| Medium | 192 kbps | Good balance |
| High | 256 kbps | High quality (recommended) |
| VeryHigh | 320 kbps | Maximum MP3 quality |

## Progress Tracking

All long-running operations support progress tracking:

```csharp
service.ProgressChanged += (sender, e) =>
{
    Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
    if (e.Error != null)
        Console.WriteLine($"Error: {e.Error.Message}");
};
```

## Roadmap

### Phase 1 (Current)
- ? CD ripping infrastructure
- ? MP3 encoding/decoding
- ? Audio playback
- ? M3U playlist support
- ? ID3 tag management

### Phase 2 (Planned)
- ?? Complete MCI integration for CD-DA reading
- ?? CDDB/MusicBrainz integration for automatic metadata
- ?? OGG Vorbis support
- ?? FLAC support
- ?? ALAC support

### Phase 3 (Planned)
- ?? WAV file effects (normalization, equalization, etc.)
- ?? Sample rate conversion
- ?? Audio analysis and visualization
- ?? CD audio playback from drive

### Phase 4 (Planned)
- ?? Custom skinning system
- ?? Plugin architecture
- ?? Batch operations UI
- ?? Advanced settings and presets

## Notes for GUI Implementation

The current implementation provides a complete service layer with minimal GUI. When implementing the full GUI:

1. **Theming/Skinning**: All services are UI-independent, making it easy to implement custom graphics and skins
2. **Async Operations**: All long-running operations are async with cancellation support
3. **Progress Reporting**: Use the `ProgressChanged` events to update UI progress bars
4. **Error Handling**: All exceptions are captured and reported through progress events

## Technical Notes

### CD Ripping
The current CD ripper uses a placeholder implementation. For production use, you'll need to:
- Implement MCI (Media Control Interface) P/Invoke calls
- Or use a library like Bass.Net
- Or implement direct CDDA reading

### LAME MP3 Encoder
NAudio.Lame requires the LAME encoder DLL. Ensure `libmp3lame.64.dll` or `libmp3lame.32.dll` is available in your output directory.

### File I/O
All file operations support relative and absolute paths. Playlist files store relative paths when possible for portability.

## License

[Your License Here]

## Credits

- NAudio by Mark Heath
- LAME MP3 Encoder
- TagLibSharp
- Original nexENCODE Studio (90's version)
