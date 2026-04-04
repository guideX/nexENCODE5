namespace nexENCODE_Studio.Skinning
{
    public sealed class SkinLoader
    {
        public SkinDefinition Load(string skinIniPath)
        {
            var fullSkinIniPath = Path.GetFullPath(skinIniPath);
            var skinDirectory = Path.GetDirectoryName(fullSkinIniPath) ?? throw new InvalidOperationException("Skin directory could not be determined.");
            var skinIni = new IniFileParser(fullSkinIniPath);

            var definition = new SkinDefinition
            {
                SkinDirectory = skinDirectory,
                Name = skinIni.GetString("Settings", "Name", Path.GetFileNameWithoutExtension(fullSkinIniPath)),
                MainWindowShapeFileName = ExpandPath(skinIni.GetString("Settings", "MainWindow_ShapeFileName"), skinDirectory),
                MainWindowObjectFileName = ExpandPath(skinIni.GetString("Settings", "MainWindow_ObjectFileName"), skinDirectory),
                MainWindowBackgroundImage = ExpandPath(skinIni.GetString("Settings", "MainWindow_BackgroundImage"), skinDirectory),
                MainWindowCodeFile = ExpandPath(skinIni.GetString("Settings", "MainWindow_CodeFile"), skinDirectory),
                IconPath = ExpandPath(skinIni.GetString("Settings", "Icon"), skinDirectory),
                MainWindowSetShape = skinIni.GetBool("Settings", "MainWindow_SetShape"),
                Width = skinIni.GetInt("Settings", "Width", 800),
                Height = skinIni.GetInt("Settings", "Height", 450)
            };

            if (File.Exists(definition.MainWindowObjectFileName))
            {
                LoadObjects(definition, definition.MainWindowObjectFileName, skinDirectory);
            }

            if (File.Exists(definition.MainWindowShapeFileName))
            {
                LoadShapes(definition, definition.MainWindowShapeFileName);
            }

            return definition;
        }

        private static void LoadObjects(SkinDefinition definition, string objectFilePath, string skinDirectory)
        {
            var ini = new IniFileParser(objectFilePath);
            var count = ini.GetInt("Settings", "Count", 0);

            for (var i = 1; i <= count; i++)
            {
                var section = i.ToString();
                var name = ini.GetString(section, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                definition.Objects.Add(new SkinObjectDefinition
                {
                    Name = name,
                    ObjectType = (SkinObjectType)ini.GetInt(section, "objecttype", 0),
                    ButtonType = ini.GetInt(section, "buttontype", 0),
                    LabelType = ini.GetInt(section, "labeltype", 0),
                    FileName1 = ExpandPath(ini.GetString(section, "filename"), skinDirectory),
                    FileName2 = ExpandPath(ini.GetString(section, "filename2"), skinDirectory),
                    FileName3 = ExpandPath(ini.GetString(section, "filename3"), skinDirectory),
                    Left = ini.GetInt(section, "left", 0),
                    Top = ini.GetInt(section, "top", 0),
                    Width = ini.GetInt(section, "width", 0),
                    Height = ini.GetInt(section, "height", 0),
                    Visible = ini.GetBool(section, "visible", true),
                    OnClick = ini.GetString(section, "onclick")
                });
            }
        }

        private static void LoadShapes(SkinDefinition definition, string shapeFilePath)
        {
            var ini = new IniFileParser(shapeFilePath);
            definition.ShapeSettings = new SkinShapeSettings
            {
                Count = ini.GetInt("Settings", "Count", 0),
                ParentShapeRegion = ini.GetInt("Settings", "ParentShapeRegion", 0),
                Combine = ini.GetBool("Settings", "Combine", true),
                UseWindowMetrics = ini.GetBool("Settings", "UseWindowMetrics", false)
            };

            for (var i = 1; i <= definition.ShapeSettings.Count; i++)
            {
                var section = i.ToString();
                definition.Shapes.Add(new SkinShapeDefinition
                {
                    Name = ini.GetString(section, "name"),
                    Enabled = ini.GetBool(section, "enabled", true),
                    Type = (SkinShapeType)ini.GetInt(section, "type", 1),
                    X1 = ini.GetInt(section, "x1", 0),
                    Y1 = ini.GetInt(section, "y1", 0),
                    X2 = ini.GetInt(section, "x2", 0),
                    Y2 = ini.GetInt(section, "y2", 0),
                    X3 = ini.GetInt(section, "x3", 0),
                    Y3 = ini.GetInt(section, "y3", 0),
                    DestRgn = ini.GetInt(section, "destrgn", 0),
                    SrcRgn1 = ini.GetInt(section, "srcrgn1", 0),
                    SrcRgn2 = ini.GetInt(section, "srcrgn2", 0),
                    CombineMode = (SkinCombineMode)ini.GetInt(section, "combinemode", 0)
                });
            }
        }

        private static string ExpandPath(string value, string skinDirectory)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var expanded = value.Replace("$skinpath", skinDirectory, StringComparison.OrdinalIgnoreCase);
            return Path.GetFullPath(expanded);
        }
    }
}
