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
    

    [FieldOffset(0x68)] public Texture* BackBufferNoUI;
    [FieldOffset(0x100)] public Texture* BackBufferNoUICopy1;
    [FieldOffset(0x108)] public Texture* BackBufferNoUICopy2;
    [FieldOffset(0x258)] public Texture* BackBufferNoUICopy3; // needs to be scaled by rez scale
    [FieldOffset(0x288)] public Texture* BackBufferNoUICopy4; // needs to be scaled by rez scale
    [FieldOffset(0x4A8)] public Texture* BackBufferNoUICopy5;
    [FieldOffset(0x4B0)] public Texture* BackBufferNoUICopy6;
    [FieldOffset(0x4C0)] public Texture* BackBufferNoUICopy7;
    [FieldOffset(0x4C8)] public Texture* BackBufferNoUICopy8;
    [FieldOffset(0x4D0)] public Texture* BackBufferNoUICopy9;
    [FieldOffset(0x4D8)] public Texture* BackBufferNoUICopy10;


    [FieldOffset(0x370)] public Texture* BackBuffer;
    [FieldOffset(0x4E0)] public Texture* BackBufferCopy1; // full screen, does not need to be scaled up
    [FieldOffset(0x4E8)] public Texture* BackBufferCopy2;
    [FieldOffset(0x570)] public Texture* BackBufferCopy3;
}

