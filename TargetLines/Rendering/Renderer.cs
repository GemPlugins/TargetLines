using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using DrahsidLib;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace TargetLines.Rendering;

public class Renderer : IDisposable {
    public delegate void OnFrameDelegate(double time);
    public event OnFrameDelegate OnFrameEvent;

    public double Time;
    private Stopwatch stopwatch;

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

    public unsafe Renderer() {
        Time = 0;
        stopwatch = new Stopwatch();
        stopwatch.Start();
        OnFrameEvent += DefaultOnFrameEvent;

        Device = new Device((IntPtr)FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance()->D3D11Forwarder);
        DeviceContext = new DeviceContext(Device);
        _ = ShaderSingleton.InitializeAsync(Device);
    }

    public void Dispose() {
        OnFrameEvent -= DefaultOnFrameEvent;
        stopwatch.Stop();
        RenderTarget?.Dispose();
        DeviceContext?.Dispose();
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

        UpdateCameraMatrices();

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

    private unsafe void UpdateCameraMatrices()
    {
        var controlCamera = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if (renderCamera == null) return;

        CameraPosition = renderCamera->Origin;

        ViewMatrix = renderCamera->ViewMatrix;
        ViewMatrix.M44 = 1; // for whatever reason, game doesn't initialize it...
        ProjectionMatrix = renderCamera->ProjectionMatrix;
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
        NearPlane = renderCamera->NearPlane;
        FarPlane = renderCamera->FarPlane;

        var device = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance();
        if (device != null)
        {
            ViewportSize = new Vector2(device->Width, device->Height);
        }
        else
        {
            Service.Logger.Warning("UpdateCameraMatrices: device is null!");
        }
    }
}
