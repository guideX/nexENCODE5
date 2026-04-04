namespace nexENCODE_Studio.Skinning
{
    internal sealed class IniFileParser
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

        public IniFileParser(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("INI file not found.", filePath);
            }

            Parse(File.ReadAllLines(filePath));
        }

        public string GetString(string section, string key, string defaultValue = "")
        {
            if (_sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            return int.TryParse(GetString(section, key), out var value) ? value : defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            return bool.TryParse(GetString(section, key), out var value) ? value : defaultValue;
        }

        private void Parse(IEnumerable<string> lines)
        {
            string currentSection = string.Empty;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith(";"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1].Trim();
                    if (!_sections.ContainsKey(currentSection))
                    {
                        _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                var equalsIndex = line.IndexOf('=');
                if (equalsIndex <= 0 || string.IsNullOrEmpty(currentSection))
                {
                    continue;
                }

                var key = line[..equalsIndex].Trim();
                var value = line[(equalsIndex + 1)..].Trim();
                _sections[currentSection][key] = value;
            }
        }
    }
}
