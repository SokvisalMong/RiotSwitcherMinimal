using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace RiotSwitcherMinimal
{
    public class ManageProfilesForm : Form
    {
        private ProfileManager _profileManager;
        private ListBox _profilesList;
        private Button _deleteButton;
        private Button _closeButton;

        public ManageProfilesForm(ProfileManager manager)
        {
            _profileManager = manager;
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Profiles";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            _profilesList = new ListBox
            {
                Location = new Point(12, 12),
                Size = new Size(360, 200)
            };
            this.Controls.Add(_profilesList);

            _deleteButton = new Button
            {
                Text = "Delete Selected",
                Location = new Point(12, 220),
                Size = new Size(100, 30)
            };
            _deleteButton.Click += DeleteSelected;
            this.Controls.Add(_deleteButton);

            _closeButton = new Button
            {
                Text = "Close",
                Location = new Point(272, 220),
                Size = new Size(100, 30)
            };
            _closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(_closeButton);
        }

        private void RefreshList()
        {
            _profilesList.Items.Clear();
            foreach (var p in _profileManager.Profiles)
            {
                _profilesList.Items.Add(p.Name);
            }
        }

        private void DeleteSelected(object sender, EventArgs e)
        {
            if (_profilesList.SelectedItem == null) return;

            string name = _profilesList.SelectedItem.ToString();
            var result = MessageBox.Show($"Are you sure you want to delete profile '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            
            if (result == DialogResult.Yes)
            {
                _profileManager.DeleteProfile(name);
                RefreshList();
            }
        }
    }
}
