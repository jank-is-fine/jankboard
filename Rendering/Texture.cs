using Managers;
using Silk.NET.OpenGL;
using ImageMagick;

/// <summary>
/// <para>Manages OpenGL texture loading and rendering, supporting both static and animated images</para>
/// <para>Uses ImageMagick for loading image formats and GIFs</para>
/// <para>Each Texture which loads a GIF subscribes to window updates</para>
/// </summary>

public class Texture : IDisposable
{
    private uint _handle;
    private GL _gl;
    private List<uint>? _frameHandles;
    private List<int>? _frameDelays;
    private int _currentFrame = 0;
    private double _frameTimer = 0;
    private bool _isAnimated = false;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsAnimated => _isAnimated;

    public Texture(byte[] textureData)
    {
        _gl = ShaderManager.gl;

        if (textureData == null || textureData.Length == 0)
        {
            return;
        }

        using var stream = new MemoryStream(textureData);
        using var collection = new MagickImageCollection(stream);

        if (collection.Count > 1)
        {
            CreateAnimatedTexture(collection);
        }
        else
        {
            CreateStaticTexture(collection[0]);
        }
    }

    public Texture(string path)
    {
        _gl = ShaderManager.gl;

        if (!File.Exists(path))
        {
            Logger.Log("Texture", $"Texture not found: {path}\n creating fallback Texture", LogLevel.WARNING);
            return;
        }

        using var collection = new MagickImageCollection(path);

        if (collection.Count > 1)
        {
            CreateAnimatedTexture(collection);
        }
        else
        {
            CreateStaticTexture(collection[0]);
        }
    }

    private unsafe void CreateAnimatedTexture(MagickImageCollection collection)
    {
        _isAnimated = true;
        _frameHandles = [];
        _frameDelays = [];

        Width = (int)collection[0].Width;
        Height = (int)collection[0].Height;

        foreach (var frame in collection)
        {
            var frameHandle = _gl.GenTexture();
            _frameHandles.Add(frameHandle);

            var frameDelay = frame.AnimationDelay <= 0 ? 1 : frame.AnimationDelay;
            _frameDelays.Add((int)(frameDelay * 10));

            _gl.BindTexture(TextureTarget.Texture2D, frameHandle);

            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8,
                (uint)Width, (uint)Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, null);

            frame.Format = MagickFormat.Rgba;
            using var pixels = frame.GetPixels();
            var dataArray = pixels.ToByteArray(0, 0, (uint)Width, (uint)Height, "RGBA");

            if (dataArray == null) { return; }
            int bytesPerRow = Width * 4;
            for (int y = 0; y < Height; y++)
            {
                fixed (void* data = &dataArray[y * bytesPerRow])
                {
                    _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y,
                        (uint)Width, 1, PixelFormat.Rgba,
                        PixelType.UnsignedByte, data);
                }
            }

            SetParameters();
        }
        WindowManager.window.Update += Update;
        _handle = _frameHandles[0];
    }

    private unsafe void CreateStaticTexture(IMagickImage<byte> image)
    {
        _isAnimated = false;
        _handle = _gl.GenTexture();
        Bind();

        Width = (int)image.Width;
        Height = (int)image.Height;

        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8,
            (uint)Width, (uint)Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, null);

        image.Format = MagickFormat.Rgba;
        using var pixels = image.GetPixels();
        var dataArray = pixels.ToByteArray(0, 0, (uint)Width, (uint)Height, "RGBA");

        if (dataArray == null) { return; }

        int bytesPerRow = Width * 4;
        for (int y = 0; y < Height; y++)
        {
            fixed (void* data = &dataArray[y * bytesPerRow])
            {
                _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y,
                    (uint)Width, 1, PixelFormat.Rgba,
                    PixelType.UnsignedByte, data);
            }
        }

        SetParameters();
    }

    public void Update(double deltaTime)
    {
        if (!_isAnimated || _frameDelays == null || _frameHandles == null || _frameDelays.Count == 0)
            return;

        _frameTimer += deltaTime * 1000;
        int currentDelay = _frameDelays[_currentFrame];

        if (_frameTimer > currentDelay)
        {
            _frameTimer -= currentDelay;
            _currentFrame = (_currentFrame + 1) % _frameHandles.Count;
            _handle = _frameHandles[_currentFrame];
        }
    }

    public void SetFrame(int frame)
    {
        if (_isAnimated && _frameHandles != null && frame >= 0 && frame < _frameHandles.Count)
        {
            _currentFrame = frame;
            _handle = _frameHandles[_currentFrame];
            _frameTimer = 0;
        }
    }

    public int GetCurrentFrameDelay() =>
        _isAnimated && _frameDelays != null && _frameDelays.Count > 0
            ? _frameDelays[_currentFrame]
            : 0;

    public int GetFrameCount() =>
        _isAnimated && _frameHandles != null ? _frameHandles.Count : 1;

    private void SetParameters()
    {
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
    }

    public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
    {
        _gl.ActiveTexture(textureSlot);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose()
    {
        if (_isAnimated && _frameHandles != null)
        {
            foreach (var handle in _frameHandles)
            {
                _gl.DeleteTexture(handle);
            }
            _frameHandles.Clear();
        }
        else if (_handle != 0)
        {
            _gl.DeleteTexture(_handle);
            _handle = 0;
        }

        WindowManager.window.Update -= Update;
    }
}