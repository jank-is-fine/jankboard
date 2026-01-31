public static class Logger
{
    public static void Log(string Source, string Error, LogLevel severity)
    {
        if ((int)severity < (int)Settings.LogLevel)
        {
            return;
        }

        var targetLogPath = $"{Settings.ApplicationName} Resources{Path.DirectorySeparatorChar}{Settings.LogBasePath}{Path.DirectorySeparatorChar}{severity}";
        if (!Directory.Exists(targetLogPath))
        {
            Directory.CreateDirectory(targetLogPath);
        }

        var logFileName = $"{severity}-{DateTime.Today}.txt";

        try
        {
            using var filestream = File.AppendText($"{targetLogPath}{Path.DirectorySeparatorChar}{logFileName}");
            filestream.Write($"\nSource : {Source}\n Error/Stacktrace:\n {Error}\n==========\n");
        }
        catch (Exception)
        {
            //cannot log - skip T-T       
        }
    }
}

public enum LogLevel
{
    INFO = 0,
    WARNING = 1,
    ERROR = 2,
    FATAL = 3
}