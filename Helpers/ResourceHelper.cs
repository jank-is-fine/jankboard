using Managers;

/// <summary>
/// <para>Helper for loading (and retrieving) embedded resources from the assembly manifest</para>
/// <para>See <see cref="GetEmbeddedText(string)"/></para>
/// <para>See <see cref="LoadEmbeddedTexture(string)"/></para>
/// <para>See <see cref="LoadEmbeddedStream(string)"/></para>
/// <para>See <see cref="LoadEmbeddedBytes(string)"/></para>
/// </summary>

public static class ResourceHelper
{
    public static List<string> ManifestResources {get; private set;} = [];

    private static void GetAllResources()
    {
        ManifestResources.AddRange(WindowManager.Assembly.GetManifestResourceNames());
    }

    public static string GetEmbeddedText(string resourcePath)
    {
        using var stream = WindowManager.Assembly.GetManifestResourceStream(resourcePath);

        if (stream == null)
        {
            var parts = resourcePath.Split('.');
            var resourceName = resourcePath;

            if (parts.Length >= 2)
            {
                resourceName = $"{parts[^2]}.{parts[^1]}";
            }

            if(ManifestResources.Count <= 0)
            {
                GetAllResources();
            }

            var foundResource = ManifestResources
            .Where(x => x.EndsWith(resourceName));

            if (foundResource == null || !foundResource.Any()) { return ""; }
            else
            {
                using var streamFromResourceName = WindowManager.Assembly.GetManifestResourceStream(foundResource.First());
                if (streamFromResourceName != null)
                {
                    using var readerFromResourceName = new StreamReader(streamFromResourceName);
                    var sourceFromResourceName = readerFromResourceName.ReadToEnd();
                    return sourceFromResourceName;
                }
            }
            Logger.Log("ResourceHelper", $"Resource not found: {resourcePath}", LogLevel.ERROR);
            return "";
        }

        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();

        return source;
    }

    public static byte[]? LoadEmbeddedTexture(string resourceName)
    {
        using var stream = WindowManager.Assembly.GetManifestResourceStream(resourceName);
        using var memoryStream = new MemoryStream();
        if (stream == null) return null;

        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static Stream? LoadEmbeddedStream(string resourceName)
    {
        return WindowManager.Assembly.GetManifestResourceStream(resourceName);
    }

    public static byte[]? LoadEmbeddedBytes(string resourceName)
    {
        using var s = LoadEmbeddedStream(resourceName);
        if (s == null) return null;
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}