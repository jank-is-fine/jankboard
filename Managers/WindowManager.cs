using System.Diagnostics;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Rendering.UI;
using Managers.Clipboard;
using System.Reflection;
using Silk.NET.Core;
using ImageMagick;
using Silk.NET.Windowing.Sdl;

namespace Managers
{
    /// <summary>
    /// <para>Entry Point for the application see <see cref="Main()"/></para>
    /// <para>Handles window creation, input device initialization, and core component startup sequence</para>
    /// </summary>

    public static class WindowManager
    {
        #region Variables and references

        public static IWindow window = null!;
        public static Vector2 GetWindowSize() => new(window.Size.X, window.Size.Y);
        private static Stopwatch? AppTime;
        public static SettingsScene SettingsMenu = null!;
        public static MainMenuScene MainMenu = null!;
        public static MainScene MainScene = null!;

        public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();


        #endregion Variables and references

        private static void Main()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = Settings.ApplicationName;
            SdlWindowing.Use();
            //options.WindowState = WindowState.Fullscreen;
            window = Window.Create(options);

            window.Update += OnUpdate;
            window.Load += OnLoad;
            window.FramebufferResize += OnFramebufferResize;
            window.Closing += OnClose;

            window.Run();
            window.Dispose();
        }

        private static void OnLoad()
        {
            //ResourceHelper.LogAllResources();

            InputDeviceHandler.Init(window);
            UIobjectHandler.Init();

            ChunkManager.Init(1024);
            ShaderManager.Init();

            TextureHandler.GetAllTextures();

            Camera.Init(window.FramebufferSize);
            Camera.Position = Vector2.Zero;
            Camera.Zoom = 1f;

            _ = new Toolbar(window);

            ConnectionManager.Init();

            GroupManager.Init();

            _ = new ContextMenu();

            MainMenu = new();
            MainScene = new();
            SettingsMenu = new();

            EntryManager.Init();

            RenderManager.Init([MainMenu, SettingsMenu, MainScene]);

            AppTime = Stopwatch.StartNew();
            //FpsCounter.Enable();
            //ShaderManager.SetCurrentFont("OpenSans");

            OutlineRender.Init();

            if (SaveManager.LoadSettings())
            {
                RenderManager.ChangeScene("Main Menu");
            }
            else
            {
                var FirstLaunchScene = new FirstLaunchScene();
                RenderManager.AddScene(FirstLaunchScene);
                RenderManager.ChangeScene(FirstLaunchScene.SceneName);
            }

            //UndoRedoManager.TestUndoRedo();
            ClipboardManager.Init();

            AudioHandler.Init();
            AudioHandler.PlaySound("maximize_008");

            SetWindowIcon();
        }

        private static void SetWindowIcon()
        {
            var stream = ResourceHelper.LoadEmbeddedStream("jankboard-icon");

            if (stream == null) { return; }

            using var collection = new MagickImageCollection(stream);
            var image = collection[0];

            image.Format = MagickFormat.Rgba;
            using var pixels = image.GetPixels();
            var pixelData = pixels.ToByteArray(0, 0, image.Width, image.Height, "RGBA");

            var icon = new RawImage((int)image.Width, (int)image.Height, pixelData);
            window.SetWindowIcon([icon]);
        }

        private static void OnUpdate(double deltaTime)
        {
            FPSCounter.Update(deltaTime);
        }

        private static void OnFramebufferResize(Vector2D<int> newSize)
        {
            ShaderManager.gl.Viewport(newSize);
            Camera.Resize(newSize);
            RenderManager.OnFramebufferResize(newSize);
        }

        public static void RecalcSize()
        {
            ShaderManager.gl.Viewport(window.Size);
            RenderManager.OnFramebufferResize(window.Size);
        }

        public static void ApplicationQuit()
        {
            AudioHandler.PlaySound("maximize_008");
            window.Close();
        }

        private static void OnClose()
        {
            window.Closing -= OnClose;

            TextureHandler.Dispose();
            window.Update -= OnUpdate;
            ChunkManager.Clear();
            AudioHandler.Dispose();
        }


        public static bool TryGetCurrentAppTime(out float CurrentApptime)
        {
            if (AppTime == null) { CurrentApptime = -100; return false; }

            CurrentApptime = (float)AppTime.Elapsed.TotalSeconds;

            return true;
        }
    }
}