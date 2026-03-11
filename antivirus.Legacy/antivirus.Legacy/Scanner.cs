using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace antivirus.Legacy
{
    public class ScanResult
    {
        public int FilesScanned { get; set; }
        public int DirectoriesScanned { get; set; }
        public int InfectionsFound { get; set; }
        public int FilesQuarantined { get; set; }
        public bool Success { get; set; }
        public List<string> InfectedFiles { get; set; }
    }

    public static class Scanner
    {
        public static ScanResult Scan(string path)
        {
            List<string> infectedFiles = new List<string>();

            try
            {
                Console.WriteLine("Initializing ClamAV scanner...");

                // Find clamscan.exe
                string clamscanPath = FindClamscanExecutable();
                if (string.IsNullOrEmpty(clamscanPath))
                {
                    Console.WriteLine("ERROR: clamscan.exe not found. Cannot perform virus scan.");
                    Console.WriteLine("Please run 'Update Virus Definitions' (option 3) first to download ClamAV.");
                    return new ScanResult 
                    { 
                        FilesScanned = 0, 
                        DirectoriesScanned = 0, 
                        InfectionsFound = 0,
                        FilesQuarantined = 0,
                        Success = false,
                        InfectedFiles = infectedFiles
                    };
                }

                // Find database directory
                string dbDir = FindClamAVDatabaseDirectory();
                if (string.IsNullOrEmpty(dbDir) || !Directory.Exists(dbDir))
                {
                    Console.WriteLine("WARNING: ClamAV database not found. Scan may not detect threats.");
                    Console.WriteLine("Please run 'Update Virus Definitions' (option 3) to download virus signatures.");
                }

                Console.WriteLine($"Using ClamAV scanner: {clamscanPath}");
                if (!string.IsNullOrEmpty(dbDir))
                    Console.WriteLine($"Using virus database: {dbDir}");
                Console.WriteLine($"Scanning: {path}");
                Console.WriteLine("This may take several minutes for large directories...\n");

                // Build clamscan arguments
                string arguments;
                bool isClamscan = Path.GetFileNameWithoutExtension(clamscanPath).Equals("clamscan", StringComparison.OrdinalIgnoreCase);

                if (isClamscan && !string.IsNullOrEmpty(dbDir) && Directory.Exists(dbDir))
                    arguments = $"--database=\"{dbDir}\" --recursive --verbose --infected \"{path}\"";
                else
                    arguments = $"--recursive --verbose --infected \"{path}\"";

                // Run clamscan with real-time progress monitoring
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = clamscanPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                int filesScanned = 0;
                int dirsScanned = 0;
                int infectionsFound = 0;
                DateTime scanStartTime = DateTime.Now;
                DateTime lastUpdate = DateTime.Now;
                List<string> allOutput = new List<string>();

                using (Process process = Process.Start(processInfo))
                {
                    // Use asynchronous output reading for real-time progress
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            allOutput.Add(e.Data);

                            // Check for infections
                            if (e.Data.Contains("FOUND"))
                            {
                                infectionsFound++;
                                string[] parts = e.Data.Split(':');
                                if (parts.Length >= 2)
                                {
                                    string filePath = parts[0].Trim();
                                    infectedFiles.Add(filePath);
                                    Console.WriteLine($"\n⚠ INFECTED: {filePath}");
                                }
                            }

                            // Count files being scanned (verbose mode shows each file)
                            filesScanned++;

                            // Display progress every 2 seconds
                            TimeSpan timeSinceUpdate = DateTime.Now - lastUpdate;
                            if (timeSinceUpdate.TotalSeconds >= 2)
                            {
                                TimeSpan elapsed = DateTime.Now - scanStartTime;
                                double filesPerSecond = filesScanned / elapsed.TotalSeconds;

                                Console.WriteLine($"Progress: {filesScanned:N0} files | " +
                                                $"{infectionsFound} threats | " +
                                                $"Elapsed: {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2} | " +
                                                $"Speed: {filesPerSecond:F0} files/sec");

                                lastUpdate = DateTime.Now;
                            }
                        }
                    };

                    process.BeginOutputReadLine();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // Parse final statistics from output
                    string fullOutput = string.Join("\n", allOutput.ToArray());
                    Match filesMatch = Regex.Match(fullOutput, @"Scanned files: (\d+)");
                    Match dirsMatch = Regex.Match(fullOutput, @"Scanned directories: (\d+)");
                    Match infectedMatch = Regex.Match(fullOutput, @"Infected files: (\d+)");

                    int finalFilesScanned = filesScanned;
                    int finalDirsScanned = dirsScanned;
                    int finalInfectionsFound = infectionsFound;

                    if (filesMatch.Success)
                        int.TryParse(filesMatch.Groups[1].Value, out finalFilesScanned);
                    if (dirsMatch.Success)
                        int.TryParse(dirsMatch.Groups[1].Value, out finalDirsScanned);
                    if (infectedMatch.Success)
                        int.TryParse(infectedMatch.Groups[1].Value, out finalInfectionsFound);

                    TimeSpan totalTime = DateTime.Now - scanStartTime;
                    Console.WriteLine($"\nScan completed in {totalTime.Hours:D2}:{totalTime.Minutes:D2}:{totalTime.Seconds:D2}");

                    // Show summary from clamscan
                    Console.WriteLine("\n--- SCAN SUMMARY ---");
                    foreach (string outputLine in allOutput)
                    {
                        if (outputLine.Contains("SCAN SUMMARY") || 
                            outputLine.Contains("Scanned files:") ||
                            outputLine.Contains("Infected files:") ||
                            outputLine.Contains("Data scanned:") ||
                            outputLine.Contains("Time:"))
                        {
                            Console.WriteLine(outputLine);
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("\n--- Warnings/Errors ---");
                        Console.WriteLine(error);
                    }

                    return new ScanResult
                    {
                        FilesScanned = finalFilesScanned,
                        DirectoriesScanned = finalDirsScanned,
                        InfectionsFound = finalInfectionsFound,
                        FilesQuarantined = 0,
                        Success = process.ExitCode == 0 || process.ExitCode == 1,
                        InfectedFiles = infectedFiles
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scan failed: {ex.Message}");
                return new ScanResult 
                { 
                    FilesScanned = 0, 
                    DirectoriesScanned = 0, 
                    InfectionsFound = 0,
                    FilesQuarantined = 0,
                    Success = false,
                    InfectedFiles = infectedFiles
                };
            }
        }

        private static string FindClamscanExecutable()
        {
            // Check local ClamAV directory first (portable installation)
            string localClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
            string clamscanInLocal = Path.Combine(localClamAVDir, "clamscan.exe");
            if (File.Exists(clamscanInLocal))
                return clamscanInLocal;

            string clamscanInLocalBin = Path.Combine(localClamAVDir, "bin\\clamscan.exe");
            if (File.Exists(clamscanInLocalBin))
                return clamscanInLocalBin;

            // Check current directory
            string clamscanInCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "clamscan.exe");
            if (File.Exists(clamscanInCurrentDir))
                return clamscanInCurrentDir;

            // Don't check system paths to avoid permission issues
            return null;
        }

        private static string FindClamAVDatabaseDirectory()
        {
            // Check local ClamAV database directory
            string localClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
            string dbDir = Path.Combine(localClamAVDir, "database");
            if (Directory.Exists(dbDir))
                return dbDir;

            // Check fallback location
            string fallback = Path.Combine(Directory.GetCurrentDirectory(), "clamav-db");
            if (Directory.Exists(fallback))
                return fallback;

            return null;
        }

        public static void ReadMBR() { Console.WriteLine("Reading MBR..."); }
        public static bool EnsureClamAVInstalled() { return true; }
        public static bool EnsureClamAVDefinitionsExist() { return true; }
        public static void EnsureBrowserInstallers() { }
    }
}
