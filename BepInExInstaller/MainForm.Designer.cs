namespace BepInExInstaller
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.txtFolder = new TextBox();
            this.btnBrowse = new Button();
            this.lblInfoHeader = new Label();
            this.lblGameName = new Label();
            this.lblBackend = new Label();
            this.lblArch = new Label();
            this.btnInstall = new Button();
            this.progressBar = new ProgressBar();
            this.txtLog = new TextBox();
            this.folderBrowserDialog = new FolderBrowserDialog();
            this.SuspendLayout();

            // lblTitle
            this.lblTitle.Text = "BepInEx Installer";
            this.lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblTitle.Location = new Point(20, 15);
            this.lblTitle.Size = new Size(400, 32);

            // txtFolder
            this.txtFolder.Location = new Point(20, 60);
            this.txtFolder.Size = new Size(430, 23);
            this.txtFolder.PlaceholderText = "Select your Unity game folder...";
            this.txtFolder.ReadOnly = true;

            // btnBrowse
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.Location = new Point(460, 59);
            this.btnBrowse.Size = new Size(100, 25);
            this.btnBrowse.Click += new EventHandler(this.btnBrowse_Click);

            // lblInfoHeader
            this.lblInfoHeader.Text = "Detected game info:";
            this.lblInfoHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.lblInfoHeader.Location = new Point(20, 100);
            this.lblInfoHeader.Size = new Size(200, 20);

            // lblGameName
            this.lblGameName.Text = "Executable: —";
            this.lblGameName.Location = new Point(20, 125);
            this.lblGameName.Size = new Size(540, 20);

            // lblBackend
            this.lblBackend.Text = "Scripting backend: —";
            this.lblBackend.Location = new Point(20, 148);
            this.lblBackend.Size = new Size(540, 20);

            // lblArch
            this.lblArch.Text = "Architecture: —";
            this.lblArch.Location = new Point(20, 171);
            this.lblArch.Size = new Size(540, 20);

            // btnInstall
            this.btnInstall.Text = "Install BepInEx";
            this.btnInstall.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.btnInstall.Location = new Point(20, 205);
            this.btnInstall.Size = new Size(540, 40);
            this.btnInstall.Enabled = false;
            this.btnInstall.Click += new EventHandler(this.btnInstall_Click);

            // progressBar
            this.progressBar.Location = new Point(20, 255);
            this.progressBar.Size = new Size(540, 20);

            // txtLog
            this.txtLog.Location = new Point(20, 285);
            this.txtLog.Size = new Size(540, 160);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.ReadOnly = true;
            this.txtLog.BackColor = Color.Black;
            this.txtLog.ForeColor = Color.LightGreen;
            this.txtLog.Font = new Font("Consolas", 9F);

            // MainForm
            this.ClientSize = new Size(580, 465);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.txtFolder);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lblInfoHeader);
            this.Controls.Add(this.lblGameName);
            this.Controls.Add(this.lblBackend);
            this.Controls.Add(this.lblArch);
            this.Controls.Add(this.btnInstall);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.txtLog);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "BepInEx Installer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Label lblTitle;
        private TextBox txtFolder;
        private Button btnBrowse;
        private Label lblInfoHeader;
        private Label lblGameName;
        private Label lblBackend;
        private Label lblArch;
        private Button btnInstall;
        private ProgressBar progressBar;
        private TextBox txtLog;
        private FolderBrowserDialog folderBrowserDialog;
    }
}
