/// <summary>
/// <para>Manages loading, caching, and disposal of texture resources from embedded resources aswell as from files via a given path</para>
/// <para>Automatically discovers and loads embedded image resources (.png, .jpg, .jpeg) from the assembly. See <see cref="GetEmbeddedTextureByName(string)"/></para>
/// <para>See <see cref="GetTextureOrCreateBasedOnPath(string)"/> for path based Texture loading</para>
/// <para>Caches textures to avoid redundant loading</para>
/// </summary>

public static class TextureHandler
{
    private static Dictionary<string, Texture> _textures = [];
    private static Dictionary<string, Texture> _texturesLoadedFromPath = [];

    public static void GetAllTextures()
    {
        var textureResources = ResourceHelper.ManifestResources.Where(x => x.Contains(".Textures."))
            .Where(x => x.EndsWith("png") || x.EndsWith("jpg") || x.EndsWith("jpeg"));

        foreach (var resourceName in textureResources)
        {
            try
            {
                var texBytes = ResourceHelper.LoadEmbeddedTexture(resourceName);
                if (texBytes == null || texBytes.Length <= 0)
                {
                    Logger.Log("TextureHandler", $"Failed to load texture: {resourceName}", LogLevel.ERROR);
                    continue;
                }

                Texture createdTex = new(texBytes);

                string key = resourceName;

                var parts = key.Split('.');
                if (parts.Length >= 2)
                {
                    // name + extension
                    key = $"{parts[^2]}.{parts[^1]}";
                    //Debug.WriteLine(key);
                }

                _textures[key] = createdTex;

            }
            catch (Exception ex)
            {
                Logger.Log("TextureHandler", $"Error loading texture {resourceName}: {ex.Message}", LogLevel.ERROR);
            }
        }
    }

    public static Texture? GetEmbeddedTextureByName(string textureName)
    {
        if (_textures.TryGetValue(textureName, out var texture))
        {
            return texture;
        }

        Logger.Log("TextureHandler", $"Texture not found: {textureName}", LogLevel.WARNING);
        return null;
    }

    public static Texture? GetTextureOrCreateBasedOnPath(string path)
    {
        if (_texturesLoadedFromPath.TryGetValue(path, out var texture))
        {
            return texture;
        }

        if (!File.Exists(path))
        {
            Logger.Log("TextureHandler", $"Texture could not be loaded from path: {path}: file does not exist", LogLevel.WARNING);
            return null;
        }

        Texture? createdTex = null;

        try
        {
            createdTex = new(path);
            _texturesLoadedFromPath.Add(path, createdTex);
            return createdTex;
        }
        catch (Exception ex)
        {
            Logger.Log("TextureHandler", $"Texture could not be loaded. path: {path}\nerror:{ex.Message}\nstacktrace:\n{ex.StackTrace}", LogLevel.WARNING);
            createdTex?.Dispose();
            return null;
        }
    }

    public static void DisposeTextureLoadedFromPath()
    {
        foreach (Texture tex in _texturesLoadedFromPath.Values)
        {
            tex?.Dispose();
        }
        _texturesLoadedFromPath.Clear();
    }

    public static void Dispose()
    {
        foreach (Texture tex in _textures.Values)
        {
            tex?.Dispose();
        }

        foreach (Texture tex in _texturesLoadedFromPath.Values)
        {
            tex?.Dispose();
        }
        _textures.Clear();
    }
}