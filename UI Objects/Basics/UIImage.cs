using System.Numerics;
using System.Runtime.InteropServices;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class UIImage : UIObject
    {
        private static BufferObject<RenderManager.ScreenVertex>? _sharedVbo;
        private static BufferObject<uint>? _sharedEbo;
        public VertexArrayObject _vao;
        private readonly GL _gl;
        public bool _nineSlice { get; private set; }
        public Vector2 _nineSliceBorder { get; private set; } = new(32, 16);
        public Action? DragStartAction;
        public Action? DragAction;
        public Action? DragEndAction;

        private static readonly RenderManager.ScreenVertex[] _quadVertices =
        [
            new() { Position = new Vector3(-0.5f, -0.5f, 0), UV = new Vector2(0, 0) },
            new() { Position = new Vector3( 0.5f, -0.5f, 0), UV = new Vector2(1, 0) },
            new() { Position = new Vector3( 0.5f,  0.5f, 0), UV = new Vector2(1, 1) },
            new() { Position = new Vector3(-0.5f,  0.5f, 0), UV = new Vector2(0, 1) }
        ];

        private static readonly uint[] _quadIndices =
        [
            0, 1, 2,
            0, 2, 3
        ];

        private static void EnsureSharedBuffers(GL gl)
        {
            if (_sharedVbo == null)
            {
                _sharedVbo = new BufferObject<RenderManager.ScreenVertex>(gl, _quadVertices, BufferTargetARB.ArrayBuffer);
                _sharedEbo = new BufferObject<uint>(gl, _quadIndices, BufferTargetARB.ElementArrayBuffer);
            }
        }

        public UIImage(Texture? texture = null, bool screenSpace = false, bool nineSlice = false, Vector2? NineSliceBorder = null)
        {
            _gl = ShaderManager.gl;

            if (nineSlice)
            {
                Shader = ShaderManager.GetShaderByName("nine-slice");
                _nineSlice = true;
                _nineSliceBorder = NineSliceBorder ?? new(37, 16);
            }
            else
            {
                Shader = ShaderManager.GetShaderByName("Default Shader");
            }

            if (texture == null && !nineSlice)
            {
                Texture = TextureHandler.GetEmbeddedTextureByName("default.png");
            }
            else if (texture == null && nineSlice)
            {
                Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png");
            }
            else
            {
                Texture = texture;
            }

            EnsureSharedBuffers(_gl);

            _vao = new VertexArrayObject(_gl);
            _vao.Bind();
            _sharedVbo!.Bind();
            _sharedEbo!.Bind();

            int stride = Marshal.SizeOf<RenderManager.ScreenVertex>();
            _vao.SetVertexAttribute<RenderManager.ScreenVertex>(0, 3, VertexAttribPointerType.Float, stride, 0);
            _vao.SetVertexAttribute<RenderManager.ScreenVertex>(1, 2, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<RenderManager.ScreenVertex>(nameof(RenderManager.ScreenVertex.UV)).ToInt32());

            _vao.Unbind();

            IsScreenSpace = screenSpace;
        }

        public unsafe override void Render()
        {
            if (!IsVisible) return;

            _vao.Bind();
            Shader?.Use();

            Shader?.SetUniform("uTexture0", 0);
            if (IsScreenSpace)
            {
                Shader?.SetUniform("uView", Camera.GetStationalViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetStationalProjectionMatrix());
            }
            else
            {
                Shader?.SetUniform("uView", Camera.GetViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
            }
            Shader?.SetUniform("uModel", Transform.ViewMatrix);
            Shader?.SetUniform("uColor", Settings.ColorToVec4(TextureColor));

            if (_nineSlice)
            {
                Shader?.SetUniform("uDimensions", Transform.Scale);
                Shader?.SetUniform("uBorderSize", _nineSliceBorder);
            }

            Texture?.Bind();

            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);

            var error = _gl.GetError();
            if (error != GLEnum.NoError)
            {
                // No logging here, to prevent huge log files
                //Console.WriteLine($"OpenGL Error after drawing: {error}");
            }
        }

        public override void Dispose()
        {
            _vao.Dispose();
            IsDisposed = true;
            IsVisible = false;
        }

        public override void OnDragStart()
        {
            DragStartAction?.Invoke();
        }

        public override void OnDrag()
        {
            DragAction?.Invoke();
        }

        public override void OnDragEnd()
        {
            DragEndAction?.Invoke();
        }
    }
}