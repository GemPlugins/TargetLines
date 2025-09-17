using DrahsidLib;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;

using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace TargetLines.Rendering;

public class RenderTargetSetup : IDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct DebugVertex
    {
        [FieldOffset(0x00)] public Vector3 Position;
        [FieldOffset(0x0C)] public Vector2 UV;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x0C)]
    private struct ConstantBuffer_DepthDebug
    {
        [FieldOffset(0x00)] public float RezoScale;
        [FieldOffset(0x04)] public float Near;
        [FieldOffset(0x08)] public float Far;
    }

    private Buffer? vertexBuffer;
    private Buffer? indexBuffer;
    private InputLayout? layout;
    private VertexBufferBinding vertexBufferBinding;

    private ConstantBuffer_DepthDebug depthDebugConstantBuffer;
    private Buffer? depthDebugConstantBufferBuffer;

    private static float previousRezoScale = 0;
    private static int previousWidth = 0;
    private static int previousHeight = 0;

    private static ShaderResourceView? depthShaderResourceView;
    private static Vector2 depthTextureSize = Vector2.Zero;

    private static ShaderResourceView? backBufferShaderResourceView;
    private static Vector2 backBufferSize = Vector2.Zero;

    private static readonly DebugVertex[] FullScreenQuad = new DebugVertex[]
    {
        new DebugVertex { Position = new Vector3(-1.0f, -1.0f, 0.0f), UV = new Vector2(0.0f, 1.0f) },
        new DebugVertex { Position = new Vector3(-1.0f,  1.0f, 0.0f), UV = new Vector2(0.0f, 0.0f) },
        new DebugVertex { Position = new Vector3( 1.0f,  1.0f, 0.0f), UV = new Vector2(1.0f, 0.0f) },
        new DebugVertex { Position = new Vector3( 1.0f, -1.0f, 0.0f), UV = new Vector2(1.0f, 1.0f) }
    };

    private static readonly int[] QuadIndices = new int[] { 0, 1, 2, 0, 2, 3 };

    public RenderTargetSetup()
    {
        depthDebugConstantBuffer = new ConstantBuffer_DepthDebug();

        InitializeBuffers();

        var vertexShaderBytecode = ShaderSingleton.GetVertexShaderBytecode(ShaderSingleton.Shader.DepthDebug );
        if (vertexShaderBytecode == null)
        {
            throw new InvalidOperationException("Debug vertex shader bytecode is null");
        }

        layout = new InputLayout(
            Globals.Renderer.Device,
            vertexShaderBytecode.Data,
            new InputElement[] {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            }
        );


        if (Globals.Renderer != null)
        {
            Globals.Renderer.OnFrameEvent += OnFrame;
        }
    }

    public void Dispose()
    {
        Globals.Renderer?.OnFrameEvent -= OnFrame;
        layout?.Dispose();
        layout = null;
        vertexBuffer?.Dispose();
        vertexBuffer = null;
        indexBuffer?.Dispose();
        indexBuffer = null;
        depthDebugConstantBufferBuffer?.Dispose();
        depthDebugConstantBufferBuffer = null;
        depthShaderResourceView?.Dispose();
        depthShaderResourceView = null;
    }

    private void InitializeBuffers()
    {
        var vertexBufferDesc = new BufferDescription()
        {
            SizeInBytes = SharpDX.Utilities.SizeOf<DebugVertex>() * FullScreenQuad.Length,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.VertexBuffer,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = SharpDX.Utilities.SizeOf<DebugVertex>()
        };
        vertexBuffer = Buffer.Create(Globals.Renderer.Device, FullScreenQuad, vertexBufferDesc);

        var indexBufferDesc = new BufferDescription()
        {
            SizeInBytes = SharpDX.Utilities.SizeOf<int>() * QuadIndices.Length,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.IndexBuffer,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = SharpDX.Utilities.SizeOf<int>()
        };
        indexBuffer = Buffer.Create(Globals.Renderer.Device, QuadIndices, indexBufferDesc);

        vertexBufferBinding = new VertexBufferBinding(vertexBuffer, SharpDX.Utilities.SizeOf<DebugVertex>(), 0);
        var cbSize = Globals.AlignSizeTo16Bytes<ConstantBuffer_DepthDebug>();
        depthDebugConstantBufferBuffer = new Buffer(Globals.Renderer.Device, cbSize, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
    }


    public static unsafe void SetupDepthTexture()
    {
        //var SwapChain = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device.Instance()->SwapChain;
        var RenderTargetManager = TargetLines.Rendering.RenderTargetManager.Instance();
        if (RenderTargetManager != null && RenderTargetManager->DepthStencil != null)
        {
            if (RenderTargetManager->DepthStencil->D3D11ShaderResourceView != null)
            {
                var nativeTexture = new Texture2D((IntPtr)RenderTargetManager->DepthStencil->D3D11Texture2D);
                var textureDesc = nativeTexture.Description;
                var currentSize = new Vector2(textureDesc.Width, textureDesc.Height);
                bool resolutionChanged = false;


                if (currentSize != depthTextureSize)
                {
                    resolutionChanged = true;
                }

                if (GraphicsConfig.Instance()->GraphicsRezoScale != previousRezoScale)
                {
                    resolutionChanged = true;
                }

                if (Globals.Renderer.ViewportSize.X != previousWidth || Globals.Renderer.ViewportSize.Y != previousHeight)
                {
                    resolutionChanged = true;
                }

                if (resolutionChanged || depthShaderResourceView == null)
                {
                    depthShaderResourceView?.Dispose();
                    depthShaderResourceView = new ShaderResourceView((IntPtr)RenderTargetManager->DepthStencil->D3D11ShaderResourceView);
                    depthTextureSize = currentSize;
                }

                nativeTexture.Dispose();
            }
        }
    }

    public static unsafe void SetupBackBuffer()
    {
        var renderTargetManager = RenderTargetManager.Instance();
        if (renderTargetManager == null || renderTargetManager->BackBuffer == null)
        {
            Service.Logger.Debug("BackBuffer null");
            return;
        }

        var backBuffer = renderTargetManager->BackBuffer;
        if (backBuffer->D3D11ShaderResourceView != null)
        {
            var nativeTexture = new Texture2D((IntPtr)backBuffer->D3D11Texture2D);
            var textureDesc = nativeTexture.Description;
            var currentSize = new Vector2(textureDesc.Width, textureDesc.Height);
            bool resolutionChanged = false;

            if (currentSize != depthTextureSize)
            {
                resolutionChanged = true;
            }

            if (GraphicsConfig.Instance()->GraphicsRezoScale != previousRezoScale)
            {
                resolutionChanged = true;
            }

            if (Globals.Renderer.ViewportSize.X != previousWidth || Globals.Renderer.ViewportSize.Y != previousHeight)
            {
                resolutionChanged = true;
            }

            if (resolutionChanged || backBufferShaderResourceView == null)
            {
                backBufferShaderResourceView?.Dispose();
                backBufferShaderResourceView = new ShaderResourceView((IntPtr)backBuffer->D3D11ShaderResourceView);
                backBufferSize = currentSize;
            }

            nativeTexture.Dispose();
        }
    }

    public static ShaderResourceView? GetDepthShaderResourceView()
    {
        return depthShaderResourceView;
    }

    public static Vector2 GetDepthTextureSize()
    {
        return depthTextureSize;
    }

    public static ShaderResourceView? GetBackBufferShaderResourceView()
    {
        return backBufferShaderResourceView;
    }

    public static Vector2 GetBackBufferSize()
    {
        return backBufferSize;
    }

    private unsafe void RenderDepthTexture()
    {
        if (Globals.Renderer?.DeviceContext == null || vertexBuffer == null || indexBuffer == null || depthDebugConstantBufferBuffer == null || layout == null)
        {
            return;
        }

        var vertexShader = ShaderSingleton.GetVertexShader(ShaderSingleton.Shader.DepthDebug);
        var pixelShader = ShaderSingleton.GetPixelShader(ShaderSingleton.Shader.DepthDebug);
        if (vertexShader == null || pixelShader == null)
        {
            Service.Logger.Warning("Debug shaders null");
            return;
        }

        ShaderResourceView? textureToDebug = null;
        Vector2 textureSize;

        SetupDepthTexture();
        textureToDebug = depthShaderResourceView;
        textureSize = depthTextureSize;

        unsafe
        {
            depthDebugConstantBuffer.RezoScale = GraphicsConfig.Instance()->GraphicsRezoScale;
            depthDebugConstantBuffer.Near = Globals.Renderer.NearPlane;
            depthDebugConstantBuffer.Far = Globals.Renderer.FarPlane;
        }

        Globals.Renderer.DeviceContext.UpdateSubresource(ref depthDebugConstantBuffer, depthDebugConstantBufferBuffer);

        // Set up pipeline
        Globals.Renderer.DeviceContext.InputAssembler.InputLayout = layout;
        Globals.Renderer.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        Globals.Renderer.DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        Globals.Renderer.DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

        Globals.Renderer.DeviceContext.VertexShader.Set(vertexShader);
        Globals.Renderer.DeviceContext.PixelShader.Set(pixelShader);

        Globals.Renderer.DeviceContext.VertexShader.SetConstantBuffer(0, depthDebugConstantBufferBuffer);
        Globals.Renderer.DeviceContext.PixelShader.SetConstantBuffer(0, depthDebugConstantBufferBuffer);

        // Bind depth texture to pixel shader
        if (textureToDebug != null)
        {
            Globals.Renderer.DeviceContext.PixelShader.SetShaderResource(0, textureToDebug);

            // Create and bind sampler for depth texture
            var samplerDesc = new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MipLodBias = 0.0f,
                MaximumAnisotropy = 16,
                ComparisonFunction = Comparison.Always,
                BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
                MinimumLod = 0.0f,
                MaximumLod = float.MaxValue
            };

            using (var samplerState = new SamplerState(Globals.Renderer.Device, samplerDesc))
            {
                Globals.Renderer.DeviceContext.PixelShader.SetSampler(0, samplerState);

                var rasterizerStateDesc = new RasterizerStateDescription()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    IsDepthClipEnabled = true,
                    IsFrontCounterClockwise = false,
                    IsMultisampleEnabled = false,
                    IsAntialiasedLineEnabled = false,
                    IsScissorEnabled = false,
                    DepthBias = 0,
                    DepthBiasClamp = 0.0f,
                    SlopeScaledDepthBias = 0.0f
                };

                using (var rasterizerState = new RasterizerState(Globals.Renderer.Device, rasterizerStateDesc))
                {
                    Globals.Renderer.DeviceContext.Rasterizer.State = rasterizerState;

                    var blendStateDesc = new BlendStateDescription();
                    blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
                    blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                    blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                    blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                    blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                    blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                    blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                    blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

                    using (var blendState = new BlendState(Globals.Renderer.Device, blendStateDesc))
                    {
                        Globals.Renderer.DeviceContext.OutputMerger.SetBlendState(blendState);
                        Globals.Renderer.DeviceContext.DrawIndexed(QuadIndices.Length, 0, 0);
                    }
                }
            }
        }
    }

    private unsafe void RenderUIMaskDebug()
    {
        if (Globals.Renderer?.DeviceContext == null || vertexBuffer == null || indexBuffer == null || layout == null)
        {
            return;
        }

        var vertexShader = ShaderSingleton.GetVertexShader(ShaderSingleton.Shader.UIMaskDebug);
        var pixelShader = ShaderSingleton.GetPixelShader(ShaderSingleton.Shader.UIMaskDebug);
        if (vertexShader == null || pixelShader == null)
        {
            Service.Logger.Warning("UIMaskDebug shaders null");
            return;
        }

        SetupBackBuffer();

        if (backBufferShaderResourceView == null)
        {
            Service.Logger.Debug("backBufferShaderResourceView null");
            return;
        }

        // Set up pipeline
        Globals.Renderer.DeviceContext.InputAssembler.InputLayout = layout;
        Globals.Renderer.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        Globals.Renderer.DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        Globals.Renderer.DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

        Globals.Renderer.DeviceContext.VertexShader.Set(vertexShader);
        Globals.Renderer.DeviceContext.PixelShader.Set(pixelShader);

        // Bind BackBuffer texture to pixel shader
        Globals.Renderer.DeviceContext.PixelShader.SetShaderResource(0, backBufferShaderResourceView);

        var samplerDesc = new SamplerStateDescription()
        {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLodBias = 0.0f,
            MaximumAnisotropy = 1,
            ComparisonFunction = Comparison.Always,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            MinimumLod = 0.0f,
            MaximumLod = float.MaxValue
        };

        using (var samplerState = new SamplerState(Globals.Renderer.Device, samplerDesc))
        {
            Globals.Renderer.DeviceContext.PixelShader.SetSampler(0, samplerState);

            var rasterizerStateDesc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = false,
                IsScissorEnabled = false,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                SlopeScaledDepthBias = 0.0f
            };

            using (var rasterizerState = new RasterizerState(Globals.Renderer.Device, rasterizerStateDesc))
            {
                Globals.Renderer.DeviceContext.Rasterizer.State = rasterizerState;

                var blendStateDesc = new BlendStateDescription();
                blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
                blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

                using (var blendState = new BlendState(Globals.Renderer.Device, blendStateDesc))
                {
                    Globals.Renderer.DeviceContext.OutputMerger.SetBlendState(blendState);
                    Globals.Renderer.DeviceContext.DrawIndexed(QuadIndices.Length, 0, 0);
                }
            }
        }

        backBufferShaderResourceView?.Dispose();
    }

    public void OnFrame(double _time)
    {
        if (Globals.Config.saved.DebugDepthTexture)
        {
            RenderDepthTexture();
        }

        if (Globals.Config.saved.DebugUIMaskTexture)
        {
            RenderUIMaskDebug();
        }

        unsafe
        {
            previousRezoScale = GraphicsConfig.Instance()->GraphicsRezoScale;
            previousWidth = (int)Globals.Renderer.ViewportSize.X;
            previousHeight = (int)Globals.Renderer.ViewportSize.Y;
        }
    }
}