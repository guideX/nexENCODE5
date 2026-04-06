using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace nexENCODE_Studio.Skinning
{
    public sealed class SkinApplicator
    {
        private readonly Dictionary<string, Control> _namedControls = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, Control> NamedControls => _namedControls;
        public Label? StatusLabel { get; private set; }

        public void Apply(Form form, SkinDefinition skin, Action<SkinObjectDefinition>? onButtonClick = null)
        {
            ApplyFormSurface(form, skin);
            ApplyShape(form, skin);
            ApplyObjects(form, skin, onButtonClick);
        }

        public void SetStatusText(string text)
        {
            if (StatusLabel != null)
            {
                StatusLabel.Text = text;
            }
        }

        private void ApplyFormSurface(Form form, SkinDefinition skin)
        {
            form.SuspendLayout();
            form.Text = string.IsNullOrWhiteSpace(skin.Name) ? form.Text : skin.Name;
            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new Size(skin.Width, skin.Height);
            form.BackgroundImageLayout = ImageLayout.None;

            if (File.Exists(skin.MainWindowBackgroundImage))
            {
                form.BackgroundImage = LoadImageFromFile(skin.MainWindowBackgroundImage);
            }

            if (File.Exists(skin.IconPath))
            {
                using var iconStream = File.OpenRead(skin.IconPath);
                form.Icon = new Icon(iconStream);
            }
        }

        private void ApplyShape(Form form, SkinDefinition skin)
        {
            if (!skin.MainWindowSetShape || skin.Shapes.Count == 0)
            {
                return;
            }

            var regions = new Region?[skin.Shapes.Count + 1];

            for (var index = 1; index <= skin.Shapes.Count; index++)
            {
                var shape = skin.Shapes[index - 1];
                if (!shape.Enabled)
                {
                    continue;
                }

                regions[index] = CreateRegion(shape);
            }

            if (skin.ShapeSettings.Combine)
            {
                for (var index = 1; index <= skin.Shapes.Count; index++)
                {
                    var shape = skin.Shapes[index - 1];
                    if (shape.CombineMode == SkinCombineMode.None || shape.DestRgn <= 0 || shape.SrcRgn1 <= 0 || shape.SrcRgn2 <= 0)
                    {
                        continue;
                    }

                    if (shape.DestRgn >= regions.Length || shape.SrcRgn1 >= regions.Length || shape.SrcRgn2 >= regions.Length)
                    {
                        continue;
                    }

                    var source1 = regions[shape.SrcRgn1];
                    var source2 = regions[shape.SrcRgn2];
                    if (source1 == null || source2 == null)
                    {
                        continue;
                    }

                    var combined = source1.Clone();
                    switch (shape.CombineMode)
                    {
                        case SkinCombineMode.And:
                            combined.Intersect(source2);
                            break;
                        case SkinCombineMode.Or:
                            combined.Union(source2);
                            break;
                        case SkinCombineMode.Xor:
                            combined.Xor(source2);
                            break;
                        case SkinCombineMode.Diff:
                            combined.Exclude(source2);
                            break;
                        case SkinCombineMode.Copy:
                            combined.Dispose();
                            combined = source2.Clone();
                            break;
                    }

                    if (shape.DestRgn != shape.SrcRgn1 && shape.DestRgn != shape.SrcRgn2)
                    {
                        regions[shape.DestRgn]?.Dispose();
                    }

                    regions[shape.DestRgn] = combined;
                }
            }

            var parentIndex = skin.ShapeSettings.ParentShapeRegion;
            if (parentIndex > 0 && parentIndex < regions.Length && regions[parentIndex] != null)
            {
                form.Region = regions[parentIndex]!.Clone();
            }

            for (var i = 1; i < regions.Length; i++)
            {
                regions[i]?.Dispose();
            }
        }

        private void ApplyObjects(Form form, SkinDefinition skin, Action<SkinObjectDefinition>? onButtonClick)
        {
            foreach (var control in _namedControls.Values)
            {
                form.Controls.Remove(control);
                control.Dispose();
            }

            _namedControls.Clear();
            StatusLabel = null;

            foreach (var obj in skin.Objects)
            {
                switch (obj.ObjectType)
                {
                    case SkinObjectType.ImageButton:
                        CreateImageButton(form, obj, onButtonClick);
                        break;
                    case SkinObjectType.StatusLabel:
                        CreateStatusLabel(form, obj);
                        break;
                }
            }

            form.ResumeLayout();
        }

        private void CreateStatusLabel(Form form, SkinObjectDefinition obj)
        {
            var label = new Label
            {
                Name = obj.Name,
                AutoSize = false,
                Width = obj.Width,
                Height = obj.Height,
                Left = obj.Left,
                Top = obj.Top,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Text = "Ready",
                TextAlign = ContentAlignment.MiddleLeft
            };

            label.MouseDown += (_, e) => BeginDrag(form, e);
            label.MouseMove += (_, e) => BeginDrag(form, e);

            StatusLabel = label;
            _namedControls[obj.Name] = label;
            form.Controls.Add(label);
            label.BringToFront();
        }

        private void CreateImageButton(Form form, SkinObjectDefinition obj, Action<SkinObjectDefinition>? onButtonClick)
        {
            var pictureBox = new PictureBox
            {
                Name = obj.Name,
                Width = obj.Width,
                Height = obj.Height,
                Left = obj.Left,
                Top = obj.Top,
                Visible = obj.Visible,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Cursor = Cursors.Hand,
                Tag = new SkinnedButtonTag
                {
                    Name = obj.Name,
                    ButtonType = obj.ButtonType,
                    ImageNormalPath = obj.FileName1,
                    ImageDownPath = obj.FileName2,
                    ImageHoverPath = obj.FileName3,
                    OnClick = obj.OnClick
                }
            };

            SetPictureImage(pictureBox, obj.FileName1);
            pictureBox.MouseEnter += (_, _) => SetPictureImage(pictureBox, obj.FileName3);
            pictureBox.MouseLeave += (_, _) => SetPictureImage(pictureBox, obj.FileName1);
            pictureBox.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    SetPictureImage(pictureBox, obj.FileName2);
                }
            };
            pictureBox.MouseUp += (_, e) =>
            {
                SetPictureImage(pictureBox, obj.FileName1);
                if (e.Button == MouseButtons.Left)
                {
                    onButtonClick?.Invoke(obj);
                }
            };

            _namedControls[obj.Name] = pictureBox;
            form.Controls.Add(pictureBox);
            pictureBox.BringToFront();
        }

        private static void SetPictureImage(PictureBox pictureBox, string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var oldImage = pictureBox.Image;
            pictureBox.Image = LoadImageFromFile(path);
            oldImage?.Dispose();
        }

        private static Image LoadImageFromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            return Image.FromStream(ms);
        }

        private static Region CreateRegion(SkinShapeDefinition shape)
        {
            var width = Math.Max(1, shape.X2 - shape.X1);
            var height = Math.Max(1, shape.Y2 - shape.Y1);
            var rectangle = new Rectangle(shape.X1, shape.Y1, width, height);

            return shape.Type switch
            {
                SkinShapeType.Ellipse => CreateEllipseRegion(rectangle),
                SkinShapeType.RoundRectangle => CreateRoundedRegion(rectangle, Math.Max(1, shape.X3 / 2), Math.Max(1, shape.Y3 / 2)),
                _ => new Region(rectangle)
            };
        }

        private static Region CreateEllipseRegion(Rectangle rectangle)
        {
            var path = new GraphicsPath();
            path.AddEllipse(rectangle);
            return new Region(path);
        }

        private static Region CreateRoundedRegion(Rectangle rectangle, int radiusX, int radiusY)
        {
            var path = new GraphicsPath();
            var diameterX = Math.Min(rectangle.Width, radiusX * 2);
            var diameterY = Math.Min(rectangle.Height, radiusY * 2);

            path.AddArc(rectangle.Left, rectangle.Top, diameterX, diameterY, 180, 90);
            path.AddArc(rectangle.Right - diameterX, rectangle.Top, diameterX, diameterY, 270, 90);
            path.AddArc(rectangle.Right - diameterX, rectangle.Bottom - diameterY, diameterX, diameterY, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameterY, diameterX, diameterY, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }

        private static void BeginDrag(Form form, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(form.Handle, 0xA1, 0x2, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);
    }
}
