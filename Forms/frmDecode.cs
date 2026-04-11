using System;
using System.Windows.Forms;

namespace nexENCODE_Studio.Forms
{
    public class frmDecode : Form
    {
        private readonly Button _decodeButton;

        public frmDecode()
        {
            Text = "Decode";
            Width = 400;
            Height = 200;

            _decodeButton = new Button { Text = "Start Decode", Dock = DockStyle.Fill };
            _decodeButton.Click += (_, _) => StartDecode();

            Controls.Add(_decodeButton);
        }

        public void StartDecode()
        {
            MessageBox.Show(this, "Starting decode...", "Decode", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
