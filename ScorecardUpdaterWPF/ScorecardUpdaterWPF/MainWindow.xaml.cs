using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ScorecardUpdaterWPF
{
    public partial class MainWindow : Window
    {
        private Settings appSettings;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                string json = File.ReadAllText("appsettings.json");
                appSettings = JsonSerializer.Deserialize<Settings>(json)!;
                Log("Loaded appsettings.json");
            }
            catch (Exception ex)
            {
                Log($"Failed to load settings: {ex.Message}");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog { Description = "Select your install folder." };
            if (dlg.ShowDialog() != true) return;

            string targetDir = dlg.SelectedPath;
            try
            {
                BackupConfigs(targetDir, out var backups);
                CloneOrPullRepo(targetDir);
                RestoreConfigs(targetDir, backups);
                Log("Update complete.");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        // Step 1: back up any existing protected files
        private void BackupConfigs(string targetDir, out Dictionary<string, string> backups)
        {
            backups = new Dictionary<string, string>();
            foreach (var pf in appSettings.ProtectedFiles)
            {
                string path = Path.Combine(targetDir, pf);
                if (File.Exists(path))
                {
                    string tmp = Path.GetTempFileName();
                    File.Copy(path, tmp, true);
                    backups[pf] = tmp;
                    Log($"Backed up: {pf}");
                }
            }
        }

        // Step 2: run Git clone or pull
        private void CloneOrPullRepo(string targetDir)
        {
            if (Directory.Exists(Path.Combine(targetDir, ".git")))
            {
                Log("Pulling latest changes…");
                RunGit($"-C \"{targetDir}\" pull origin {appSettings.RepoBranch}");
            }
            else
            {
                Log("Cloning deploy branch…");
                RunGit($"clone --branch {appSettings.RepoBranch} {appSettings.GitHubRepoUrl} \"{targetDir}\"");
            }
        }

        // Step 3: restore the backups over whatever Git wrote
        private void RestoreConfigs(string targetDir, Dictionary<string, string> backups)
        {
            foreach (var kv in backups)
            {
                string dest = Path.Combine(targetDir, kv.Key);
                File.Copy(kv.Value, dest, true);
                File.Delete(kv.Value);
                Log($"Restored: {kv.Key}");
            }
        }

        private void RunGit(string args)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            Log(stdout.Trim());

            if (p.ExitCode != 0)
                Log($"Git error: {stderr.Trim()}");
        }


        private void Log(string msg)
        {
            LogTextBox.AppendText($"{DateTime.Now:T} – {msg}\n");
            LogTextBox.ScrollToEnd();
        }

        private class Settings
        {
            public string GitHubRepoUrl { get; set; } = "";
            public string RepoBranch { get; set; } = "deploy";
            public string[] ProtectedFiles { get; set; } = Array.Empty<string>();
        }
    }
}
