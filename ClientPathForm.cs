using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace RiotSwitcherMinimal
{
    public class ClientPathForm : Form
    {
        private TextBox _pathTextBox;
        public string SelectedPath { get; private set; }

        public ClientPathForm(string currentPath)
        {
            SelectedPath = currentPath;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Riot Client Directory";
            this.Size = new Size(500, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var label = new Label
            {
                Text = "Riot Client Location (Folder containing RiotClientServices.exe):",
                Location = new Point(12, 15),
                AutoSize = true
            };
            this.Controls.Add(label);

            _pathTextBox = new TextBox
            {
                Location = new Point(12, 40),
                Width = 360,
                Text = SelectedPath,
                ReadOnly = true
            };
            this.Controls.Add(_pathTextBox);

            var browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(380, 39),
                Width = 90
            };
            browseButton.Click += Browse_Click;
            this.Controls.Add(browseButton);

            var saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new Point(150, 80),
                Width = 90
            };
            // Validation on save
            saveButton.Click += (s, e) =>
            {
                if (ValidatePath(_pathTextBox.Text))
                {
                    SelectedPath = _pathTextBox.Text;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("RiotClientServices.exe not found in the selected folder!", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Prevent closing by setting DialogResult to None if we could intercept, 
                    // but with Button.DialogResult property set, it closes automatically.
                    // So we'll handle validation differently or reset DialogResult.
                    this.DialogResult = DialogResult.None; 
                }
            };
            this.Controls.Add(saveButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(250, 80),
                Width = 90
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the folder containing RiotClientServices.exe";
                fbd.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(_pathTextBox.Text) && Directory.Exists(_pathTextBox.Text))
                {
                    fbd.SelectedPath = _pathTextBox.Text;
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _pathTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return File.Exists(Path.Combine(path, "RiotClientServices.exe"));
        }
    }
}
