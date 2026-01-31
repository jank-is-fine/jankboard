using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Managers
{
    /// <summary>
    /// <para>Manager for the main render pass and the post-processing passes</para>
    /// <para>See <see cref="RenderPass"/> abstraction for the Render Pass creation</para>
    /// </summary>

    public static class RenderManager
    {
        private static List<Scene> Scenes = [];
        private static List<RenderPass> renderPasses = [];
        public static Scene CurrentScene = null!;
        private static GL Gl = null!;
        private static IWindow Window = null!;
        private static uint MainframeBuffer;
        private static uint MainColorTexture;
        private static string? LastScene;


        private static readonly float[] Vertices =
        [
            // X      Y     Z     U     V
            -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,  // Bottom-left
             1.0f, -1.0f, 0.0f, 1.0f, 0.0f,  // Bottom-right
             1.0f,  1.0f, 0.0f, 1.0f, 1.0f,  // Top-right
             1.0f,  1.0f, 0.0f, 1.0f, 1.0f,  // Top-right
            -1.0f,  1.0f, 0.0f, 0.0f, 1.0f,  // Top-left
            -1.0f, -1.0f, 0.0f, 0.0f, 0.0f   // Bottom-left
        ];

        public static BufferObject<float> ScreenVbo = null!;
        public static BufferObject<uint> ScreenEbo = null!;
        public static VertexArrayObject<float, uint> ScreenVao = null!;

        private static FinalPass finalPass = new();

        public static void Init(List<Scene> AvailibleScenes)
        {
            Gl = ShaderManager.gl;
            Window = WindowManager.window;
            CurrentScene = WindowManager.MainMenu;
            CurrentScene.SubActions();

            Scenes = AvailibleScenes;

            InitEvents();
            InitFBO();
        }

        private static unsafe void InitFBO()
        {
            // https://learnopengl.com/Advanced-OpenGL/Framebuffers
            ScreenVbo = new BufferObject<float>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
            ScreenEbo = new BufferObject<uint>(Gl, [], BufferTargetARB.ElementArrayBuffer);
            ScreenVao = new VertexArrayObject<float, uint>(Gl, ScreenVbo, ScreenEbo);

            ScreenVao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0); // Position
            ScreenVao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3); // UV

            ScreenVao.Unbind();

            MainframeBuffer = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, MainframeBuffer);

            MainColorTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, MainColorTexture);

            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, 800, 600, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, MainColorTexture, 0);

            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                Logger.Log("RenderHandler", "Framebuffer not complete!", LogLevel.ERROR);
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            var RenderPasses = ShaderConfig.GetRenderPasses();

            foreach (var renderpass in RenderPasses)
            {
                var CreatedframeBuffer = Gl.GenFramebuffer();
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, CreatedframeBuffer);

                uint colorTexture = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2D, colorTexture);

                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

                Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, 800, 600, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTexture, 0);

                renderpass.colorTexture = colorTexture;
                renderpass.frameBuffer = CreatedframeBuffer;
                renderPasses.Add(renderpass);

                if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
                {
                    Logger.Log("RenderHandler", $"Framebuffer for {renderpass.GetType().Name} not complete!", LogLevel.FATAL);
                }
            }

            // just in case
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        public static unsafe void OnFramebufferResize(Vector2D<int> newSize)
        {
            Gl.BindTexture(TextureTarget.Texture2D, MainColorTexture);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)newSize.X, (uint)newSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

            foreach (var renderpass in renderPasses)
            {
                if (renderpass.colorTexture.HasValue)
                {
                    Gl.BindTexture(TextureTarget.Texture2D, renderpass.colorTexture.Value);
                    Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)newSize.X, (uint)newSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
                }
            }

            // Unbind
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            foreach (var scene in Scenes)
            {
                scene?.RecalcSize();
                scene?.RecalcLayout();
            }
        }

        public static void InitEvents()
        {
            Window.Render += OnRender;
            Window.Closing += OnClose;
        }

        public static void AddScene(Scene target)
        {
            if (!Scenes.Contains(target))
            {
                Scenes.Add(target);
            }
        }

        public static void RemoveScene(Scene target)
        {
            Scenes.Remove(target);
        }

        public static void LoadLastScene()
        {
            if (LastScene != null)
            {
                ChangeScene(LastScene);
            }
        }

        public static void ChangeScene(Scene target)
        {
            OutlineRender.Clear();
            UIobjectHandler.ClearHoeverObject();
            SelectionManager.ClearSelection();
            CurrentScene?.UnsubActions();

            if (CurrentScene != null)
            {
                LastScene = CurrentScene.SceneName;
            }

            CurrentScene = target;
            target.SubActions();
        }

        public static void ChangeScene(string target)
        {
            if (Scenes.Count <= 0) { return; }
            foreach (var s in Scenes)
            {
                if (s.SceneName == target)
                {
                    ChangeScene(s);
                }
            }
        }

        private static void OnRender(double deltaTime)
        {
            ExecuteMainPass();
            ExecutePostPass();
            FPSCounter.Render();
        }

        private static void ExecuteMainPass()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, MainframeBuffer);
            Clear();
            CurrentScene?.Render();
        }

        private static void ExecutePostPass()
        {
            ScreenVao.Bind();

            uint previousTexture = MainColorTexture;

            foreach (var renderpass in renderPasses)
            {
                if (renderpass.frameBuffer == null || !renderpass.IsActive)
                {
                    continue;
                }

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)renderpass.frameBuffer);

                Clear();

                if (renderpass.ScreenShader == null)
                {
                    continue;
                }

                renderpass.ScreenShader.Use();

                renderpass.ScreenShader.SetUniform("uscreenTexture", 0);

                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, previousTexture);

                renderpass.SetupPass();

                Gl.DrawArrays(GLEnum.Triangles, 0, 6);

                previousTexture = (uint)renderpass.colorTexture!;
            }

            // Final pass
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Clear();

            if (finalPass.ScreenShader == null)
            {
                Logger.Log("RenderHandler", "FinalPass has no shader!", LogLevel.FATAL);
                return;
            }

            finalPass.ScreenShader.Use();

            finalPass.ScreenShader.SetUniform("uscreenTexture", 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, previousTexture);

            finalPass.SetupPass();

            Gl.DrawArrays(GLEnum.Triangles, 0, 6);

            ScreenVao.Unbind();
        }

        private static void Clear()
        {
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.ClearColor(Settings.BackgroundColor);
        }

        private static void OnClose()
        {
            Window.Render -= OnRender;
            Window.FramebufferResize -= OnFramebufferResize;

            Gl.DeleteFramebuffer(MainframeBuffer);
            Gl.DeleteTexture(MainColorTexture);

            foreach (var renderpass in renderPasses)
            {
                if (renderpass.frameBuffer.HasValue)
                    Gl.DeleteFramebuffer(renderpass.frameBuffer.Value);
                if (renderpass.colorTexture.HasValue)
                    Gl.DeleteTexture(renderpass.colorTexture.Value);
            }

            foreach (var scene in Scenes)
            {
                scene.Dispose();
            }
        }
    }

}