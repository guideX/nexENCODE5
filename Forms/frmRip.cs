using System;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using nexENCODE_Studio.Models;
using nexENCODE_Studio.Services;

namespace nexENCODE_Studio.Forms
{
    public class frmRip : Form
    {
        private readonly ComboBox _driveCombo;
        private readonly Button _refreshButton;
        private readonly ListBox _tracksList;
        private readonly Button _ripSelectedButton;
        private readonly Button _ripAllButton;
        private readonly CheckBox _encodeCheck;
        private readonly TextBox _outputPathBox;
        private readonly Button _browseButton;
        private readonly ProgressBar _progressBar;
        private readonly Label _statusLabel;

        private readonly CdRipperService _ripper = new();
        private readonly AudioEncoderService _encoder = new();
        private CdInfo? _currentCd;
        private CancellationTokenSource? _cts;

        public frmRip()
        {
            Text = "Rip CD";
            Width = 600;
            Height = 420;

            _driveCombo = new ComboBox { Left = 10, Top = 10, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _refreshButton = new Button { Text = "Refresh", Left = 140, Top = 8, Width = 80 };
            _refreshButton.Click += (_, _) => LoadDrives();

            _tracksList = new ListBox { Left = 10, Top = 40, Width = 560, Height = 220 }; 

            _ripSelectedButton = new Button { Text = "Rip Selected", Left = 10, Top = 270, Width = 120 };
            _ripSelectedButton.Click += async (_, _) => await RipSelectedAsync();

            _ripAllButton = new Button { Text = "Rip All", Left = 140, Top = 270, Width = 120 };
            _ripAllButton.Click += async (_, _) => await RipAllAsync();

            _encodeCheck = new CheckBox { Text = "Encode to MP3 after ripping", Left = 280, Top = 275, Width = 220 };

            _outputPathBox = new TextBox { Left = 10, Top = 310, Width = 480 };
            _browseButton = new Button { Text = "Browse...", Left = 500, Top = 308, Width = 70 };
            _browseButton.Click += (_, _) => BrowseOutputFolder();

            _progressBar = new ProgressBar { Left = 10, Top = 350, Width = 560, Height = 20 };
            _statusLabel = new Label { Left = 10, Top = 380, Width = 560, Height = 20, Text = "Ready" };

            Controls.Add(_driveCombo);
            Controls.Add(_refreshButton);
            Controls.Add(_tracksList);
            Controls.Add(_ripSelectedButton);
            Controls.Add(_ripAllButton);
            Controls.Add(_encodeCheck);
            Controls.Add(_outputPathBox);
            Controls.Add(_browseButton);
            Controls.Add(_progressBar);
            Controls.Add(_statusLabel);

            Load += async (_, _) => await OnLoadAsync();

            _ripper.ProgressChanged += Ripper_ProgressChanged;
            _encoder.ProgressChanged += Encoder_ProgressChanged;
        }

        private void Ripper_ProgressChanged(object? sender, ProgressEventArgs e)
        {
            InvokeIfRequired(() =>
            {
                _progressBar.Value = Math.Clamp(e.PercentComplete, 0, 100);
                _statusLabel.Text = e.StatusMessage ?? e.CurrentOperation ?? "";
            });
        }

        private void Encoder_ProgressChanged(object? sender, ProgressEventArgs e)
        {
            InvokeIfRequired(() =>
            {
                _progressBar.Value = Math.Clamp(e.PercentComplete, 0, 100);
                _statusLabel.Text = e.StatusMessage ?? e.CurrentOperation ?? "";
            });
        }

        private void InvokeIfRequired(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }

        private async Task OnLoadAsync()
        {
            LoadDrives();
            _outputPathBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "nexENCODE");

            // Try to read CD info if present
            await Task.Delay(100);
            await ReadCdInfoAsync();
        }

        private void LoadDrives()
        {
            _driveCombo.Items.Clear();
            var drives = _ripper.GetAvailableCdDrives();
            foreach (var d in drives) _driveCombo.Items.Add(d.ToString() + ":\\");
            if (_driveCombo.Items.Count > 0) _driveCombo.SelectedIndex = 0;
        }

        private async Task ReadCdInfoAsync()
        {
            try
            {
                char drive = _driveCombo.SelectedItem != null ? _driveCombo.SelectedItem.ToString()![0] : 'D';
                _ripper.CdDriveLetter = drive;
                _currentCd = await Task.Run(() => _ripper.ReadCdInfo(true));

                _tracksList.Items.Clear();
                foreach (var t in _currentCd.Tracks)
                {
                    var dur = t.Duration.ToString(@"mm\:ss");
                    _tracksList.Items.Add($"{t.TrackNumber:00}. {t.Title} ({dur})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Read CD Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseOutputFolder()
        {
            using var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = _outputPathBox.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _outputPathBox.Text = dlg.SelectedPath;
            }
        }

        private async Task RipSelectedAsync()
        {
            if (_currentCd == null || _tracksList.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Select a track first.", "No Track", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var track = _currentCd.Tracks[_tracksList.SelectedIndex];
            var outputDir = _outputPathBox.Text;
            Directory.CreateDirectory(outputDir);

            _cts = new CancellationTokenSource();
            try
            {
                var wav = await _ripper.RipTrackToWavAsync(track, outputDir, _cts.Token);
                if (_encodeCheck.Checked)
                {
                    var options = new EncodingOptions { Format = AudioFormat.Mp3, OutputDirectory = outputDir };
                    await _encoder.ConvertWavToMp3Async(wav, track, options, _cts.Token);
                }

                MessageBox.Show(this, "Rip complete.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Rip Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task RipAllAsync()
        {
            if (_currentCd == null)
            {
                MessageBox.Show(this, "No CD information available.", "No CD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var outputDir = _outputPathBox.Text;
            Directory.CreateDirectory(outputDir);

            _cts = new CancellationTokenSource();
            try
            {
                var wavFiles = await _ripper.RipAllTracksAsync(_currentCd, outputDir, _cts.Token);
                if (_encodeCheck.Checked && wavFiles.Count > 0)
                {
                    var options = new EncodingOptions { Format = AudioFormat.Mp3, OutputDirectory = outputDir };
                    await _encoder.BatchConvertWavToMp3Async(wavFiles, _currentCd.Tracks, options, _cts.Token);
                }

                MessageBox.Show(this, "All tracks processed.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Rip Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
