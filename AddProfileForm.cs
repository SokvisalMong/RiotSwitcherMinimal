using System;
using System.Drawing;
using System.Windows.Forms;

namespace RiotSwitcherMinimal
{
    public class AddProfileForm : Form
    {
        private TextBox _nameTextBox;
        private Button _createButton;
        private Button _cancelButton;
        public string ProfileName { get; private set; }

        public AddProfileForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Profile";
            this.Size = new Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var label = new Label
            {
                Text = "Profile Name:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(label);

            _nameTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Width = 240
            };
            this.Controls.Add(_nameTextBox);

            _createButton = new Button
            {
                Text = "Create",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 80),
                Width = 70
            };
            _createButton.Click += (s, e) => { ProfileName = _nameTextBox.Text; };
            this.Controls.Add(_createButton);

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(180, 80),
                Width = 70
            };
            this.Controls.Add(_cancelButton);

            this.AcceptButton = _createButton;
            this.CancelButton = _cancelButton;
        }
    }
}
