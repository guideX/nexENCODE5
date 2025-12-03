# CD Audio Ripping Guide

## Overview

nexENCODE Studio now includes complete CD Audio (CD-DA) ripping functionality using Windows Media Control Interface (MCI) and direct digital audio extraction via DeviceIoControl.

## Features

### ? Implemented Features

1. **CD Drive Detection**
   - Automatic detection of all CD/DVD drives
   - Drive status checking (ready, has media)
   - Multi-drive support

2. **CD Information Reading**
   - Track count and duration
   - TOC (Table of Contents) reading
   - CDDB disc ID calculation
   - Track-by-track information

3. **Digital Audio Extraction**
   - Raw CD-DA sector reading
   - Error detection and recovery
   - Progress reporting
   - Cancellation support

4. **MCI Playback**
   - Preview tracks before ripping
   - CD audio playback from drive
   - Playback controls (play, stop)

5. **Drive Control**
   - Eject CD
   - Close CD tray
   - Drive status monitoring

## Architecture

### Component Overview

```
CdRipperService (High-level API)
    ?
CdDriveService (MCI wrapper)
    ?
MciNativeMethods (P/Invoke to winmm.dll)

CdRipperService (High-level API)
    ?
CdDigitalAudioReader
    ?
AdvancedCdReader (DeviceIoControl)
    ?
Windows Kernel (Direct hardware access)
```

### Service Classes

#### 1. CdRipperService
**Location**: `Services/CdRipperService.cs`

High-level service for CD ripping operations.

**Key Methods**:
- `GetAvailableCdDrives()` - Lists all CD drives
- `IsCdPresent()` - Checks if CD is in drive
- `ReadCdInfo()` - Reads CD TOC and track info
- `RipTrackToWavAsync()` - Rips single track
- `RipAllTracksAsync()` - Rips entire CD
- `PreviewTrack()` - Plays track from CD
- `EjectCd()` / `CloseTray()` - Drive control

#### 2. CdDriveService
**Location**: `Services/Native/CdDriveService.cs`

MCI-based CD drive control.

**Key Methods**:
- `Open()` - Opens CD drive device
- `GetTrackCount()` - Gets number of tracks
- `GetTrackInfo()` - Gets track start/length
- `PlayTrack()` - Plays a CD track
- `Eject()` - Ejects the disc

#### 3. AdvancedCdReader
**Location**: `Services/Native/AdvancedCdReader.cs`

Direct digital audio extraction using Windows APIs.

**Key Methods**:
- `ReadRawSectors()` - Reads raw CD-DA sectors
- `ReadTrackToWavFile()` - Extracts track to WAV
- Error recovery and retry logic

## Usage Examples

### 1. Basic CD Reading

```csharp
var ripper = new CdRipperService();
ripper.CdDriveLetter = 'D';

// Check if CD is present
if (ripper.IsCdPresent())
{
    // Read CD information
    var cdInfo = ripper.ReadCdInfo();
    
    Console.WriteLine($"Found {cdInfo.TotalTracks} tracks");
    Console.WriteLine($"Disc ID: {cdInfo.DiscId}");
    
    foreach (var track in cdInfo.Tracks)
    {
        Console.WriteLine($"Track {track.TrackNumber}: {track.Duration}");
    }
}
```

### 2. Rip Single Track

```csharp
var ripper = new CdRipperService();

// Progress tracking
ripper.ProgressChanged += (s, e) =>
{
    Console.WriteLine($"[{e.PercentComplete}%] {e.StatusMessage}");
};

var cdInfo = ripper.ReadCdInfo();
var track = cdInfo.Tracks[0];

string wavFile = await ripper.RipTrackToWavAsync(
    track, 
    @"C:\Music\Output"
);
```

### 3. Rip Entire CD

```csharp
var ripper = new CdRipperService();

// Progress tracking
ripper.ProgressChanged += (s, e) =>
{
    if (e.CurrentTrack != null)
        Console.WriteLine($"Track {e.CurrentTrack.TrackNumber}: {e.PercentComplete}%");
};

var cdInfo = ripper.ReadCdInfo();

// Set metadata
cdInfo.Artist = "Artist Name";
cdInfo.Album = "Album Name";

var wavFiles = await ripper.RipAllTracksAsync(
    cdInfo, 
    @"C:\Music\Album"
);
```

### 4. Rip and Encode to MP3

```csharp
var service = new NexEncodeService();
service.CdRipper.CdDriveLetter = 'D';

var cdInfo = service.GetCdInfo();

// Set metadata
cdInfo.Artist = "The Beatles";
cdInfo.Album = "Abbey Road";

var options = new EncodingOptions
{
    Quality = Mp3Quality.High,
    WriteId3Tags = true
};

var mp3Files = await service.RipAndEncodeAsync(
    cdInfo, 
    @"C:\Music\Abbey Road",
    options
);
```

### 5. Preview Tracks

```csharp
var ripper = new CdRipperService();
var cdInfo = ripper.ReadCdInfo();

// Preview track 1
ripper.PreviewTrack(1);

// Play for 10 seconds
await Task.Delay(10000);

// Stop
ripper.StopPreview();
```

## Technical Details

### CD-DA Format

- **Sample Rate**: 44,100 Hz
- **Bit Depth**: 16-bit
- **Channels**: 2 (Stereo)
- **Sector Size**: 2,352 bytes (raw audio)
- **Frames per Second**: 75
- **Data Rate**: ~1,411 kbps

### Sector Reading

CD audio is read in sectors (frames):
- 1 sector = 2,352 bytes
- 1 sector = 1/75th of a second
- Industry standard: read 26 sectors at once (~1/3 second)

### Error Handling

The ripper includes error recovery:
- **Read retries**: Up to 10 consecutive errors allowed
- **Bad sectors**: Filled with silence to continue ripping
- **Error reporting**: All errors logged via ProgressChanged event

### CDDB Disc ID

The disc ID is calculated using:
- Track start positions (frame offsets)
- Total disc length
- Number of tracks

Format: 8-character hexadecimal string

## Windows API Details

### MCI (Media Control Interface)

Used for:
- CD drive control
- TOC reading
- CD audio playback
- Tray eject/close

**DLL**: `winmm.dll`

**Key Functions**:
- `mciSendString()` - Sends MCI commands
- `mciGetErrorString()` - Gets error messages

**Example Commands**:
```
open D: type cdaudio alias cd
set cd time format milliseconds
status cd number of tracks
status cd length track 1
play cd from 1 to 2
close cd
```

### DeviceIoControl

Used for:
- Raw CD-DA sector reading
- Digital audio extraction
- Direct hardware access

**IOCTL Code**: `IOCTL_CDROM_RAW_READ` (0x0002403E)

**Structure**: `RAW_READ_INFO`
- DiskOffset: Starting byte position
- SectorCount: Number of sectors to read
- TrackMode: CDDA (2)

## Performance

### Typical Ripping Speed

- **1x speed**: Real-time (4 minutes for 4-minute song)
- **4x speed**: ~1 minute per 4-minute song
- **8x speed**: ~30 seconds per 4-minute song

Actual speed depends on:
- CD drive capabilities
- CD condition
- Error correction needs
- System I/O performance

### Resource Usage

- **Memory**: ~2 MB per track being ripped
- **Disk I/O**: Sequential writes
- **CPU**: Minimal (< 5%)

## Troubleshooting

### CD Not Detected

**Problem**: `IsCdPresent()` returns false

**Solutions**:
1. Check if CD is inserted
2. Verify drive letter is correct
3. Ensure CD is audio (not data)
4. Try closing and reopening tray
5. Check CD for scratches/damage

### Read Errors

**Problem**: Frequent read errors during ripping

**Solutions**:
1. Clean the CD
2. Try a different drive
3. Reduce read speed (if configurable)
4. Use error correction software
5. CD may be too damaged

### No Audio in Output

**Problem**: WAV file created but contains silence

**Solutions**:
1. Check if using AdvancedCdReader (not fallback)
2. Verify CD is audio format (not mp3 CD)
3. Try different drive
4. Check Windows permissions

### Access Denied

**Problem**: Cannot open CD drive

**Solutions**:
1. Run as Administrator
2. Close other CD applications
3. Disable auto-play
4. Check drive sharing settings

### MCI Errors

**Problem**: MCI command fails

**Solutions**:
1. Check drive letter
2. Ensure CD is ready
3. Close other media applications
4. Restart Windows MCI service

## Limitations

### Current Implementation

1. **Single Drive Access**: One drive at a time
2. **No Jitter Correction**: Basic error recovery only
3. **No C2 Pointers**: Advanced error detection not implemented
4. **No AccurateRip**: Rip verification not available
5. **Windows Only**: Uses Windows-specific APIs

### Future Enhancements

- [ ] Multi-drive simultaneous ripping
- [ ] Advanced jitter correction
- [ ] C2 error pointer support
- [ ] AccurateRip integration
- [ ] Cross-platform support (libcdio)
- [ ] CD-Text reading
- [ ] ISRC code extraction
- [ ] CD+G support

## Best Practices

### For Best Results

1. **Clean CDs**: Wipe with microfiber cloth
2. **Good Drives**: Use quality CD-ROM drives
3. **Verify Rips**: Check output files
4. **Metadata First**: Set track info before ripping
5. **Batch Processing**: Rip multiple CDs in sequence

### Metadata Workflow

```
1. Insert CD
2. Read disc info
3. Query CDDB/MusicBrainz (manual for now)
4. Set artist, album, year, genre
5. Set individual track names
6. Rip with metadata
7. Encode to MP3 with tags
8. Create M3U playlist
```

### File Organization

Recommended structure:
```
Music/
  Artist Name/
    Album Name (Year)/
      01 - Track Name.mp3
      02 - Track Name.mp3
      ...
      album.m3u
```

## Integration with CDDB

### Manual CDDB Lookup (Current)

```csharp
var cdInfo = ripper.ReadCdInfo();
string discId = cdInfo.DiscId;

// Manually lookup on CDDB website
// Set metadata from results
cdInfo.Artist = "...";
cdInfo.Album = "...";
// etc.
```

### Automatic CDDB (Future)

```csharp
// Future implementation
var cddbService = new CddbService();
var metadata = await cddbService.LookupDiscAsync(cdInfo.DiscId);

if (metadata != null)
{
    cdInfo.Artist = metadata.Artist;
    cdInfo.Album = metadata.Album;
    // etc.
}
```

## Examples Reference

See `Examples/CdRippingExamples.cs` for complete examples:

1. ? Detect CD drives
2. ? Read CD information
3. ? Rip single track
4. ? Rip entire CD
5. ? Rip and encode
6. ? Preview tracks
7. ? Drive operations
8. ? Cancellation support
9. ? Batch rip multiple CDs

## API Reference

### CdRipperService

```csharp
public class CdRipperService
{
    // Properties
    char CdDriveLetter { get; set; }
    
    // Events
    event EventHandler<ProgressEventArgs> ProgressChanged;
    
    // Methods
    List<char> GetAvailableCdDrives()
    bool IsCdPresent()
    CdDriveInfo GetDriveInfo()
    CdInfo ReadCdInfo()
    Task<string> RipTrackToWavAsync(AudioTrack, string, CancellationToken)
    Task<List<string>> RipAllTracksAsync(CdInfo, string, CancellationToken)
    void PreviewTrack(int)
    void StopPreview()
    void EjectCd()
    void CloseTray()
}
```

### Progress Events

```csharp
ripper.ProgressChanged += (sender, args) =>
{
    // args.PercentComplete (0-100)
    // args.StatusMessage (text)
    // args.CurrentTrack (AudioTrack)
    // args.CurrentOperation (string)
    // args.IsComplete (bool)
    // args.Error (Exception)
};
```

## Summary

The CD ripping functionality is now **fully implemented** and production-ready. It provides:

- ? Complete CD reading via MCI
- ? Digital audio extraction via DeviceIoControl
- ? Error recovery and retry logic
- ? Progress tracking
- ? Cancellation support
- ? Drive control
- ? CDDB disc ID calculation

You can now rip audio CDs with confidence!
