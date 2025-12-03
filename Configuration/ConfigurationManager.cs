using nexENCODE_Studio.Models;
using System.Text.Json;

namespace nexENCODE_Studio.Configuration
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppSettings
    {
        public char DefaultCdDrive { get; set; } = 'D';
        public string DefaultOutputDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public EncodingOptions DefaultEncodingOptions { get; set; } = new()
        {
            Format = AudioFormat.Mp3,
            Quality = Mp3Quality.High,
            SampleRate = 44100,
            Channels = 2,
            WriteId3Tags = true
        };
        public float DefaultVolume { get; set; } = 0.8f;
        public bool AutoCreatePlaylists { get; set; } = true;
        public bool DeleteTempFilesAfterEncode { get; set; } = true;
        public string TempDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "nexENCODE");
    }
    
    /// <summary>
    /// Manages application configuration and settings
    /// </summary>
    public class ConfigurationManager
    {
        private static readonly string ConfigFileName = "nexencode.config.json";
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "nexENCODE Studio");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, ConfigFileName);
        
        private AppSettings _settings;
        
        public AppSettings Settings => _settings;
        
        public ConfigurationManager()
        {
            _settings = LoadSettings();
        }
        
        /// <summary>
        /// Loads settings from file or creates default settings
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new AppSettings();
        }
        
        /// <summary>
        /// Saves current settings to file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(ConfigDirectory);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = new AppSettings();
            SaveSettings();
        }
        
        /// <summary>
        /// Updates a specific setting
        /// </summary>
        public void UpdateSetting(Action<AppSettings> updateAction)
        {
            updateAction(_settings);
            SaveSettings();
        }
        
        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        public static string GetConfigFilePath() => ConfigFilePath;
        
        /// <summary>
        /// Ensures the temp directory exists and is ready to use
        /// </summary>
        public void EnsureTempDirectory()
        {
            if (!Directory.Exists(_settings.TempDirectory))
            {
                Directory.CreateDirectory(_settings.TempDirectory);
            }
        }
        
        /// <summary>
        /// Cleans up the temp directory
        /// </summary>
        public void CleanupTempDirectory()
        {
            try
            {
                if (Directory.Exists(_settings.TempDirectory))
                {
                    var files = Directory.GetFiles(_settings.TempDirectory);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore individual file deletion errors
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning temp directory: {ex.Message}");
            }
        }
    }
}
