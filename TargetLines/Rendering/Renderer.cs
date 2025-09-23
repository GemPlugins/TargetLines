using System;
using System.Diagnostics;
using System.Numerics;
using DrahsidLib;
using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Dalamud.Interface.Utility;
using SharpDX;
using SharpDX.Direct3D11;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace TargetLines.Rendering;

public class Renderer : IDisposable {
    public delegate void OnFrameDelegate(double time);
    public event OnFrameDelegate OnFrameEvent;

    public double Time;
    private readonly Stopwatch stopwatch;

    public Device? Device { get; private set; }
    public DeviceContext? DeviceContext { get; private set; }
    public RenderTarget? RenderTarget { get; private set; } = null!;
    public RenderTargetView? RenderTargetView => RenderTarget?.renderTargetView;

    public ViewportF Viewport { get; set; }
    public Matrix4x4 ViewMatrix = Matrix4x4.Identity;
    public Matrix4x4 ProjectionMatrix = Matrix4x4.Identity;
    public Matrix4x4 ViewProjectionMatrix = Matrix4x4.Identity;
    public Vector3 CameraPosition = Vector3.Zero;
    public Vector2 ViewportSize = Vector2.Zero;
    public float NearPlane = 0;
    public float FarPlane = 0;

    private delegate IntPtr EnvironmentManagerUpdateDelegate(IntPtr thisx, nint unk1);
    private static Hook<EnvironmentManagerUpdateDelegate>? EnvironmentManagerUpdateHook { get; set; } = null!;
    private static IntPtr EnvironmentManagerUpdateAddress = IntPtr.Zero;

    public unsafe Renderer() {
        Time = 0;
        stopwatch = new Stopwatch();
        stopwatch.Start();
        OnFrameEvent += DefaultOnFrameEvent;

        Device = new Device((IntPtr)FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance()->D3D11Forwarder);
        DeviceContext = new DeviceContext(Device);
        _ = ShaderSingleton.InitializeAsync(Device);

        if (EnvironmentManagerUpdateAddress == IntPtr.Zero)
        {
            IntPtr EnvironmentManagerUpdateAddress = Service.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 55 41 56 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05");
            if (EnvironmentManagerUpdateAddress != IntPtr.Zero)
            {
                EnvironmentManagerUpdateHook = Service.GameInteropProvider.HookFromAddress<EnvironmentManagerUpdateDelegate>(EnvironmentManagerUpdateAddress, EnvironmentManagerUpdateDetour);
                EnvironmentManagerUpdateHook?.Enable();
            }
        }
    }

    public void Dispose() {
        OnFrameEvent -= DefaultOnFrameEvent;
        stopwatch.Stop();
        RenderTarget?.Dispose();
        DeviceContext?.Dispose();
        EnvironmentManagerUpdateHook?.Disable();
        EnvironmentManagerUpdateHook?.Dispose();
        EnvironmentManagerUpdateAddress = IntPtr.Zero;
    }

    private void Execute() {
        if (Device == null || DeviceContext == null) return;
        using var cmds = DeviceContext.FinishCommandList(true);
        Device.ImmediateContext.ExecuteCommandList(cmds, true);
        DeviceContext.ClearState();
    }

    public void OnStartFrame() {
        Time = stopwatch.Elapsed.TotalSeconds;
        if (DeviceContext == null) return;

        if (RenderTarget == null || RenderTarget.Size != ViewportSize)
        {
            if (!(ViewportSize.X == 0 || ViewportSize.Y == 0))
            {
                RenderTarget?.Dispose();
                RenderTarget = new RenderTarget((int)ViewportSize.X, (int)ViewportSize.Y);
            }
        }

        RenderTarget?.Bind();
        OnFrameEvent?.Invoke(Time);
    }

    public void OnEndFrame() {
        if (DeviceContext == null) return;
        Execute();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.Begin("##TargetLinesDXOverlay", Plugin.OVERLAY_WINDOW_FLAGS);
        ImGui.SetWindowSize(ViewportSize);
        if (RenderTarget != null)
        {
            ImGui.GetWindowDrawList().AddImage(RenderTarget.ImguiHandle, Vector2.Zero, ViewportSize);
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DefaultOnFrameEvent(double time)
    {
    }

    private static unsafe void UpdateCameraMatrices()
    {
        var controlCamera = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if (renderCamera == null) return;

        Globals.Renderer.CameraPosition = renderCamera->Origin;

        Globals.Renderer.ViewMatrix = renderCamera->ViewMatrix;
        Globals.Renderer.ViewMatrix.M44 = 1; // for whatever reason, game doesn't initialize it...
        Globals.Renderer.ProjectionMatrix = renderCamera->ProjectionMatrix;
        Globals.Renderer.ViewProjectionMatrix = Globals.Renderer.ViewMatrix * Globals.Renderer.ProjectionMatrix;
        Globals.Renderer.NearPlane = renderCamera->NearPlane;
        Globals.Renderer.FarPlane = renderCamera->FarPlane;

        var device = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();
        if (device != null)
        {
            Globals.Renderer.ViewportSize = new Vector2(device->Width, device->Height);
        }
        else
        {
            Service.Logger.Warning("UpdateCameraMatrices: device is null!");
        }
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    private static IntPtr EnvironmentManagerUpdateDetour(IntPtr thisx, nint unk1)
    {
        UpdateCameraMatrices();
        return EnvironmentManagerUpdateHook.OriginalDisposeSafe(thisx, unk1);
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}
