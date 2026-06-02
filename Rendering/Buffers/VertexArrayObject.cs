using Silk.NET.OpenGL;

public class VertexArrayObject(GL gl) : IDisposable
{
    private readonly uint _handle = gl.GenVertexArray();
    private readonly GL _gl = gl;

    public void Bind() => _gl.BindVertexArray(_handle);
    public void Unbind() => _gl.BindVertexArray(0);
    public void Dispose() => _gl.DeleteVertexArray(_handle);

    public unsafe void SetVertexAttribute<T>(
        uint index,
        int count,
        VertexAttribPointerType type,
        int stride,
        int offset)
        where T : unmanaged
    {
        _gl.VertexAttribPointer(
            index,
            count,
            type,
            false,
            (uint)stride,
            (void*)offset);

        _gl.EnableVertexAttribArray(index);
    }

    public void VertexAttribDivisor(uint index, uint divisor)
    {
        _gl.VertexAttribDivisor(index, divisor);
    }
}