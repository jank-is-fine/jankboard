using Silk.NET.OpenGL;

namespace Managers
{
    /// <summary>
    /// <para>Central shader management system that loads, stores, and provides access to all shaders</para>
    /// <para>Manages font handling and text rendering initialization through FontHandler and TextRenderer</para>
    /// <para>Exposes function for shader retrieval by name</para>
    /// <para>Gets the config from <see cref="ShaderConfig"/></para>
    /// <para><see cref="Init()"/> needs to be called after Window creation</para>
    /// </summary>

    public static class ShaderManager
    {
        public static GL gl = null!;
        private static List<Shader> Shaders = [];
        public static FontHandler FontHandler = null!;


        public static void Init()
        {
            gl = GL.GetApi(WindowManager.window);

            Dictionary<string, (string, string)> ShaderDictonary = ShaderConfig.GetShaders();


            foreach (var pair in ShaderDictonary)
            {
                try
                {
                    if (string.IsNullOrEmpty(pair.Value.Item1) || string.IsNullOrEmpty(pair.Value.Item2))
                    {
                        Logger.Log("ShaderManager", $"Shader: {pair} has null or empty fields", LogLevel.WARNING);
                        continue;
                    }

                    var shader = new Shader(pair.Key, gl, pair.Value.Item1, pair.Value.Item2);
                    Shaders.Add(shader);
                }
                catch (Exception ex)
                {
                    Logger.Log("ShaderManager", $"Failed to load shader {pair.Value}: {ex.Message}", LogLevel.FATAL);
                }
            }

            InitializeTextRendering();
        }

        private static void InitializeTextRendering()
        {
            try
            {
                FontHandler = new FontHandler();

                if (!TryGetShaderByName("Text Shader", out var msdfShader) || msdfShader == null)
                {
                    Logger.Log("ShaderManager", "Text Shader not found", LogLevel.FATAL);
                    return;
                }

                var defaultFontCollection = FontHandler.GetDefaultFont();
                if (defaultFontCollection == null)
                {
                    Logger.Log("ShaderManager", "Default Font not found!", LogLevel.FATAL);
                    return;
                }
                TextRenderer.Init(gl, defaultFontCollection, msdfShader);

                Logger.Log("ShaderManager", $"Text rendering initialized with font collection: {defaultFontCollection}", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Logger.Log("ShaderManager", $"Failed to initialize text rendering: {ex.Message}\nStack trace: {ex.StackTrace}", LogLevel.FATAL);
            }
        }


        public static void SetCurrentFont(string fontName)
        {
            if (FontHandler.SetCurrentFont(fontName))
            {
                var newFont = FontHandler.GetFontCollection(fontName);
                if (newFont != null)
                {
                    TextRenderer.SetFont(newFont);
                    Logger.Log("ShaderManager", $"Switched to font: {fontName}", LogLevel.INFO);
                }
                else
                {
                    Logger.Log("ShaderManager", $"Could not set Font: {fontName}", LogLevel.WARNING);
                }
            }
            else
            {
                Logger.Log("ShaderManager", $"Font not found: {fontName}", LogLevel.WARNING);
            }
        }


        public static bool TryGetShaderByName(string ShaderName, out Shader? shader)
        {
            shader = null;

            Shader? FoundShader = Shaders.Find(x => x.ShaderName == ShaderName);

            if (FoundShader != null)
            {
                shader = FoundShader;
                return true;
            }

            return false;
        }

        public static Shader? GetShaderByName(string ShaderName)
        {
            return Shaders.Find(x => x.ShaderName == ShaderName);
        }

        public static void Dispose()
        {
            foreach (var shader in Shaders)
            {
                shader?.Dispose();
            }
            Shaders.Clear();
            FontHandler?.Dispose();
            TextRenderer.Dispose();
            gl.Dispose();
        }
    }
}