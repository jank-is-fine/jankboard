using System.Numerics;
using Silk.NET.OpenGL;

public class Shader : IDisposable
{
    public string ShaderName;
    private uint _handle;
    private GL _gl;
    private uint VertexShader;
    private uint FragmentShader;

    public Shader(string name, GL gl, string vertex, string fragment)
    {
        _gl = gl;
        ShaderName = name;
            
        //No need to try catch here, ShaderManager should catch it
        VertexShader = LoadShader(ShaderType.VertexShader, vertex);
        FragmentShader = LoadShader(ShaderType.FragmentShader, fragment);

        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, VertexShader);
        _gl.AttachShader(_handle, FragmentShader);
        _gl.LinkProgram(_handle);

        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(_handle);

            //No need to log here - shadermanager takes care of that            
            throw new Exception($"Program failed to link with error: {infoLog}");
        }

        _gl.ValidateProgram(_handle);
        _gl.GetProgram(_handle, GLEnum.ValidateStatus, out status);
        if (status == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(_handle);
            Logger.Log("Shader", $"Program validation warning: {infoLog}", LogLevel.ERROR);
        }

        _gl.DetachShader(_handle, VertexShader);
        _gl.DetachShader(_handle, FragmentShader);
        _gl.DeleteShader(VertexShader);
        _gl.DeleteShader(FragmentShader);
    }

    public void Use()
    {
        _gl.UseProgram(_handle);
    }

    public void SetUniform(string name, int value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.Uniform1(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.UniformMatrix4(location, 1, false, (float*)&value);
    }

    public void SetUniform(string name, float value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.Uniform1(location, value);
    }
    public void SetUniform(string name, Vector2 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.Uniform2(location, value.X, value.Y);
    }


    public void SetUniform(string name, float x, float y, float z, float w)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.Uniform4(location, x, y, z, w);
    }

    public void SetUniform(string name, Vector4 vector4)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            //Console.WriteLine($"Warning: Uniform '{name}' not found in shader");
            return;
        }
        _gl.Uniform4(location, vector4.X, vector4.Y, vector4.Z, vector4.W);
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_handle);
        GC.SuppressFinalize(this);
    }

    private uint LoadShader(ShaderType type, string shader)
    {
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, shader);
        _gl.CompileShader(handle);
        string infoLog = _gl.GetShaderInfoLog(handle);

        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error:\n {infoLog}");
        }

        return handle;
    }
}