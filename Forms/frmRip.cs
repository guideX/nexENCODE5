using System;
using System.Linq;
using System.Windows.Forms;

namespace nexENCODE_Studio.Forms
{
    public class frmRip : Form
    {
        private readonly ListBox _tracksList;
        private readonly Button _ripButton;

        public frmRip()
        {
            Text = "Rip CD";
            Width = 400;
            Height = 300;

            _tracksList = new ListBox { Dock = DockStyle.Top, Height = 200 };
            _tracksList.Items.AddRange(new object[] { "Track 01", "Track 02", "Track 03" });

            _ripButton = new Button { Text = "Rip Selected", Dock = DockStyle.Bottom, Height = 30 };
            _ripButton.Click += (_, _) => StartRip();

            Controls.Add(_tracksList);
            Controls.Add(_ripButton);
        }

        public void StartRip()
        {
            var selected = _tracksList.SelectedItem ?? _tracksList.Items.Cast<object>().FirstOrDefault();
            MessageBox.Show(this, $"Starting rip for: {selected}", "Rip", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
