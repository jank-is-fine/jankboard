using System.Diagnostics;

namespace Managers.Clipboard
{
    /// <summary>
    /// <para>Tests and identifies working clipboard backend commands across Windows, Linux, and macOS platforms</para>
    /// <para>Windows and macOS platforms have a known good way to archieve this but linux has some special commands depeding on the Clipboard package installed like wl-copy or xsel etc.</para>
    /// </summary>

    public class ClipboardTester
    {
        private static readonly string testString = "ClipboardTest !123%";
        static readonly Dictionary<string, (string getCommand, string setCommand)> LinuxBackends = new()
        {
            { "wl-clipboard (Wayland)", ("wl-paste", "wl-copy") },
            { "xsel (clipboard)", ("xsel --clipboard --output", "xsel --clipboard --input") },
            { "xclip (clipboard)", ("xclip -selection clipboard -out", "xclip -selection clipboard -in") },
            { "copyq", ("copyq clipboard", "copyq copy") },
        };

        static readonly Dictionary<string, (string getCommand, string setCommand)> MacOSBackends = new()
        {
            { "pbcopy/pbpaste (native)", ("pbpaste", "pbcopy") },
        };

        static readonly Dictionary<string, (string getCommand, string setCommand)> WindowsBackends = new()
        {
            { "PowerShell", ("powershell -Command \"Get-Clipboard\"", "powershell -Command \"$input | Set-Clipboard\"") },
        };

        private static Dictionary<string, (string getCommand, string setCommand)>? GetPlatformDictionary()
        {
            if (OperatingSystem.IsWindows()) return WindowsBackends;
            if (OperatingSystem.IsLinux()) return LinuxBackends;
            if (OperatingSystem.IsMacOS()) return MacOSBackends;
            return null;
        }

        public static (string GetCommand, string SetCommand, int index)? GetWorkingCommands()
        {
            var platformDictionary = GetPlatformDictionary();
            if (platformDictionary == null) return null;

            foreach (var entry in platformDictionary)
            {
                try
                {
                    bool setSuccess = ExecuteSetCommand(entry.Value.setCommand, testString);
                    if (!setSuccess) continue;

                    string gotText = ExecuteGetCommand(entry.Value.getCommand)?.Trim() ?? "";

                    if (gotText == testString)
                    {
                        Logger.Log("ClipboardTester", $"Found Working backend: {entry.Key}", LogLevel.INFO);

                        return (entry.Value.getCommand, entry.Value.setCommand, platformDictionary.Values.ToList().FindIndex(x => x == entry.Value));
                    }
                    else
                    {
                        Logger.Log("ClipboardTester", $"Backend {entry.Key} failed. Expected: '{testString}', Got: '{gotText}'", LogLevel.ERROR);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("ClipboardTester", $"Failed {entry.Key}: {e.Message}", LogLevel.INFO);
                    continue;
                }
            }
            return null;
        }

        private static string? ExecuteGetCommand(string command)
        {
            var parts = ParseCommand(command);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = parts.fileName,
                    Arguments = parts.arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            bool exited = process.WaitForExit(2000);

            if (!exited)
            {
                process.Kill();
                return null;
            }

            return result;
        }

        private static bool ExecuteSetCommand(string command, string text)
        {
            var (fileName, arguments) = ParseCommand(command);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            process.StandardInput.Write(text);
            process.StandardInput.Close();

            bool exited = process.WaitForExit(200);

            if (!exited)
            {
                process.Kill();
                return false;
            }

            return process.ExitCode == 0;
        }

        private static (string fileName, string arguments) ParseCommand(string command)
        {
            var parts = command.Split(' ', 2);
            return (parts[0], parts.Length > 1 ? parts[1] : "");
        }

        public static (string GetCommand, string SetCommand)? GetCommandByIndex(int index)
        {
            var dict = GetPlatformDictionary();
            if (dict != null && dict.Count - 1 >= index)
            {
                var list = dict.Values.ToList();
                return list[index];
            }
            return null;
        }
    }
}