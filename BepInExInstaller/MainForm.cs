namespace BepInExInstaller
{
    public partial class MainForm : Form
    {
        private GameInfo? _currentGame;
        private bool _installing;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var folder = folderBrowserDialog.SelectedPath;
            txtFolder.Text = folder;
            AppendLog($"Scanning \"{folder}\"...");

            var info = GameDetector.Detect(folder);
            _currentGame = info;

            if (!info.IsValidUnityGame)
            {
                lblGameName.Text = "Executable: —";
                lblBackend.Text = "Scripting backend: —";
                lblArch.Text = "Architecture: —";
                btnInstall.Enabled = false;
                AppendLog(info.Error ?? "This doesn't look like a Unity game folder.");
                MessageBox.Show(this, info.Error ?? "This doesn't look like a Unity game folder.",
                    "Not a Unity game", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblGameName.Text = $"Executable: {info.ExecutableName}  ({info.DataFolderName})";
            lblBackend.Text = $"Scripting backend: {DescribeBackend(info.Backend)}";
            lblArch.Text = $"Architecture: {info.Arch}";

            AppendLog($"Found {info.ExecutableName} — backend: {info.Backend}, arch: {info.Arch}");

            if (info.Backend == UnityBackend.Unknown)
            {
                AppendLog("Could not confirm Mono vs IL2CPP automatically; will default to the Mono build.");
            }

            btnInstall.Enabled = true;
        }

        private static string DescribeBackend(UnityBackend backend) => backend switch
        {
            UnityBackend.Mono => "Mono",
            UnityBackend.IL2CPP => "IL2CPP",
            _ => "Unknown (will default to Mono)"
        };

        private async void btnInstall_Click(object? sender, EventArgs e)
        {
            if (_currentGame == null || _installing) return;

            var confirm = MessageBox.Show(this,
                $"Install BepInEx into:\n{_currentGame.FolderPath}\n\nThis will overwrite any existing BepInEx install in this folder. Continue?",
                "Confirm install", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _installing = true;
            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            progressBar.Value = 0;

            var core = new BepInExInstallerCore
            {
                Log = msg => SafeAppendLog(msg)
            };

            var progress = new Progress<double>(p =>
            {
                if (IsHandleCreated)
                    BeginInvoke(() => progressBar.Value = Math.Min(100, (int)p));
            });

            try
            {
                bool ok = await core.InstallAsync(_currentGame, progress);
                MessageBox.Show(this,
                    ok ? "BepInEx was installed successfully!" : "Install finished with problems — check the log for details.",
                    ok ? "Success" : "Warning",
                    MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                AppendLog($"Unexpected error: {ex.Message}");
                MessageBox.Show(this, $"Unexpected error:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _installing = false;
                btnInstall.Enabled = true;
                btnBrowse.Enabled = true;
            }
        }

        private void SafeAppendLog(string message)
        {
            if (IsHandleCreated)
                BeginInvoke(() => AppendLog(message));
            else
                AppendLog(message);
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
