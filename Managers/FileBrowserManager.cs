using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeFileDialogSharp;

public record ExtensionFilter(string Name, string[] Extensions) { };

/// <summary>
/// <para>Exposes a function to launch a File picker (<see cref="OpenFileBrowser(ExtensionFilter[]? filters = null, bool selectFolders = false)"/>) </para>
/// <para>Tries to detect the current operating system at runtime using <see cref="RuntimeInformation.IsOSPlatform(OSPlatform)"/></para>
/// <para>Tested for Linux (using Zenity) and Windows (using <see cref="https://github.com/milleniumbug/NativeFileDialogSharp"/>)</para>
/// <para>MacOS implementation is not tested but uses OSA/AppleScript</para>
/// </summary>

public static class FileBrowserManager
{
    private static OSPlatform? Platform;

    public static string[]? OpenFileBrowser(ExtensionFilter[]? filters = null, bool selectFolders = false)
    {
        if (Platform == null) GetCurrentOSPlatform();

        return Platform switch //You gotta love IDEs
        {
            OSPlatform platform when platform == OSPlatform.Windows => OpenFileBrowserWindows(filters, selectFolders),
            OSPlatform platform when platform == OSPlatform.Linux => OpenFileBrowserLinux(filters, selectFolders),
            OSPlatform platform when platform == OSPlatform.OSX => OpenFileBrowserMacOS(filters, selectFolders),
            _ => null
        };
    }

    private static void GetCurrentOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Platform = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Platform = OSPlatform.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Platform = OSPlatform.OSX;
        }
    }

    public static string[]? OpenFileBrowserWindows(ExtensionFilter[]? filters, bool selectFolders)
    {
        if (selectFolders)
        {
            var dirResult = Dialog.FolderPicker();
            return dirResult.IsOk && dirResult.Path != null ? [dirResult.Path] : null;
        }

        string? filterString = null;

        if (filters != null && filters.Length > 0)
        {
            filterString = string.Join(",", filters.SelectMany(f => f.Extensions));
        }

        var fileResult = Dialog.FileOpenMultiple(filterList: filterString);
        return fileResult.IsOk ? [.. fileResult.Paths] : null;
    }


    public static string[]? OpenFileBrowserLinux(ExtensionFilter[]? filters, bool selectFolders)
    {
        string args = selectFolders
            ? "--file-selection --directory --multiple"
            : "--file-selection --multiple" +
              (filters != null && filters.Length > 0
                ? " --file-filter=\"*." + string.Join(" *.", filters.SelectMany(f => f.Extensions)) + "\""
                : "");

        Process process = new()
        {
            StartInfo = new()
            {
                FileName = "zenity",
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return string.IsNullOrEmpty(output)
            ? []
            : output.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }

    //TODO this needs testing 
    public static string[]? OpenFileBrowserMacOS(ExtensionFilter[]? filters, bool selectFolders)
    {
        string appleScript = selectFolders
            ? "set chosen to choose folder with multiple selections allowed\n" +
              "set output to \"\"\n" +
              "repeat with f in chosen\n" +
              "set output to output & (POSIX path of f) & \"|\"\n" +
              "end repeat\n" +
              "return output"
            : "set chosen to choose file with multiple selections allowed\n" +
              "set output to \"\"\n" +
              "repeat with f in chosen\n" +
              "set output to output & (POSIX path of f) & \"|\"\n" +
              "end repeat\n" +
              "return output";

        Process process = new()
        {
            StartInfo = new()
            {
                FileName = "osascript",
                Arguments = $"-e \"{appleScript}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        var files = string.IsNullOrEmpty(output) ? [] : output.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (!selectFolders && filters != null)
        {
            var allowed = filters.SelectMany(f => f.Extensions)
                                 .ToHashSet();
            files = [.. files.Where(f => allowed.Contains(Path.GetExtension(f).ToLower()))];
        }

        return files;
    }

    public static string[] GetFilesFromFolders(string[] folders, ExtensionFilter[]? filters)
    {
        var allowed = filters?.SelectMany(f => f.Extensions)
                              .Select(e => e.TrimStart('*').ToLower())
                              .ToHashSet();

        return [.. folders.SelectMany(folder =>
            Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                     .Where(f => allowed == null || allowed.Contains(Path.GetExtension(f).ToLower()))
        )];
    }
}
