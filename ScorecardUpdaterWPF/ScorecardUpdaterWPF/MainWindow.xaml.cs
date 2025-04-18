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
                Log("📄 Loaded appsettings.json");
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to load settings: {ex.Message}");
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog { Description = "Select your install folder." };
            if (dlg.ShowDialog() != true) return;

            string targetDir = dlg.SelectedPath;
            try
            {
                var backups = BackupProtectedFiles(targetDir);
                string tempRepoDir = Path.Combine(Path.GetTempPath(), $"repo_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempRepoDir);
                Log("📥 Cloning deploy branch to temp folder...");
                RunGit($"clone --branch {appSettings.RepoBranch} {appSettings.GitHubRepoUrl} \"{tempRepoDir}\"");

                string gitFolder = Path.Combine(tempRepoDir, ".git");
                if (Directory.Exists(gitFolder))
                {
                    try
                    {
                        Directory.Delete(gitFolder, true);
                        Log("🧽 Removed .git folder from cloned repo");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to remove .git folder: {ex.Message}");
                    }
                }

                CopyCleanFromTemp(tempRepoDir, targetDir);
                RestoreProtectedFiles(targetDir, backups);
                Directory.Delete(tempRepoDir, true);
                Log("✅ Update complete.");
            }
            catch (Exception ex)
            {
                Log($"❌ Error: {ex.Message}");
            }
        }

        private void CopyCleanFromTemp(string sourceDir, string targetDir)
        {
            var protectedSet = new HashSet<string>(
                appSettings.ProtectedFiles.Select(p => p.Replace('\\', '/')),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var srcPath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(sourceDir, srcPath).Replace('\\', '/');

                if (protectedSet.Contains(relPath))
                {
                    Log($"🔒 Skipped protected: {relPath}");
                    continue;
                }

                string destPath = Path.Combine(targetDir, relPath);
                string? destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(srcPath, destPath, true);
                Log($"📦 Copied: {relPath}");
            }
        }


        private Dictionary<string, string> BackupProtectedFiles(string root)
        {
            var backups = new Dictionary<string, string>();
            foreach (var file in appSettings.ProtectedFiles)
            {
                var fullPath = Path.Combine(root, file);
                if (File.Exists(fullPath))
                {
                    var tempFile = Path.GetTempFileName();
                    File.Copy(fullPath, tempFile, true);
                    backups[file] = tempFile;
                    Log($"🔐 Backed up: {file}");
                }
            }
            return backups;
        }

        private void RestoreProtectedFiles(string root, Dictionary<string, string> backups)
        {
            foreach (var kvp in backups)
            {
                string targetPath = Path.Combine(root, kvp.Key);
                string targetDir = Path.GetDirectoryName(targetPath)!;
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(kvp.Value, targetPath, true);
                File.Delete(kvp.Value);
                Log($"♻️ Restored: {kvp.Key}");
            }
        }

        private void ClearDirectory(string path)
        {
            Log("🧹 Clearing install folder...");
            foreach (var dir in Directory.GetDirectories(path))
            {
                try { Directory.Delete(dir, true); }
                catch (Exception ex) { Log($"⚠️ Failed to delete folder: {dir} – {ex.Message}"); }
            }

            foreach (var file in Directory.GetFiles(path))
            {
                try { File.Delete(file); }
                catch (Exception ex) { Log($"⚠️ Failed to delete file: {file} – {ex.Message}"); }
            }
        }

        private void CopyAllFiles(string sourceDir, string targetDir)
        {
            foreach (var srcPath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(sourceDir, srcPath);
                string destPath = Path.Combine(targetDir, relPath);
                string? destFolder = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                File.Copy(srcPath, destPath, true);
                Log($"📦 Copied: {relPath}");
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

            if (!string.IsNullOrWhiteSpace(stdout))
                Log(stdout.Trim());

            if (p.ExitCode != 0)
                throw new Exception($"Git error: {stderr.Trim()}");
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
