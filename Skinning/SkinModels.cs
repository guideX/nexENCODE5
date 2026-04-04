namespace nexENCODE_Studio.Skinning
{
    public enum SkinObjectType
    {
        Unknown = 0,
        ImageButton = 1,
        StatusLabel = 2
    }

    public enum SkinShapeType
    {
        Rectangle = 1,
        Ellipse = 2,
        RoundRectangle = 3
    }

    public enum SkinCombineMode
    {
        None = 0,
        And = 1,
        Or = 2,
        Xor = 3,
        Diff = 4,
        Copy = 5
    }

    public sealed class SkinDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string SkinDirectory { get; set; } = string.Empty;
        public string MainWindowShapeFileName { get; set; } = string.Empty;
        public string MainWindowObjectFileName { get; set; } = string.Empty;
        public string MainWindowBackgroundImage { get; set; } = string.Empty;
        public string MainWindowCodeFile { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public bool MainWindowSetShape { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public SkinShapeSettings ShapeSettings { get; set; } = new();
        public List<SkinObjectDefinition> Objects { get; } = new();
        public List<SkinShapeDefinition> Shapes { get; } = new();
    }

    public sealed class SkinShapeSettings
    {
        public int Count { get; set; }
        public int ParentShapeRegion { get; set; }
        public bool Combine { get; set; }
        public bool UseWindowMetrics { get; set; }
    }

    public sealed class SkinObjectDefinition
    {
        public string Name { get; set; } = string.Empty;
        public SkinObjectType ObjectType { get; set; }
        public int ButtonType { get; set; }
        public int LabelType { get; set; }
        public string FileName1 { get; set; } = string.Empty;
        public string FileName2 { get; set; } = string.Empty;
        public string FileName3 { get; set; } = string.Empty;
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Visible { get; set; }
        public string OnClick { get; set; } = string.Empty;
    }

    public sealed class SkinShapeDefinition
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public SkinShapeType Type { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public int X3 { get; set; }
        public int Y3 { get; set; }
        public int DestRgn { get; set; }
        public int SrcRgn1 { get; set; }
        public int SrcRgn2 { get; set; }
        public SkinCombineMode CombineMode { get; set; }
    }

    public sealed class SkinnedButtonTag
    {
        public string Name { get; init; } = string.Empty;
        public int ButtonType { get; init; }
        public string ImageNormalPath { get; init; } = string.Empty;
        public string ImageDownPath { get; init; } = string.Empty;
        public string ImageHoverPath { get; init; } = string.Empty;
        public string OnClick { get; init; } = string.Empty;
    }
}
