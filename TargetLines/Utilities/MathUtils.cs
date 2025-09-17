using System.Numerics;
using System.Runtime.CompilerServices;

namespace TargetLines.Utilities;

public static class MathUtils {
    public static readonly float RAD2DEG = 57.29577951308232f;
    public static readonly float DEG2RAD = 0.017453292519943f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerpf(float lhs, float rhs, float t) {
        return (1 - t) * lhs + t * rhs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float QuadraticLerpf(float lhs, float rhs, float t) {
        return lhs + (rhs - lhs) * t * t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CubicLerpf(float lhs, float rhs, float t) {
        float t3 = t * t * t;
        float one_minus_t3 = (1 - t) * (1 - t) * (1 - t);
        return lhs * one_minus_t3 + rhs * t3;
    }
}

