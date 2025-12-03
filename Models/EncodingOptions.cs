namespace nexENCODE_Studio.Models
{
    /// <summary>
    /// Audio format enumeration
    /// </summary>
    public enum AudioFormat
    {
        Mp3,
        Wav,
        Ogg,
        Flac,
        Alac
    }
    
    /// <summary>
    /// MP3 encoding quality settings
    /// </summary>
    public enum Mp3Quality
    {
        Low = 128,           // 128 kbps
        Medium = 192,        // 192 kbps
        High = 256,          // 256 kbps
        VeryHigh = 320       // 320 kbps
    }
    
    /// <summary>
    /// Encoding configuration options
    /// </summary>
    public class EncodingOptions
    {
        public AudioFormat Format { get; set; } = AudioFormat.Mp3;
        public Mp3Quality Quality { get; set; } = Mp3Quality.High;
        public int SampleRate { get; set; } = 44100;
        public int Channels { get; set; } = 2; // Stereo
        public bool WriteId3Tags { get; set; } = true;
        public string OutputDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the bitrate as an integer
        /// </summary>
        public int GetBitrate()
        {
            return (int)Quality;
        }
    }
}
