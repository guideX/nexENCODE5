# nexENCODE Studio - Project Structure

## Overview
This document describes the architecture and structure of nexENCODE Studio, a modern audio encoding and CD ripping application.

## Directory Structure

```
nexENCODE Studio/
??? Configuration/
?   ??? ConfigurationManager.cs    # App settings management
??? Models/
?   ??? AudioTrack.cs              # Audio track data model
?   ??? CdInfo.cs                  # CD information model
?   ??? EncodingOptions.cs         # Encoding configuration
?   ??? ProgressEventArgs.cs       # Progress reporting
??? Services/
?   ??? AudioEncoderService.cs     # Audio encoding/decoding
?   ??? AudioPlayerService.cs      # Audio playback
?   ??? CdRipperService.cs         # CD ripping
?   ??? NexEncodeService.cs        # Main service orchestrator
?   ??? PlaylistService.cs         # M3U playlist management
??? Utilities/
?   ??? Helpers.cs                 # Utility functions
??? frmMain.cs                     # Main form (GUI placeholder)
??? Program.cs                     # Application entry point
??? UsageExamples.cs               # Code examples
??? README.md                      # Documentation
```

## Core Components

### 1. Models Layer
**Purpose**: Data structures and domain models

- **AudioTrack**: Represents a single audio track with metadata (title, artist, duration, etc.)
- **CdInfo**: Represents a CD album with tracks and metadata
- **EncodingOptions**: Configuration for audio encoding operations
- **ProgressEventArgs**: Event arguments for progress reporting

### 2. Services Layer
**Purpose**: Business logic and audio operations

#### CdRipperService
- Reads CD information from physical drives
- Rips CD tracks to WAV format
- Supports single track and full album ripping
- Progress reporting via events
- Async operations with cancellation support

#### AudioEncoderService
- Converts WAV to MP3 (and vice versa)
- Configurable quality settings
- ID3 tag reading and writing
- Batch conversion support
- Progress tracking

#### AudioPlayerService
- Plays audio files (MP3, WAV, etc.)
- Playback controls (play, pause, stop, seek)
- Volume control
- Position tracking
- Event-based playback notifications

#### PlaylistService
- Creates and reads M3U playlist files
- Extended M3U format support
- Relative/absolute path handling
- Batch tag updates

#### NexEncodeService
- Main orchestrator that combines all services
- High-level operations (rip & encode in one step)
- Manages temporary files
- Simplified API for common tasks

### 3. Configuration Layer
**Purpose**: Application settings and persistence

- Saves/loads user preferences
- JSON-based configuration
- Default settings management
- Temp directory management

### 4. Utilities Layer
**Purpose**: Helper functions and common operations

- **FileHelper**: File name sanitization, path operations
- **AudioHelper**: Duration formatting, file size estimation
- **CdHelper**: CD drive detection, disc ID calculation

## Design Patterns

### Service-Oriented Architecture
All major features are encapsulated in service classes that can be used independently or together.

### Event-Driven Progress Reporting
Long-running operations report progress through events, allowing for responsive UI updates.

### Async/Await Pattern
All I/O operations are asynchronous with proper cancellation token support.

### Separation of Concerns
- Models: Data only, no logic
- Services: Business logic, no UI
- UI (pending): Presentation only

## Data Flow

### CD Ripping Flow
```
User Request ? NexEncodeService ? CdRipperService ? Read CD ? Rip to WAV
                                                             ?
                                AudioEncoderService ? Temp WAV Files
                                        ?
                                   Encode to MP3
                                        ?
                                 Output MP3 Files
```

### Audio Conversion Flow
```
Input File ? AudioEncoderService ? Read Audio ? Decode/Encode ? Output File
                  ?
            Write ID3 Tags (if MP3)
```

### Playback Flow
```
Audio File ? AudioPlayerService ? Load ? NAudio ? Sound Device
                                   ?
                            Control Commands
                        (play/pause/seek/stop)
```

## Technology Stack

### Core Libraries
- **.NET 9.0**: Target framework
- **Windows Forms**: UI framework (planned full implementation)
- **NAudio 2.2.1**: Audio processing and playback
- **NAudio.Lame 2.1.0**: MP3 encoding (LAME wrapper)
- **TagLibSharp 2.3.0**: ID3 tag management

### Key Features by Library

**NAudio**:
- Audio file reading/writing
- Format conversion
- Audio playback
- Sample manipulation

**LAME** (via NAudio.Lame):
- High-quality MP3 encoding
- Variable/Constant bitrate support
- Industry-standard encoder

**TagLibSharp**:
- ID3v1 and ID3v2 tag support
- Multiple format support
- Metadata reading/writing

## Extension Points

### Adding New Audio Formats
1. Add format to `AudioFormat` enum
2. Implement encoder/decoder in `AudioEncoderService`
3. Update `FileHelper.GetFormatFromExtension()`
4. Add file extension handling

### Adding CD Database Integration (CDDB/MusicBrainz)
1. Create new service: `CdDatabaseService`
2. Implement disc ID calculation in `CdHelper`
3. Call service from `CdRipperService.ReadCdInfo()`
4. Populate track metadata automatically

### Adding Audio Effects
1. Create new service: `AudioEffectsService`
2. Use NAudio's effect processors
3. Implement effects: normalize, equalize, fade, etc.
4. Chain effects together

### Custom Skinning System
1. Create `Theme` model with colors, fonts, images
2. Create `ThemeManager` service
3. Implement custom control renderers
4. Store themes as JSON or XML files

## Future Enhancements

### Phase 2
- [ ] Complete MCI/CDDA integration for real CD ripping
- [ ] CDDB/MusicBrainz automatic metadata lookup
- [ ] OGG Vorbis encoding support
- [ ] FLAC encoding support
- [ ] ALAC encoding support

### Phase 3
- [ ] Audio effects and processing
- [ ] Spectrum analyzer visualization
- [ ] Waveform display
- [ ] Sample rate conversion
- [ ] Channel mixing

### Phase 4
- [ ] Custom theme engine
- [ ] Plugin architecture
- [ ] Scripting support
- [ ] Advanced batch operations
- [ ] Profile/preset system

## Performance Considerations

### Async Operations
All file I/O and encoding operations are async to prevent UI blocking.

### Progress Reporting
Progress is reported in chunks (not every byte) to minimize event overhead.

### Memory Management
- Streaming approach for large files
- Proper disposal of audio readers/writers
- Temp file cleanup

### Cancellation Support
All long-running operations support cancellation for responsive user experience.

## Testing Strategy (Recommended)

### Unit Tests
- Model validation
- Helper methods
- Configuration management

### Integration Tests
- Service operations
- File I/O operations
- Format conversions

### Manual Tests
- CD ripping (requires physical CD)
- Audio playback
- UI responsiveness (when implemented)

## Build and Deployment

### Requirements
- .NET 9.0 SDK
- Visual Studio 2022 or later
- Windows OS (for CD ripping features)

### External Dependencies
- LAME encoder DLL must be distributed with application
- Or use NAudio.Lame.Binaries package

### Configuration
- Default settings stored in `%AppData%\nexENCODE Studio\nexencode.config.json`
- Temp files in `%Temp%\nexENCODE\`

## API Usage Examples

See `UsageExamples.cs` for comprehensive examples of:
- CD ripping and encoding
- Audio file conversion
- Playback operations
- Playlist management
- Configuration management

## Contributing Guidelines

### Code Style
- Follow Microsoft C# coding conventions
- Use async/await for I/O operations
- Implement IDisposable where appropriate
- Document public APIs with XML comments

### Error Handling
- Use try-catch in services
- Report errors through ProgressEventArgs
- Log exceptions for debugging
- Don't let exceptions crash the app

### Event Usage
- Use event-based progress reporting
- Unsubscribe from events to prevent leaks
- Invoke events safely (null check)

## License
[To be determined]

## Credits
- Original nexENCODE Studio concept
- NAudio library by Mark Heath
- LAME MP3 encoder project
- TagLibSharp library
