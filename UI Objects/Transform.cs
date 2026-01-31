using System.Numerics;

public class Transform
{
    /// <summary>
    /// <para>Directly taken from the Silk.net examples and adjusted for 2D space</para>
    /// <para>See <see cref="https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.5%20-%20Transformations/Transform.cs"/></para>
    /// <para>See also <seealso cref="https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.5%20-%20Transformations"/></para>
    /// </summary>

    //For a transform we need to have a position, a scale, and a rotation,
    //depending on what application you are creating, the type for these may vary.

    //Here we have chosen a vec3 for position, float for scale and quaternion for rotation,
    //as that is the most normal to go with.
    //Another example could have been vec3, vec3, vec4, so the rotation is an axis angle instead of a quaternion

    public Vector2 Position { get; set; } = new Vector2(0, 0);
    public Vector2 Scale { get; set; } = new Vector2(1f, 1f);
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    public Matrix4x4 ViewMatrix
    {
        get
        {
            var matrix = Matrix4x4.CreateScale(Scale.X, Scale.Y, 1f) *
                        Matrix4x4.CreateFromYawPitchRoll(Rotation.X,Rotation.Y,Rotation.Z) *
                        Matrix4x4.CreateTranslation(Position.X, Position.Y, 0);

            return matrix;
        }
    }
}