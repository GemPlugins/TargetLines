using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using System;
using System.Runtime.InteropServices;

namespace TargetLines.Rendering;

// Exposing some members within the game's RenderTargetManager
[StructLayout(LayoutKind.Explicit)]
public unsafe struct RenderTargetManager
{
    public static RenderTargetManager* Instance() => (RenderTargetManager*)((IntPtr)FFXIVClientStructs.FFXIV.Client.Graphics.Render.RenderTargetManager.Instance());
    [FieldOffset(0x090)] public Texture* DepthStencil; // needs to be scaled by rez scale
    [FieldOffset(0x258)] public Texture* BackBufferNoUI;  // needs to be scaled by rez scale
    [FieldOffset(0x4E0)] public Texture* BackBuffer; // full screen, does not need to be scaled up
}

