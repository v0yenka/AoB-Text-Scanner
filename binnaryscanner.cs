using System;
using System.IO;
using System.Text;

class BinaryScanner
{
    static void Main()
    {
        // Path to search
        string baseDir = @" ";
        // The text to look for
        string search = " ";

        Console.WriteLine($"Scanning folder: {baseDir}");
        Console.WriteLine($"Searching for: \"{search}\"");
        Console.WriteLine();

        string outFile = Path.Combine(Environment.CurrentDirectory, "searchresults.txt");
        using StreamWriter writer = new StreamWriter(outFile, false, Encoding.UTF8);
        writer.WriteLine($"Search for '{search}' at {DateTime.Now}");
        writer.WriteLine("--------------------------------------------------");

        byte[] pattern = Encoding.ASCII.GetBytes(search);
        byte[] buffer = new byte[1024 * 1024]; // 1 MB buffer
        int contextSize = 64; // bytes to show around match
        int foundCount = 0;

        foreach (string file in Directory.GetFiles(baseDir, "*.*", SearchOption.AllDirectories))
        {
            try
            {
                using FileStream fs = File.OpenRead(file);
                int bytesRead;
                long offset = 0;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead - pattern.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < pattern.Length; j++)
                        {
                            if (buffer[i + j] != pattern[j]) { match = false; break; }
                        }
                        if (match)
                        {
                            foundCount++;
                            long absoluteOffset = offset + i;

                            // Extract context bytes
                            fs.Seek(absoluteOffset - contextSize >= 0 ? absoluteOffset - contextSize : 0, SeekOrigin.Begin);
                            byte[] contextBytes = new byte[pattern.Length + 2 * contextSize];
                            fs.Read(contextBytes, 0, contextBytes.Length);
                            string hexDump = BitConverter.ToString(contextBytes).Replace("-", " ");
                            string asciiDump = Encoding.ASCII.GetString(contextBytes).Replace("\0", ".");

                            string info = $"[{foundCount}] File: {file}\n" +
                                          $"Offset: 0x{absoluteOffset:X}\n" +
                                          $"ASCII context: {asciiDump}\n" +
                                          $"Hex context: {hexDump}\n";
                            Console.WriteLine(info);
                            writer.WriteLine(info);
                            writer.WriteLine("--------------------------------------------------");

                            // Save small binary fragment
                            string dumpDir = Path.Combine(Environment.CurrentDirectory, "matches");
                            Directory.CreateDirectory(dumpDir);
                            string dumpFile = Path.Combine(dumpDir, $"{Path.GetFileName(file)}_{absoluteOffset:X}.bin");
                            File.WriteAllBytes(dumpFile, contextBytes);
                        }
                    }
                    offset += bytesRead;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading {file}: {e.Message}");
            }
        }

        Console.WriteLine($"\nSearch finished. Found {foundCount} matches.");
        writer.WriteLine($"\nSearch finished. Found {foundCount} matches.");
        writer.Close();

        Console.WriteLine($"Results saved to: {outFile}");
        Console.WriteLine("Press Enter to close.");
        Console.ReadLine();
    }
}