using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace RiotSwitcherMinimal
{
    public class RiotSwitcherAppContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ProfileManager _profileManager;
        private ContextMenuStrip _contextMenu;

        public RiotSwitcherAppContext()
        {
            _profileManager = new ProfileManager();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _contextMenu = new ContextMenuStrip();
            UpdateContextMenu();

            Icon appIcon = null;
            try
            {
                // Try loading from embedded resource first (Release mode)
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("RiotSwitcherMinimal.icon.ico"))
                {
                    if (stream != null)
                    {
                        appIcon = new Icon(stream);
                    }
                }

                // Fallback to file if resource failed (Debug mode maybe?)
                if (appIcon == null && System.IO.File.Exists("icon.ico"))
                {
                    appIcon = new Icon("icon.ico");
                }
            }
            catch 
            {
                // appIcon remains null, will fallback below
            }

            if (appIcon == null)
            {
                 appIcon = SystemIcons.Application;
            }

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "RiotSwitcherMinimal"
            };

            // Check for Riot Client Path on startup
            if (string.IsNullOrEmpty(_profileManager.RiotClientInstallPath) || !System.IO.Directory.Exists(_profileManager.RiotClientInstallPath))
            {
                 PromptForRiotClientPath();
            }
        }

        private void PromptForRiotClientPath()
        {
             using (var form = new ClientPathForm(_profileManager.RiotClientInstallPath))
             {
                 if (form.ShowDialog() == DialogResult.OK)
                 {
                     _profileManager.SetRiotClientPath(form.SelectedPath);
                 }
                 else
                 {
                     // If they cancel and we have no path, maybe warn?
                     // They can set it later via menu.
                     if (string.IsNullOrEmpty(_profileManager.RiotClientInstallPath))
                     {
                         MessageBox.Show("Riot Client path is not set. Switching profiles will not work until this is configured.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     }
                 }
             }
        }

        private void UpdateContextMenu()
        {
            _contextMenu.Items.Clear();

            // Header
            var header = new ToolStripMenuItem("RiotSwitcher") { Enabled = false };
            header.Font = new Font(header.Font, FontStyle.Bold);
            _contextMenu.Items.Add(header);
            _contextMenu.Items.Add(new ToolStripSeparator());

            // Profiles
            if (_profileManager.Profiles.Count == 0)
            {
                var noProfiles = new ToolStripMenuItem("No profiles found") { Enabled = false };
                _contextMenu.Items.Add(noProfiles);
            }
            else
            {
                foreach (var profile in _profileManager.Profiles)
                {
                    string displayText = profile.Name == _profileManager.CurrentProfileName ? $"✓ {profile.Name}" : profile.Name;
                    var item = new ToolStripMenuItem(displayText, null, (s, e) => SwitchProfile(profile.Name));
                    _contextMenu.Items.Add(item);
                }
            }

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Actions
            _contextMenu.Items.Add(new ToolStripMenuItem("Add Profile...", null, ShowAddProfile));
            _contextMenu.Items.Add(new ToolStripMenuItem("Manage Profiles...", null, ShowManageProfiles));
            _contextMenu.Items.Add(new ToolStripMenuItem("Set Riot Client Path...", null, (s, e) => PromptForRiotClientPath()));

            // Settings
            _contextMenu.Items.Add(new ToolStripSeparator());
            var startupItem = new ToolStripMenuItem("Run on Startup", null, ToggleStartup)
            {
                Checked = IsStartupEnabled()
            };
            _contextMenu.Items.Add(startupItem);

            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));
        }

        private bool IsStartupEnabled()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue("RiotSwitcherMinimal") != null;
            }
        }

        private void ToggleStartup(object? sender, EventArgs e)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (IsStartupEnabled())
                    {
                        key?.DeleteValue("RiotSwitcherMinimal", false);
                    }
                    else
                    {
                        key?.SetValue("RiotSwitcherMinimal", Application.ExecutablePath);
                    }
                }
                UpdateContextMenu(); // Refresh check state
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to toggle startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SwitchProfile(string profileName)
        {
            try
            {
                _profileManager.SwitchProfile(profileName);
                UpdateContextMenu(); // Refresh UI to show new checkmark
                _trayIcon.ShowBalloonTip(3000, "Switched Profile", $"Switched to {profileName}", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "Error", $"Failed to switch: {ex.Message}", ToolTipIcon.Error);
                MessageBox.Show($"Failed to switch profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddProfile(object sender, EventArgs e)
        {
            using (var form = new AddProfileForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _profileManager.CreateProfile(form.ProfileName);
                        UpdateContextMenu();
                        _trayIcon.ShowBalloonTip(3000, "Success", $"Created profile {form.ProfileName}", ToolTipIcon.Info);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to create profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ShowManageProfiles(object sender, EventArgs e)
        {
             using (var form = new ManageProfilesForm(_profileManager))
             {
                 form.ShowDialog();
                 UpdateContextMenu(); // Refresh list after management
             }
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
