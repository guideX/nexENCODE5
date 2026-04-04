using System.Runtime.InteropServices;
using nexENCODE_Studio.Skinning;

namespace nexENCODE_Studio
{
    public partial class frmMain : Form
    {
        private readonly SkinLoader _skinLoader = new();
        private readonly SkinApplicator _skinApplicator = new();

        public frmMain()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();
            Load += frmMain_Load;
            MouseDown += frmMain_MouseDown;
        }

        private void frmMain_Load(object? sender, EventArgs e)
        {
            try
            {
                LoadDefaultSkin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Skin Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDefaultSkin()
        {
            var skinPath = Path.Combine(AppContext.BaseDirectory, "default", "default.ini");
            if (!File.Exists(skinPath))
            {
                Text = "nexENCODE Studio";
                return;
            }

            var skin = _skinLoader.Load(skinPath);
            _skinApplicator.Apply(this, skin, HandleSkinButtonClick);
            _skinApplicator.SetStatusText("Skin loaded.");
        }

        private void HandleSkinButtonClick(SkinObjectDefinition button)
        {
            switch (button.OnClick)
            {
                case "Exit_Click":
                    Close();
                    break;
                case "MinimizeButton_Click":
                    WindowState = FormWindowState.Minimized;
                    break;
                case "Maximize_Click":
                    WindowState = WindowState == FormWindowState.Maximized
                        ? FormWindowState.Normal
                        : FormWindowState.Maximized;
                    break;
                default:
                    _skinApplicator.SetStatusText($"{button.Name} pressed ({button.OnClick}).");
                    break;
            }
        }

        private void frmMain_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);
    }
}
