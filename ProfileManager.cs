using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace RiotSwitcherMinimal
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public string DirectoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ProfileManager
    {
        private const string RiotClientProcessName = "RiotClientServices";
        private const string LeagueClientProcessName = "LeagueClient";
        private const string ValorantProcessName = "Valorant";
        
        // Profiles
        private readonly string _profilesDir;
        private readonly string _profilesJsonPath;
        private List<Profile> _profiles = new();

        // Config persistence
        private readonly string _configPath;
        private AppConfig _config = new();

        // Cached paths
        public string RiotClientInstallPath { get; private set; } = string.Empty;
        private string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public ProfileManager()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RiotSwitcherMinimal");
            _profilesDir = Path.Combine(appData, "Profiles");
            _profilesJsonPath = Path.Combine(_profilesDir, "profiles.json");
            _configPath = Path.Combine(appData, "config.json");

            if (!Directory.Exists(_profilesDir))
            {
                Directory.CreateDirectory(_profilesDir);
            }

            LoadProfiles();
            LoadConfig();
            
            // Should we fallback to auto-detect if config is empty? 
            if (string.IsNullOrEmpty(RiotClientInstallPath))
            {
                 AutoDetectRiotClient();
            }
        }

        public List<Profile> Profiles => _profiles;
        public string? CurrentProfileName => _config.LastActiveProfile;

        private class AppConfig
        {
            public string? LastActiveProfile { get; set; }
            public string? RiotClientInstallPath { get; set; }
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_configPath)) ?? new AppConfig();
                }
                catch { _config = new AppConfig(); }
            }
            // Populate public property from config
            RiotClientInstallPath = _config.RiotClientInstallPath ?? string.Empty;
        }

        private void SaveConfig()
        {
            try
            {
                // Ensure config reflects current property state
                _config.RiotClientInstallPath = RiotClientInstallPath;
                File.WriteAllText(_configPath, JsonSerializer.Serialize(_config));
            }
            catch { }
        }

        public void SetRiotClientPath(string path)
        {
            RiotClientInstallPath = path;
            SaveConfig();
        }

        public void LoadProfiles()
        {
            if (File.Exists(_profilesJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(_profilesJsonPath);
                    _profiles = JsonSerializer.Deserialize<List<Profile>>(json) ?? new List<Profile>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load profiles: {ex.Message}");
                    _profiles = new List<Profile>();
                }
            }
        }

        public void SaveProfiles()
        {
            try
            {
                string json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_profilesJsonPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save profiles: {ex.Message}");
            }
        }

        public void CreateProfile(string name)
        {
            if (_profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Profile already exists.");
            }

            // 1. If we have a current profile, save implementation state to it
            // Only if we actually have a valid current profile that exists
            if (!string.IsNullOrEmpty(_config.LastActiveProfile))
            {
                var current = _profiles.FirstOrDefault(p => p.Name == _config.LastActiveProfile);
                if (current != null)
                {
                    Debug.WriteLine($"Saving current state to profile '{current.Name}' before creating new one.");
                    string currentProfileDir = Path.Combine(_profilesDir, current.DirectoryName);
                    // We save the CURRENT live data to the CURRENT profile
                    BackupCurrentConfigTo(currentProfileDir); 
                }
            }

            // 2. Create new profile entry
            string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            string dirName = $"{cleanName}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            string newProfileDir = Path.Combine(_profilesDir, dirName);
            Directory.CreateDirectory(newProfileDir);

            var newProfile = new Profile
            {
                Name = name,
                DirectoryName = dirName,
                CreatedAt = DateTime.Now
            };
            _profiles.Add(newProfile);
            SaveProfiles();

            // 3. Set as active
            _config.LastActiveProfile = name;
            SaveConfig();

            // 4. Kill Riot, Clear Live Data, Launch Riot
            KillRiotProcesses();
            ClearLiveRiotData();
            LaunchRiotClient();
        }

        public void DeleteProfile(string name)
        {
            var profile = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (profile == null) return;

            string profileDir = Path.Combine(_profilesDir, profile.DirectoryName);
            if (Directory.Exists(profileDir))
            {
                try { Directory.Delete(profileDir, true); } catch { }
            }

            _profiles.Remove(profile);
            SaveProfiles();

            if (_config.LastActiveProfile == name)
            {
                _config.LastActiveProfile = null;
                SaveConfig();
            }
        }

        public void SwitchProfile(string name)
        {
            var targetProfile = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (targetProfile == null) throw new Exception("Profile not found.");

            // 1. Save current state to current profile (if exists)
            if (!string.IsNullOrEmpty(_config.LastActiveProfile))
            {
                var current = _profiles.FirstOrDefault(p => p.Name == _config.LastActiveProfile);
                if (current != null)
                {
                    string currentProfileDir = Path.Combine(_profilesDir, current.DirectoryName);
                    BackupCurrentConfigTo(currentProfileDir);
                }
            }

            // 2. Kill Processes
            KillRiotProcesses();

            // 3. Restore target profile data
            string targetProfileDir = Path.Combine(_profilesDir, targetProfile.DirectoryName);
            RestoreConfigFrom(targetProfileDir);

            // 4. Update Config
            _config.LastActiveProfile = name;
            SaveConfig();

            // 5. Launch
            LaunchRiotClient();
        }

        private void KillRiotProcesses()
        {
            foreach (var procName in new[] { RiotClientProcessName, LeagueClientProcessName, ValorantProcessName })
            {
                foreach (var proc in Process.GetProcessesByName(procName))
                {
                    try { proc.Kill(); proc.WaitForExit(1000); } catch { }
                }
            }
        }

        private void LaunchRiotClient()
        {
            if (string.IsNullOrEmpty(RiotClientInstallPath)) return;

            string exePath = Path.Combine(RiotClientInstallPath, "RiotClientServices.exe");
            if (File.Exists(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--launch-product=riot-client --launch-patchline=live", 
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        private void ClearLiveRiotData()
        {
            // Delete key Login Data files to force a logout/clean slate
            try
            {
                var privateSettings = GetLocalFilePath("Riot Games/Riot Client/Data/RiotGamesPrivateSettings.yaml");
                if (File.Exists(privateSettings)) File.Delete(privateSettings);

                var sessionsDir = GetLocalFilePath("Riot Games/Riot Client/Data/Sessions");
                if (Directory.Exists(sessionsDir)) Directory.Delete(sessionsDir, true);

                // Optional: Clear RiotClientSettings.yaml?
                // Kept for now to preserve region/locale, but can be deleted if issues arise.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear live data: {ex.Message}");
            }
        }



        private bool BackupCurrentConfigTo(string destDir)
        {
            try
            {
                // Ensure dest dir exists
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                CopyFile(GetLocalFilePath("Riot Games/Riot Client/Data/RiotGamesPrivateSettings.yaml"), Path.Combine(destDir, "RiotGamesPrivateSettings.yaml"), optional: true);
                CopyDirectory(GetLocalFilePath("Riot Games/Riot Client/Data/Sessions"), Path.Combine(destDir, "Sessions"));
                CopyFile(GetLocalFilePath("Riot Games/Riot Client/Config/RiotClientSettings.yaml"), Path.Combine(destDir, "RiotClientSettings.yaml"), optional: true);
                CopyFile(GetLocalFilePath("Riot Games/Riot Client/Config/lockfile"), Path.Combine(destDir, "lockfile"), optional: true);
                
                if (!string.IsNullOrEmpty(RiotClientInstallPath))
                {
                     CopyFile(Path.Combine(RiotClientInstallPath, "Config/client.config.yaml"), Path.Combine(destDir, "client.config.yaml"), optional: true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Backup failed: {ex.Message}");
                return false;
            }
        }

        private void RestoreConfigFrom(string sourceDir)
        {
            try
            {
                // If the profile is "empty" (just created), checking for file existence prevents errors.
                // But we should also likely ClearLiveRiotData first to ensure we don't merge old state with "empty" state.
                // However, SwitchProfile logic cleans up by overwriting.
                // If source has NO files (fresh profile), we effectively want a clean state.
                
                // Let's check if we have data. If not, maybe just clear live data?
                if (!File.Exists(Path.Combine(sourceDir, "RiotGamesPrivateSettings.yaml")))
                {
                    ClearLiveRiotData(); // Fallback for empty profiles
                }

                RestoreFile(Path.Combine(sourceDir, "RiotGamesPrivateSettings.yaml"), GetLocalFilePath("Riot Games/Riot Client/Data/RiotGamesPrivateSettings.yaml"), optional: true);
                RestoreDirectory(Path.Combine(sourceDir, "Sessions"), GetLocalFilePath("Riot Games/Riot Client/Data/Sessions"));
                RestoreFile(Path.Combine(sourceDir, "RiotClientSettings.yaml"), GetLocalFilePath("Riot Games/Riot Client/Config/RiotClientSettings.yaml"), optional: true);
                RestoreFile(Path.Combine(sourceDir, "lockfile"), GetLocalFilePath("Riot Games/Riot Client/Config/lockfile"), optional: true);

                if (!string.IsNullOrEmpty(RiotClientInstallPath))
                {
                     RestoreFile(Path.Combine(sourceDir, "client.config.yaml"), Path.Combine(RiotClientInstallPath, "Config/client.config.yaml"), optional: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Restore failed: {ex.Message}");
                throw;
            }
        }

        private string GetLocalFilePath(string relPath)
        {
            return Path.Combine(_localAppData, relPath);
        }

        private void CopyFile(string source, string dest, bool optional = false)
        {
            if (File.Exists(source))
            {
                File.Copy(source, dest, true);
            }
            else if (!optional)
            {
                throw new FileNotFoundException($"Required file not found: {source}");
            }
        }

        private void RestoreFile(string source, string dest, bool optional = false)
        {
            if (File.Exists(source))
            {
                string destDir = Path.GetDirectoryName(dest);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                File.Copy(source, dest, true);
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir)) return; 

            if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(destDir, name));
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string name = Path.GetFileName(dir);
                CopyDirectory(dir, Path.Combine(destDir, name));
            }
        }

        private void RestoreDirectory(string sourceDir, string destDir)
        {
             CopyDirectory(sourceDir, destDir);
        }

        private void AutoDetectRiotClient()
        {
            string defaultPath = @"C:\Riot Games\Riot Client";
            if (Directory.Exists(defaultPath))
            {
                RiotClientInstallPath = defaultPath;
            }
        }
    }
}
