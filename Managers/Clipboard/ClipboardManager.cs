using System.Diagnostics;

namespace Managers.Clipboard
{
    /// <summary>
    /// <para>Uses process execution with timeout handling to interact with system clipboard utilities</para>
    /// <para>Tries to detect working clipboard backend commands via ClipboardTester and caches them</para>
    /// <para>Stores successful backend index in <see cref="Settings.ClipboardCommandIndex"/> for persistence</para>
    /// </summary>

    public static class ClipboardManager
    {
        private static readonly object initLock = new();
        private static string? _getCommand;
        private static string? _setCommand;
        private static bool RetriedInit = false;

        public static void SetText(string text)
        {
            lock (initLock)
            {
                if (_setCommand == null) Init();
                if (_setCommand == null) return;

                try
                {
                    ExecuteSetCommand(_setCommand, text);
                }
                catch (Exception ex)
                {
                    Logger.Log("ClipboardManager", $"SetText failed: {ex.Message}", LogLevel.ERROR);

                    if (!RetriedInit)
                    {
                        Logger.Log("ClipboardManager", $"SetText failed, Retrying to get the correct backend again", LogLevel.INFO);
                        //Maybe clipboard backend changed - retry to init again
                        Settings.ClipboardCommandIndex = -1;
                        Init();
                        RetriedInit = true;
                        SetText(text);
                    }
                }
            }
        }

        public static string? GetText()
        {
            lock (initLock)
            {
                if (_getCommand == null) Init();
                if (_getCommand == null) return null;

                try
                {
                    return ExecuteGetCommand(_getCommand);
                }
                catch (Exception ex)
                {
                    Logger.Log("ClipboardManager", $"GetText failed: {ex.Message}", LogLevel.ERROR);

                    if (!RetriedInit)
                    {
                        Logger.Log("ClipboardManager", $"GetText failed, Retrying to get the correct backend again", LogLevel.INFO);
                        //Maybe clipboard backend changed - retry to init again
                        Settings.ClipboardCommandIndex = -1;
                        Init();
                        RetriedInit = true;
                        return GetText();
                    }
                    return null;
                }
            }
        }

        public static void Init()
        {
            if (Settings.ClipboardCommandIndex != -1)
            {
                var existingCommands = ClipboardTester.GetCommandByIndex(Settings.ClipboardCommandIndex);
                if (existingCommands != null)
                {
                    _getCommand = existingCommands.Value.GetCommand;
                    _setCommand = existingCommands.Value.SetCommand;
                    RetriedInit = false;
                    return;
                }
            }

            var commands = ClipboardTester.GetWorkingCommands();
            if (commands != null)
            {
                _getCommand = commands.Value.GetCommand;
                _setCommand = commands.Value.SetCommand;

                Settings.ClipboardCommandIndex = commands.Value.index;
                RetriedInit = false;

                Logger.Log("ClipboardManager", $"Found Backends: Get='{_getCommand}', Set='{_setCommand}'", LogLevel.INFO);
            }
            else
            {
                Logger.Log("ClipboardManager", "No working clipboard Backends found, supressing retries to find backend", LogLevel.ERROR);

                // no need to allow to retry in this case
                RetriedInit = true;
            }
        }

        private static string? ExecuteGetCommand(string command)
        {
            var parts = command.Split(' ', 2);
            var fileName = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : "";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
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
                throw new(process.StandardError.ReadToEnd());
            }

            if(result.Count() > 0)
            {
                result = result[..(result.Count() - 1)];//remove new line character
            }
            return result;
        }

        private static bool ExecuteSetCommand(string command, string text)
        {
            var parts = command.Split(' ', 2);
            var fileName = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : "";

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

            bool exited = process.WaitForExit(2000);

            if (!exited)
            {
                process.Kill();
                throw new(process.StandardError.ReadToEnd());
            }

            return process.ExitCode == 0;
        }
    }
}