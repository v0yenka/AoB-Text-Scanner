# Universal AoB / Text Scanner

A small C# tool for scanning binary/text data in game or project directories.  
It helps to locate specific byte patterns, strings, or script references inside complex data folders.

---

## What It Does

- Recursively scans through all files in a selected directory.  
- Looks for specific binary signatures (AoB patterns) or plain text strings.  
- Saves the search results automatically to a seperate file called `SearchResults.txt`.  
- Handles large data sets safely — **it doesn’t modify or write to any files being scanned**. 

## **Safe to use**:  
> This tool only reads files from disk.  
> It does **not** interact with memory, game processes, or executables.  
> You can freely use it for research, file analysis or modding documentation.

---

## Usage

### 1. Open the project in Visual Studio or run it with the .NET SDK:
```bash
   dotnet run
```

### 2. When prompted, enter the full path to the folder you want to scan.

### 3. Enter your search term — either a text snippet or a byte pattern.

### 4. Wait for the scan to finish.

### 5. Open the file SearchResults.txt in the project folder to view all results.

---

## Author

v0yenka
---
