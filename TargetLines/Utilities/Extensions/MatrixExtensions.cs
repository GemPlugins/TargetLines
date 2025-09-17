using System.Numerics;
using SharpDX;

namespace TargetLines.Utilities.Extensions;

public static class MatrixExtensions
{
    public static Matrix ToSharpDX(this Matrix4x4 matrix)
    {
        return new Matrix(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    public static Matrix4x4 ToNumerics(this Matrix matrix)
    {
        return new Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    public static Matrix TransposeToSharpDX(this Matrix4x4 matrix)
    {
        var transposed = Matrix4x4.Transpose(matrix);
        return transposed.ToSharpDX();
    }

    public static Matrix FromNumerics(Matrix4x4 matrix)
    {
        return matrix.ToSharpDX();
    }

    public static Matrix4x4 FromSharpDX(Matrix matrix)
    {
        return matrix.ToNumerics();
    }

    public static SharpDX.Vector3 ToSharpDX(this System.Numerics.Vector3 vector)
    {
        return new SharpDX.Vector3(vector.X, vector.Y, vector.Z);
    }

    public static System.Numerics.Vector3 ToNumerics(this SharpDX.Vector3 vector)
    {
        return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Matrix4x4 ToSystem(this FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math.Matrix4x3 m)
    {
        return new Matrix4x4(
            m.M11, m.M12, m.M13, 0,
            m.M21, m.M22, m.M23, 0,
            m.M31, m.M32, m.M33, 0,
            m.M41, m.M42, m.M43, 1
        );
    }

    public static Matrix4x4 ToSystem(this FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4 m)
    {
        return new Matrix4x4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }
}