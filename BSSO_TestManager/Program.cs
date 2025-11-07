using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class FilePatternSwapper
{
    // CONFIG
    private static readonly string DataFolder = @" "; // Set your data folder path here
    private static readonly string LogFile = "searchresults.txt";
    private static readonly string BackupFolder = "Backups";
    private static readonly bool ReplaceAllOccurrences = true; // Set to false to only replace first found per file

    // ASCII text to find
    private static readonly string SearchText = " "; // e.g. "global/MapWindow.Start()"
    // AoB hex pattern to search - space separated hex bytes. Leave empty if not using
    private static readonly string AoBHex = "69 66 28 20 67 6C 6F 62 61 6C 2F 4D 61 70 57 69 6E 64 6F 77 2E 47 65 74 41 63 74 69 6F 6E 53 74 61 72 74 65 64 28 29 20 29 0D 0A 7B 0D 0A 09 67 6C 6F 62 61 6C 2F 4D 61 70 57 69 6E 64 6F 77 2E 53 74 6F 70 28 29 3B 0D 0A 7D 0D 0A 65 6C 73 65 0D 0A 7B 0D 0A 09 67 6C 6F 62 61 6C 2F 4D 61 70 57 69 6E 64 6F 77 2E 53 74 61 72 74 28 29 3B 0D 0A 7D 0D 0A 0D 0A 2F 2F 20 4D 65 74 72 69 63 73 0D 0A 67 6C 6F 62 61 6C 2F 4D 65 74 72 69 63 73 4D 61 6E 61 67 65 72 2F 55 49 57 69 6E 64 6F 77 2E 52 65 73 65 74 4D 65 74 72 69 63 28 29 3B 0D 0A 67 6C 6F 62 61 6C 2F 4D 65 74 72 69 63 73 4D 61 6E 61 67 65 72 2F 55 49 57 69 6E 64 6F 77 2E 53 65 74 4D 65 74 72 69 63 44 79 6E 61 6D 69 63 56 61 6C 75 65 28 22 57 69 6E 64 6F 77 22 2C 20 22 4D 69 6E 69 4D 61 70 56 69 65 77 42 75 74 74 6F 6E 22 29 3B 20 2F 2F 20 43 75 73 74 6F 6D 20 4E 61 6D 65 0D 0A 67 6C 6F 62 61 6C 2F 4D 65 74 72 69 63 73 4D 61 6E 61 67 65 72 2F 55 49 57 69 6E 64 6F 77 2E 53 65 74 4D 65 74 72 69 63 44 79 6E 61 6D 69 63 56 61 6C 75 65 28 22 45 6C 65 6D 65 6E 74 56 61 6C 75 65 22 2C 20 22 42 75 74 74 6F 6E 43 6C 69 63 6B 22 29 3B 0D 0A 67 6C 6F 62 61 6C 2F 4D 65 74 72 69 63 73 4D 61 6E 61 67 65 72 2F 55 49 57 69 6E 64 6F 77 2E 53 65 6E 64 4D 65 74 72 69 63 28 29 3B"; // e.g. "69 66 28 20 67 6C ..." leave empty if not using

    // Replacement text - what you want in place of SearchText. String only, no AoB replacement
    private static readonly string ReplacementText = " "; // e.g. "global/Horse.AddRelativeForce(0,1,2.5f);"

    static void Main()
    {
        Directory.CreateDirectory(BackupFolder);
        if (File.Exists(LogFile)) File.Delete(LogFile);

        byte[] searchBytes = Encoding.UTF8.GetBytes(SearchText);
        byte[] aobBytes = string.IsNullOrWhiteSpace(AoBHex) ? null : HexToBytes(AoBHex);
        byte[] replacementRaw = Encoding.UTF8.GetBytes(ReplacementText);

        Log($"Scan started at {DateTime.Now}");
        Log($"Data folder: {DataFolder}");
        Log($"Searching for text: \"{SearchText}\" (bytes len {searchBytes.Length})");
        if (aobBytes != null) Log($"Searching for AoB hex pattern (len {aobBytes.Length})");

        var files = Directory.GetFiles(DataFolder, "*.*", SearchOption.AllDirectories);
        int totalMatches = 0;
        foreach (var file in files)
        {
            try
            {
                byte[] data = File.ReadAllBytes(file);
                var matches = new List<int>();

                // Search ASCII pattern
                if (searchBytes.Length > 0)
                    matches.AddRange(FindAllOffsets(data, searchBytes));

                // Search AoB pattern (if provided)
                if (aobBytes != null)
                    matches.AddRange(FindAllOffsets(data, aobBytes));

                if (matches.Count == 0) continue;

                // Deduplicate and sort
                matches = matches.Distinct().OrderBy(x => x).ToList();
                Log($"File: {file} => Found {matches.Count} match(es): offsets: {string.Join(", ", matches.Select(o => "0x" + o.ToString("X"))) }");
                totalMatches += matches.Count;

                // Ask user whether to perform replacements for this file
                Console.WriteLine($"Found {matches.Count} match(es) in: {file}");
                Console.Write("Replace matches in this file? (y/N) : ");
                var ans = Console.ReadLine().Trim().ToLowerInvariant();
                if (ans != "y") continue;

                // Backup, just in case
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupName = Path.Combine(BackupFolder, Path.GetFileName(file) + "." + stamp + ".bak");
                File.Copy(file, backupName, true);
                Console.WriteLine($"Backup created: {backupName}");
                Log($"Backup created: {backupName}");

                // Replace in-memory copy
                foreach (var offset in matches)
                {
                    // If not replacing all, just do first
                    if (!ReplaceAllOccurrences && matches.IndexOf(offset) > 0) break;

                    // Determine replacement bytes sized to match original match length
                    int originalLen = (aobBytes != null && FindAllOffsets(data, aobBytes).Contains(offset)) ? aobBytes.Length : searchBytes.Length;
                    byte[] replacementBytes = new byte[originalLen];

                    if (replacementRaw.Length <= originalLen)
                    {
                        // Copy replacement, rest stays 0x00
                        Array.Copy(replacementRaw, replacementBytes, replacementRaw.Length);
                    }
                    else
                    {
                        // Truncate to original length - no growing file size
                        Array.Copy(replacementRaw, replacementBytes, originalLen);
                    }

                    // Write replacement bytes into data
                    Array.Copy(replacementBytes, 0, data, offset, originalLen);
                    Log($"Replaced {originalLen} bytes at 0x{offset:X} in {file}");
                }

                // Write atomic - to temp, then replace
                string tmp = file + ".tmp";
                File.WriteAllBytes(tmp, data);
                File.Replace(tmp, file, null);
                Console.WriteLine($"File updated: {file}");
                Log($"File updated: {file}");
            }
            catch (Exception ex)
            {
                Log($"Error processing {file}: {ex.Message}");
            }
        }

        Log($"Scan completed. Total matches found: {totalMatches}");
        Console.WriteLine("Done. See " + Path.GetFullPath(LogFile));
    }

    static void Log(string line)
    {
        Console.WriteLine(line);
        try { File.AppendAllText(LogFile, $"[{DateTime.Now:O}] {line}{Environment.NewLine}"); } catch { }
    }

    static List<int> FindAllOffsets(byte[] data, byte[] pattern)
    {
        var offsets = new List<int>();
        if (pattern == null || pattern.Length == 0 || data.Length == 0) return offsets;
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j]) { ok = false; break; }
            }
            if (ok) offsets.Add(i);
        }
        return offsets;
    }

    static byte[] HexToBytes(string hex)
    {
        // Accept hex like "69 66 28" or "696628"
        hex = hex.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        if (hex.Length % 2 == 1) throw new ArgumentException("Hex string must have even length");
        byte[] result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return result;
    }
}