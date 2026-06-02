using Silk.NET.OpenGL;

/// <summary>
/// <para>Based on Silk.Net examples</para> 
/// <para>See <see cref="https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.5%20-%20Transformations"/></para>
/// </summary>

public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
{
    private uint _handle;
    private BufferTargetARB _bufferType;
    private GL _gl;
    private nuint _allocatedBytes;

    public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
    {
        _gl = gl;
        _bufferType = bufferType;

        _handle = _gl.GenBuffer();
        Bind();
        fixed (void* d = data)
        {
            _allocatedBytes = (nuint)(data.Length * sizeof(TDataType));

            _gl.BufferData(
                bufferType,
                _allocatedBytes,
                d,
                BufferUsageARB.DynamicDraw);
        }
    }


    public unsafe void BufferData(Span<TDataType> data)
    {
        var requiredBytes =
     (nuint)(data.Length * sizeof(TDataType));

        Bind();

        if (requiredBytes > _allocatedBytes)
        {
            _allocatedBytes = requiredBytes;

            fixed (void* d = data)
            {
                _gl.BufferData(
                    _bufferType,
                    _allocatedBytes,
                    d,
                    BufferUsageARB.DynamicDraw);
            }
        }
        else
        {
            fixed (void* d = data)
            {
                _gl.BufferSubData(
                    _bufferType,
                    0,
                    requiredBytes,
                    d);
            }
        }

        var err = _gl.GetError();
        if (err != GLEnum.NoError)
        {
            Console.WriteLine(err);
        }
    }

    public void Bind()
    {
        _gl.BindBuffer(_bufferType, _handle);
    }

    public void Unbind()
    {
        _gl.BindBuffer(_bufferType, 0);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(_handle);
    }
}