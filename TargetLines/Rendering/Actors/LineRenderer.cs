using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using TargetLines.Utilities.Extensions;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace TargetLines.Rendering.Actors;

public unsafe class LineRenderer : IDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct LineVertex
    {
        [FieldOffset(0x00)] public Vector3 Position;
        [FieldOffset(0x0C)] public Vector2 UV;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x94)]
    private struct LineConstantBuffer
    {
        [FieldOffset(0x00)] public Matrix ViewProjection;
        [FieldOffset(0x40)] public Vector4 LineColor;
        [FieldOffset(0x50)] public Vector4 OutlineColor;
        [FieldOffset(0x60)] public Vector3 StartPoint;
        [FieldOffset(0x6C)] public float Thickness;
        [FieldOffset(0x70)] public Vector3 EndPoint;
        [FieldOffset(0x7C)] public float OutlineThickness;
        [FieldOffset(0x80)] public Vector3 MiddlePoint;
        [FieldOffset(0x8C)] public float UseQuadratic; // 1.0 for quadratic, 0.0 for cubic
        [FieldOffset(0x90)] public float RezoScale;
    }

    private Buffer? vertexBuffer;
    private Buffer? indexBuffer;
    private InputLayout? layout;
    private VertexBufferBinding vertexBufferBinding;

    private LineConstantBuffer constantBuffer;
    private Buffer? constantBufferBuffer;

    private LineVertex[] boundingQuad = new LineVertex[4];
    private static readonly int[] QuadIndices = new int[] { 0, 1, 2, 0, 2, 3 };

    private bool initialized = false;

    public LineRenderer()
    {
        constantBuffer = new LineConstantBuffer();
        InitializeBuffers();
        InitializeLayout();
        initialized = true;
    }

    private void InitializeLayout()
    {
        var vertexShaderBytecode = ShaderSingleton.GetVertexShaderBytecode(ShaderSingleton.Shader.Line);
        if (vertexShaderBytecode == null)
        {
            throw new InvalidOperationException("Line vertex shader bytecode is null - shader compilation may have failed");
        }

        layout = new InputLayout(
            Globals.Renderer.Device,
            vertexShaderBytecode.Data,
            new InputElement[] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            }
        );
    }

    private void InitializeBuffers()
    {
        UpdateBoundingQuad(Vector3.Zero, Vector3.Zero, Vector3.Zero);

        var vertexBufferDesc = new BufferDescription()
        {
            SizeInBytes = SharpDX.Utilities.SizeOf<LineVertex>() * boundingQuad.Length,
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.VertexBuffer,
            CpuAccessFlags = CpuAccessFlags.Write,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = SharpDX.Utilities.SizeOf<LineVertex>()
        };
        vertexBuffer = Buffer.Create(Globals.Renderer.Device, boundingQuad, vertexBufferDesc);

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

        vertexBufferBinding = new VertexBufferBinding(vertexBuffer, SharpDX.Utilities.SizeOf<LineVertex>(), 0);
        constantBufferBuffer = new Buffer(Globals.Renderer.Device, Globals.AlignSizeTo16Bytes<LineConstantBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
    }

    private void UpdateBoundingQuad(Vector3 startPoint, Vector3 endPoint, Vector3 middlePoint)
    {
        var viewProj = Globals.Renderer.ViewProjectionMatrix;

        var startClip = Vector4.Transform(new Vector4(startPoint, 1.0f), viewProj);
        var endClip = Vector4.Transform(new Vector4(endPoint, 1.0f), viewProj);
        var middleClip = Vector4.Transform(new Vector4(middlePoint, 1.0f), viewProj);

        // Convert to NDC
        if (startClip.W <= 0.0f && endClip.W <= 0.0f && middleClip.W <= 0.0f)
        {
            // Line is completely behind camera
            for (int index = 0; index < 4; index++)
            {
                boundingQuad[index] = new LineVertex { Position = new Vector3(0, 0, 0), UV = new Vector2(0, 0) };
            }
            return;
        }

        var startNDC = startClip.W > 0 ? new Vector2(startClip.X / startClip.W, startClip.Y / startClip.W) : new Vector2(0, 0);
        var endNDC = endClip.W > 0 ? new Vector2(endClip.X / endClip.W, endClip.Y / endClip.W) : new Vector2(0, 0);
        var middleNDC = middleClip.W > 0 ? new Vector2(middleClip.X / middleClip.W, middleClip.Y / middleClip.W) : new Vector2(0, 0);

        // Calculate bounding box with padding for line thickness
        float maxThickness = Math.Max(Globals.Config.saved.LineThickness, Globals.Config.saved.OutlineThickness) * 0.002f;
        float padding = maxThickness * 4.0f;

        float minX = Math.Min(Math.Min(startNDC.X, endNDC.X), middleNDC.X) - padding;
        float maxX = Math.Max(Math.Max(startNDC.X, endNDC.X), middleNDC.X) + padding;
        float minY = Math.Min(Math.Min(startNDC.Y, endNDC.Y), middleNDC.Y) - padding;
        float maxY = Math.Max(Math.Max(startNDC.Y, endNDC.Y), middleNDC.Y) + padding;

        // Clamp to screen bounds
        minX = Math.Max(minX, -1.0f);
        maxX = Math.Min(maxX, 1.0f);
        minY = Math.Max(minY, -1.0f);
        maxY = Math.Min(maxY, 1.0f);

        // UV coordinates for proper texture sampling
        float uvMinX = (minX + 1.0f) * 0.5f;
        float uvMaxX = (maxX + 1.0f) * 0.5f;
        float uvMinY = 1.0f - ((maxY + 1.0f) * 0.5f);
        float uvMaxY = 1.0f - ((minY + 1.0f) * 0.5f);

        // Create quad vertices with proper UV mapping
        boundingQuad[0] = new LineVertex { Position = new Vector3(minX, minY, 0.0f), UV = new Vector2(uvMinX, uvMaxY) };
        boundingQuad[1] = new LineVertex { Position = new Vector3(minX, maxY, 0.0f), UV = new Vector2(uvMinX, uvMinY) };
        boundingQuad[2] = new LineVertex { Position = new Vector3(maxX, maxY, 0.0f), UV = new Vector2(uvMaxX, uvMinY) };
        boundingQuad[3] = new LineVertex { Position = new Vector3(maxX, minY, 0.0f), UV = new Vector2(uvMaxX, uvMaxY) };
    }

    public void RenderLine(LineRenderData renderData)
    {
        if (!initialized || vertexBuffer == null || indexBuffer == null || constantBufferBuffer == null || layout == null) return;
        if (Globals.Renderer?.DeviceContext == null) return;

        UpdateBoundingQuad(renderData.StartPoint, renderData.EndPoint, renderData.MiddlePoint);

        // Update vertex buffer
        var dataBox = Globals.Renderer.DeviceContext.MapSubresource(vertexBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
        SharpDX.Utilities.Write(dataBox.DataPointer, boundingQuad, 0, boundingQuad.Length);
        Globals.Renderer.DeviceContext.UnmapSubresource(vertexBuffer, 0);

        // Set shaders
        var vertexShader = ShaderSingleton.GetVertexShader(ShaderSingleton.Shader.Line);
        var pixelShader = ShaderSingleton.GetPixelShader(ShaderSingleton.Shader.Line);
        if (vertexShader == null || pixelShader == null) return;

        Globals.Renderer.DeviceContext.VertexShader.Set(vertexShader);
        Globals.Renderer.DeviceContext.PixelShader.Set(pixelShader);

        // Set input layout and buffers
        Globals.Renderer.DeviceContext.InputAssembler.InputLayout = layout;
        Globals.Renderer.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        Globals.Renderer.DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        Globals.Renderer.DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

        // Update constant buffer with render data
        constantBuffer.ViewProjection = Globals.Renderer.ViewProjectionMatrix.ToSharpDX();
        constantBuffer.ViewProjection.Transpose();
        constantBuffer.LineColor = renderData.Color;
        constantBuffer.OutlineColor = renderData.OutlineColor;
        constantBuffer.StartPoint = renderData.StartPoint;
        constantBuffer.EndPoint = renderData.EndPoint;
        constantBuffer.MiddlePoint = renderData.MiddlePoint;
        constantBuffer.Thickness = renderData.LineThickness;
        constantBuffer.OutlineThickness = renderData.OutlineThickness;
        constantBuffer.UseQuadratic = renderData.UseQuadratic ? 1.0f : 0.0f;

        unsafe
        {
            constantBuffer.RezoScale = FFXIVClientStructs.FFXIV.Client.Graphics.Render.GraphicsConfig.Instance()->GraphicsRezoScale;
        }

        Globals.Renderer.DeviceContext.UpdateSubresource(ref constantBuffer, constantBufferBuffer);
        Globals.Renderer.DeviceContext.VertexShader.SetConstantBuffer(0, constantBufferBuffer);
        Globals.Renderer.DeviceContext.PixelShader.SetConstantBuffer(0, constantBufferBuffer);

        var depthSRV = RenderTargetSetup.GetDepthShaderResourceView();
        if (depthSRV != null)
        {
            Globals.Renderer.DeviceContext.PixelShader.SetShaderResource(0, depthSRV);
        }

        var backSRV = RenderTargetSetup.GetBackBufferShaderResourceView();
        if (backSRV != null && Globals.Config.saved.UIOcclusion)
        {
            Globals.Renderer.DeviceContext.PixelShader.SetShaderResource(1, backSRV);
        }

        SetupSamplers();
        SetupBlendState();
        SetupDepthState();
        Globals.Renderer.DeviceContext.DrawIndexed(QuadIndices.Length, 0, 0);
    }

    private void SetupSamplers()
    {
        var depthSamplerDesc = new SamplerStateDescription()
        {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLodBias = 0.0f,
            MaximumAnisotropy = 1,
            ComparisonFunction = Comparison.Never,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            MinimumLod = 0.0f,
            MaximumLod = float.MaxValue
        };

        using (var depthSampler = new SamplerState(Globals.Renderer?.Device, depthSamplerDesc))
        {
            Globals.Renderer?.DeviceContext?.PixelShader.SetSampler(0, depthSampler);
        }


        var backBufferSamplerDesc = new SamplerStateDescription()
        {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLodBias = 0.0f,
            MaximumAnisotropy = 1,
            ComparisonFunction = Comparison.Never,
            BorderColor = new Color4(0.0f, 0.0f, 0.0f, 0.0f),
            MinimumLod = 0.0f,
            MaximumLod = float.MaxValue
        };

        using (var backBufferSampler = new SamplerState(Globals.Renderer?.Device, backBufferSamplerDesc))
        {
            Globals.Renderer?.DeviceContext?.PixelShader.SetSampler(1, backBufferSampler);
        }
    }

    private void SetupBlendState()
    {
        var blendStateDesc = new BlendStateDescription();
        blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
        blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
        blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
        blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
        blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

        using (var blendState = new BlendState(Globals.Renderer.Device, blendStateDesc))
        {
            Globals.Renderer?.DeviceContext?.OutputMerger.SetBlendState(blendState, null, 0xFFFFFFFF);
        }
    }

    private void SetupDepthState()
    {
        // Proper ordering is handled by sorting lines back-to-front and otherwise doing self-occlusion in the shader
        var depthStencilStateDesc = new DepthStencilStateDescription()
        {
            IsDepthEnabled = false,
            IsStencilEnabled = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthComparison = Comparison.Always
        }; 

        using (var depthStencilState = new DepthStencilState(Globals.Renderer.Device, depthStencilStateDesc))
        {
            Globals.Renderer?.DeviceContext?.OutputMerger.SetDepthStencilState(depthStencilState);
        }
    }

    public void Dispose()
    {
        layout?.Dispose();
        layout = null;
        vertexBuffer?.Dispose();
        vertexBuffer = null;
        indexBuffer?.Dispose();
        indexBuffer = null;
        constantBufferBuffer?.Dispose();
        constantBufferBuffer = null;
    }
}

public struct LineRenderData
{
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    public Vector3 MiddlePoint;
    public Vector4 Color;
    public Vector4 OutlineColor;
    public float LineThickness;
    public float OutlineThickness;
    public bool UseQuadratic;
}
