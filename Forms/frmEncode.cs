using System;
using System.Windows.Forms;

namespace nexENCODE_Studio.Forms
{
    public class frmEncode : Form
    {
        private readonly Button _encodeButton;

        public frmEncode()
        {
            Text = "Encode";
            Width = 400;
            Height = 200;

            _encodeButton = new Button { Text = "Start Encode", Dock = DockStyle.Fill };
            _encodeButton.Click += (_, _) => StartEncode();

            Controls.Add(_encodeButton);
        }

        public void StartEncode()
        {
            MessageBox.Show(this, "Starting encode...", "Encode", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
