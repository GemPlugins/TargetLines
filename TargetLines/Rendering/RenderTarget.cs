using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;


namespace TargetLines.Rendering;

// The target we render to
public class RenderTarget : IDisposable
{
    public Vector2 Size { get; private set; }
    private Texture2D renderTarget;
    private Texture2D depthTexture;
    public RenderTargetView renderTargetView;
    private ShaderResourceView shaderResourceView;
    private DepthStencilView depthStencilView;
    private DepthStencilState depthStencilState;

    public ImTextureID ImguiHandle => new ImTextureID(shaderResourceView.NativePointer);

    public RenderTarget(int width, int height)
    {
        Size = new(width, height);
        renderTarget = new Texture2D(Globals.Renderer.Device, new()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        renderTargetView = new RenderTargetView(Globals.Renderer.Device, renderTarget, new RenderTargetViewDescription()
        {
            Format = Format.R8G8B8A8_UNorm,
            Dimension = RenderTargetViewDimension.Texture2D,
            Texture2D = new() { }
        });

        shaderResourceView = new ShaderResourceView(Globals.Renderer.Device, renderTarget, new ShaderResourceViewDescription()
        {
            Format = Format.R8G8B8A8_UNorm,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new()
            {
                MostDetailedMip = 0,
                MipLevels = 1
            }
        });

        depthTexture = new Texture2D(Globals.Renderer.Device, new Texture2DDescription()
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D32_Float,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        depthStencilView = new DepthStencilView(Globals.Renderer.Device, depthTexture, new DepthStencilViewDescription()
        {
            Format = Format.D32_Float,
            Dimension = DepthStencilViewDimension.Texture2D,
            Texture2D = new() { }
        });

        var dssDesc = DepthStencilStateDescription.Default();
        dssDesc.DepthComparison = Comparison.GreaterEqual;
        depthStencilState = new(Globals.Renderer.Device, dssDesc);
    }

    public void Dispose()
    {
        renderTarget.Dispose();
        renderTargetView.Dispose();
        shaderResourceView.Dispose();
        depthTexture.Dispose();
        depthStencilView.Dispose();
        depthStencilState.Dispose();
    }

    public void Bind()
    {
        if (Globals.Renderer == null || Globals.Renderer.DeviceContext == null) return;
        Globals.Renderer.DeviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Color4(0.0f, 0.0f, 0.0f, 0.0f));
        Globals.Renderer.DeviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 0, 0);
        Globals.Renderer.DeviceContext.Rasterizer.SetViewport(0, 0, Size.X, Size.Y);
        Globals.Renderer.DeviceContext.OutputMerger.SetDepthStencilState(depthStencilState);
        Globals.Renderer.DeviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
    }
}

