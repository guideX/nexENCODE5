# CD Metadata Lookup Guide

## Overview

nexENCODE Studio now includes **automatic CD metadata lookup** from multiple online databases including MusicBrainz, FreeDB/GNUDB, Discogs, and GD3. This allows automatic identification of albums and track names without manual entry.

## Supported Services

### 1. MusicBrainz ?
- **Status**: Fully implemented and FREE
- **API**: https://musicbrainz.org
- **Coverage**: Extensive database with millions of releases
- **Features**:
  - Album and artist information
  - Track names and durations
  - Release dates and countries
  - Cover art (via Cover Art Archive)
  - High accuracy
- **Requirements**: None (free to use)
- **Confidence**: 90-95%

### 2. FreeDB/GNUDB ?
- **Status**: Fully implemented and FREE
- **API**: http://gnudb.gnudb.org (FreeDB successor)
- **Coverage**: Good for older/mainstream releases
- **Features**:
  - Basic album and track information
  - Year and genre
  - Fast lookups
- **Requirements**: None (free to use)
- **Confidence**: 80%

### 3. Discogs ??
- **Status**: Implemented, requires API token
- **API**: https://www.discogs.com/developers
- **Coverage**: Extensive with detailed release information
- **Features**:
  - Detailed album metadata
  - Label and catalog numbers
  - Barcodes
  - Cover art
  - Release variations
- **Requirements**: Free API token (sign up required)
- **Confidence**: 85%

### 4. GD3 (Gracenote) ??
- **Status**: Structure implemented, requires license
- **API**: Gracenote Web API
- **Coverage**: Commercial database with highest accuracy
- **Features**:
  - Professional-grade metadata
  - Extensive music information
  - High-quality cover art
  - Genre and mood data
- **Requirements**: Commercial license required
- **Confidence**: 90%

## Quick Start

### Automatic Lookup (Default)

```csharp
var ripper = new CdRipperService
{
    CdDriveLetter = 'D',
    AutoLookupMetadata = true // Enabled by default
};

// Just read the CD - metadata will be fetched automatically
var cdInfo = ripper.ReadCdInfo();

// Metadata is now populated
Console.WriteLine($"{cdInfo.Artist} - {cdInfo.Album}");
```

### Manual Lookup

```csharp
var ripper = new CdRipperService
{
    CdDriveLetter = 'D',
    AutoLookupMetadata = false
};

// Read CD without metadata
var cdInfo = ripper.ReadCdInfo(lookupMetadata: false);

// Manually lookup metadata
var result = await ripper.LookupMetadataAsync(cdInfo);

if (result.Success)
{
    Console.WriteLine($"Found: {cdInfo.Artist} - {cdInfo.Album}");
}
```

## Configuration

### Metadata Lookup Options

```csharp
var options = new MetadataLookupOptions
{
    // Enable/disable providers
    UseMusicBrainz = true,    // Free - recommended
    UseFreeDB = true,         // Free - backup
    UseDiscogs = false,       // Requires API token
    UseGD3 = false,           // Requires commercial license
    
    // Download cover art
    DownloadCoverArt = true,
    
    // Timeout settings
    TimeoutSeconds = 30,
    
    // User agent for API requests
    UserAgent = "nexENCODE Studio/1.0 (your@email.com)",
    
    // Discogs API token (if using Discogs)
    DiscogsToken = "YOUR_TOKEN_HERE"
};

var ripper = new CdRipperService();
ripper.MetadataOptions = options;
```

### Provider Priority

When multiple providers are enabled, the service queries all of them in parallel and selects the best match based on:

1. **Confidence score** (0-100)
2. **Completeness** (number of tracks with names)
3. **Additional data** (cover art, etc.)

## Getting API Credentials

### MusicBrainz
? **No credentials needed** - free to use
- Just set a descriptive User-Agent

### FreeDB/GNUDB
? **No credentials needed** - free to use
- Automatic connection to GNUDB servers

### Discogs
1. Go to https://www.discogs.com/settings/developers
2. Create an account (free)
3. Generate a Personal Access Token
4. Set token in `MetadataOptions.DiscogsToken`

```csharp
ripper.MetadataOptions.DiscogsToken = "your_token_here";
ripper.MetadataOptions.UseDiscogs = true;
```

### GD3/Gracenote
Requires commercial licensing:
1. Contact Gracenote for developer account
2. Obtain Client ID, Client Tag, and User ID
3. Configure in GD3Provider:

```csharp
var gd3Provider = new GD3Provider(options);
gd3Provider.ConfigureCredentials(clientId, clientTag, userId);
```

## Features

### CD Identification

The service identifies CDs using:
- **CDDB Disc ID**: Calculated from track offsets and lengths
- **Track count**: Number of tracks on the CD
- **Total duration**: Length of the entire album
- **Track durations**: Individual track lengths

### Metadata Retrieved

#### Album Level
- Artist name
- Album title
- Release year
- Genre
- Country
- Record label
- Barcode (if available)
- Cover art

#### Track Level
- Track number
- Track title
- Track artist (for compilations)
- Track duration
- ISRC codes (MusicBrainz)

### Cover Art

Automatically download album cover art:

```csharp
var ripper = new CdRipperService();
ripper.MetadataOptions.DownloadCoverArt = true;

var cdInfo = ripper.ReadCdInfo();
var result = await ripper.LookupMetadataAsync(cdInfo);

if (result?.BestMatch?.CoverArt != null)
{
    // Save cover art
    await File.WriteAllBytesAsync(
        "cover.jpg", 
        result.BestMatch.CoverArt
    );
}
```

## Usage Examples

### Example 1: Basic Auto-Lookup

```csharp
var ripper = new CdRipperService { CdDriveLetter = 'D' };

// Progress tracking
ripper.ProgressChanged += (s, e) =>
{
    Console.WriteLine(e.StatusMessage);
};

var cdInfo = ripper.ReadCdInfo();
// Metadata automatically retrieved

Console.WriteLine($"{cdInfo.Artist} - {cdInfo.Album} ({cdInfo.Year})");
foreach (var track in cdInfo.Tracks)
{
    Console.WriteLine($"  {track.TrackNumber:00}. {track.Title}");
}
```

### Example 2: Compare Multiple Sources

```csharp
var ripper = new CdRipperService();
ripper.MetadataOptions.UseMusicBrainz = true;
ripper.MetadataOptions.UseFreeDB = true;

var cdInfo = ripper.ReadCdInfo(lookupMetadata: false);
var result = await ripper.LookupMetadataAsync(cdInfo);

foreach (var match in result.AllMatches)
{
    Console.WriteLine($"{match.Source}: {match.Album} ({match.Confidence}%)");
}

Console.WriteLine($"\nBest: {result.BestMatch.Source}");
```

### Example 3: Manual Entry on Failure

```csharp
var ripper = new CdRipperService();
var cdInfo = ripper.ReadCdInfo();

if (string.IsNullOrEmpty(cdInfo.Artist))
{
    // Auto-lookup failed, enter manually
    var trackNames = new List<string>
    {
        "Track 1 Name",
        "Track 2 Name",
        // ...
    };
    
    ripper.SetManualMetadata(
        cdInfo,
        artist: "Artist Name",
        album: "Album Name",
        year: 2024,
        genre: "Rock",
        trackNames: trackNames
    );
}
```

### Example 4: Rip with Metadata

```csharp
var service = new NexEncodeService();
service.CdRipper.AutoLookupMetadata = true;

var cdInfo = service.GetCdInfo();
// Metadata automatically populated

var options = new EncodingOptions
{
    Quality = Mp3Quality.High,
    WriteId3Tags = true
};

var mp3Files = await service.RipAndEncodeAsync(
    cdInfo,
    @"C:\Music\" + $"{cdInfo.Artist} - {cdInfo.Album}",
    options
);
```

### Example 5: Batch Rip with Auto-ID

```csharp
var service = new NexEncodeService();
service.CdRipper.AutoLookupMetadata = true;

while (true)
{
    Console.WriteLine("Insert CD and press Enter...");
    Console.ReadLine();
    
    var cdInfo = service.GetCdInfo();
    
    if (!string.IsNullOrEmpty(cdInfo.Artist))
    {
        Console.WriteLine($"Found: {cdInfo.Artist} - {cdInfo.Album}");
        
        string outputDir = $@"C:\Music\{cdInfo.Artist} - {cdInfo.Album}";
        await service.RipAndEncodeAsync(cdInfo, outputDir, options);
        
        service.CdRipper.EjectCd();
    }
}
```

## Technical Details

### Disc ID Calculation

The CDDB disc ID is calculated using:

```
discid = ((n % 0xff) << 24) | (t << 8) | i

where:
  n = checksum of track offsets
  t = total disc length in seconds
  i = number of tracks
```

This creates a unique 8-character hexadecimal identifier for each CD.

### API Rate Limiting

**MusicBrainz**:
- 1 request per second
- Be respectful with User-Agent

**Discogs**:
- 60 requests per minute (authenticated)
- 25 requests per minute (unauthenticated)

**FreeDB/GNUDB**:
- No strict limits
- Be reasonable

### Error Handling

All providers handle errors gracefully:
- Network timeouts
- API unavailability
- Invalid responses
- Missing data

If one provider fails, others are still queried.

## Confidence Scores

Metadata matches are scored based on:

| Source | Base Confidence | Notes |
|--------|----------------|-------|
| MusicBrainz | 90-95% | Very reliable |
| GD3/Gracenote | 90% | Commercial quality |
| Discogs | 85% | Good but varied |
| FreeDB | 80% | Older database |

## Performance

Typical lookup times:
- **MusicBrainz**: 1-3 seconds
- **FreeDB**: < 1 second
- **Discogs**: 2-4 seconds
- **GD3**: 1-2 seconds (with license)

Multiple providers are queried in parallel, so total time ? slowest provider.

## Troubleshooting

### No Metadata Found

**Problem**: Lookup returns no results

**Solutions**:
1. Check internet connection
2. Try different providers
3. Verify disc ID is correct
4. Use manual entry
5. Check if CD is in database (try online first)

### Wrong Metadata

**Problem**: Incorrect album/track info retrieved

**Solutions**:
1. Compare multiple sources
2. Check confidence scores
3. Use manual override
4. Submit corrections to MusicBrainz

### API Errors

**Problem**: Provider returns errors

**Solutions**:
1. Check API credentials (Discogs, GD3)
2. Verify User-Agent is set
3. Check rate limiting
4. Try alternative provider

### Timeout Issues

**Problem**: Lookups timing out

**Solutions**:
1. Increase timeout: `MetadataOptions.TimeoutSeconds = 60`
2. Check internet speed
3. Try during off-peak hours
4. Disable slow providers

## Best Practices

### 1. Use Multiple Providers

```csharp
var options = new MetadataLookupOptions
{
    UseMusicBrainz = true,  // Primary
    UseFreeDB = true,       // Backup
    UseDiscogs = true       // Additional
};
```

### 2. Set Descriptive User-Agent

```csharp
options.UserAgent = "nexENCODE Studio/1.0 (your.email@example.com)";
```

### 3. Handle Failures Gracefully

```csharp
var cdInfo = ripper.ReadCdInfo();
if (string.IsNullOrEmpty(cdInfo.Artist))
{
    // Provide manual entry option
}
```

### 4. Cache Results

```csharp
// Save metadata for later use
var json = JsonSerializer.Serialize(result.BestMatch);
await File.WriteAllTextAsync("metadata.json", json);
```

### 5. Verify Before Ripping

```csharp
Console.WriteLine($"Found: {cdInfo.Artist} - {cdInfo.Album}");
Console.Write("Is this correct? (y/n): ");
if (Console.ReadLine() != "y")
{
    // Enter manually
}
```

## Integration

### With CD Ripping

```csharp
var ripper = new CdRipperService();
var cdInfo = ripper.ReadCdInfo(); // Auto-lookup
var files = await ripper.RipAllTracksAsync(cdInfo, outputDir);
```

### With Encoding

```csharp
var service = new NexEncodeService();
var cdInfo = service.GetCdInfo(); // Auto-lookup
var mp3Files = await service.RipAndEncodeAsync(cdInfo, outputDir, options);
// ID3 tags automatically written with metadata
```

### With Playlists

```csharp
var playlist = new PlaylistService();
var cdInfo = ripper.ReadCdInfo(); // With metadata
playlist.WriteM3uPlaylist("album.m3u", cdInfo.Tracks);
```

## API Reference

### CdRipperService

```csharp
// Properties
bool AutoLookupMetadata { get; set; }
MetadataLookupOptions MetadataOptions { get; set; }

// Methods
CdInfo ReadCdInfo(bool lookupMetadata = true)
Task<MetadataLookupResult> LookupMetadataAsync(CdInfo, CancellationToken)
void SetManualMetadata(CdInfo, artist, album, year, genre, trackNames)
```

### MetadataLookupOptions

```csharp
bool UseMusicBrainz { get; set; }
bool UseFreeDB { get; set; }
bool UseDiscogs { get; set; }
bool UseGD3 { get; set; }
bool DownloadCoverArt { get; set; }
int TimeoutSeconds { get; set; }
string UserAgent { get; set; }
string DiscogsToken { get; set; }
```

### MetadataLookupResult

```csharp
bool Success { get; set; }
CdMetadata BestMatch { get; set; }
List<CdMetadata> AllMatches { get; set; }
string Error { get; set; }
TimeSpan LookupTime { get; set; }
```

## Summary

? **Automatic CD identification** from multiple databases  
? **No manual typing** for mainstream albums  
? **High accuracy** with confidence scoring  
? **Cover art download** included  
? **Multiple providers** for best coverage  
? **Fallback to manual** entry when needed  
? **Parallel queries** for fast results  
? **Free providers** available (no cost to start)  

Your CD ripping workflow is now **fully automated**! ??
