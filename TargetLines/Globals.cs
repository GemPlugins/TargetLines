using DrahsidLib;
using System;
using System.Numerics;
using TargetLines.Rendering;

namespace TargetLines;

public class Globals {
    public static Configuration Config { get; set; } = null!;
    public static Renderer Renderer { get; set; } = null!;

    public static unsafe CSFramework* Framework => CSFramework.Instance();

    public static void Initialize()
    {
        Renderer = new Renderer();
    }

    public static void Dispose()
    {
        ShaderSingleton.Dispose();
        Renderer?.Dispose();
    }

    public static unsafe Vector3 WorldCamera_GetPos() {
        if (Service.CameraManager->Camera == null) {
            return new Vector3(0, 0, 0);
        }

        return new Vector3(
            Service.CameraManager->Camera->CameraBase.X,
            Service.CameraManager->Camera->CameraBase.Z,
            Service.CameraManager->Camera->CameraBase.Y
        );
    }

    public static unsafe Vector3 WorldCamera_GetLookAtPos() {
        if (Service.CameraManager->Camera == null) {
            return new Vector3(0, 0, 0);
        }

        return new Vector3(Service.CameraManager->Camera->CameraBase.LookAtX, Service.CameraManager->Camera->CameraBase.LookAtZ, Service.CameraManager->Camera->CameraBase.LookAtY);
    }

    public static unsafe Vector3 WorldCamera_GetForward() {
        if (Service.CameraManager->Camera == null) {
            return new Vector3(0, 0, 1);
        }

        return Vector3.Normalize(WorldCamera_GetPos() - WorldCamera_GetLookAtPos());
    }

    public static unsafe bool IsInFirstPerson() {
        if (Service.CameraManager->Camera != null) {
            if (Service.CameraManager->Camera->Mode == 0) {
                return true;
            }
        }

        return false;
    }

    public static int AlignSizeTo16Bytes<T>() where T : struct
    {
        var size = SharpDX.Utilities.SizeOf<T>();
        return (size + 15) & ~15;
    }
}

