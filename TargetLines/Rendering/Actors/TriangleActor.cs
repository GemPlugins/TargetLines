using DrahsidLib;
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

public class TriangleActor : IDisposable
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct TriangleVertex
    {
        [FieldOffset(0x00)] public Vector3 Position;
        [FieldOffset(0x0C)] public Vector4 Color;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    private struct TriangleConstantBuffer
    {
        [FieldOffset(0)] public Matrix ViewProjection;
    }

    private Buffer vertexBuffer;
    private Buffer indexBuffer;
    private InputLayout layout;
    private VertexBufferBinding vertexBufferBinding;

    private TriangleConstantBuffer constantBuffer;
    private Buffer constantBufferBuffer;

    private static readonly TriangleVertex[] TriangleVertices = new TriangleVertex[]
    {
        new TriangleVertex { Position = new Vector3(0.0f, 0.5f, 0.0f), Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f) },   // Top (red)
        new TriangleVertex { Position = new Vector3(-0.5f, -0.5f, 0.0f), Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f) }, // Bottom-left (green)
        new TriangleVertex { Position = new Vector3(0.5f, -0.5f, 0.0f), Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f) }   // Bottom-right (blue)
    };

    private static readonly int[] TriangleIndices = new int[] { 0, 1, 2 };

    public TriangleActor()
    {
        constantBuffer = new TriangleConstantBuffer();

        InitializeBuffers();

        var vertexShaderBytecode = ShaderSingleton.GetVertexShaderBytecode(ShaderSingleton.Shader.Triangle);
        if (vertexShaderBytecode == null)
        {
            throw new InvalidOperationException("Triangle vertex shader bytecode is null - shader compilation may have failed");
        }

        layout = new InputLayout(
            Globals.Renderer.Device,
            vertexShaderBytecode.Data,
            new InputElement[] {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
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
        constantBufferBuffer?.Dispose();
        constantBufferBuffer = null;
    }

    private void InitializeBuffers()
    {
        var vertexBufferDesc = new BufferDescription()
        {
            SizeInBytes = SharpDX.Utilities.SizeOf<TriangleVertex>() * TriangleVertices.Length,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.VertexBuffer,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = SharpDX.Utilities.SizeOf<TriangleVertex>()
        };
        vertexBuffer = Buffer.Create(Globals.Renderer.Device, TriangleVertices, vertexBufferDesc);

        var indexBufferDesc = new BufferDescription()
        {
            SizeInBytes = SharpDX.Utilities.SizeOf<int>() * TriangleIndices.Length,
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.IndexBuffer,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None,
            StructureByteStride = SharpDX.Utilities.SizeOf<int>()
        };
        indexBuffer = Buffer.Create(Globals.Renderer.Device, TriangleIndices, indexBufferDesc);

        vertexBufferBinding = new VertexBufferBinding(vertexBuffer, SharpDX.Utilities.SizeOf<TriangleVertex>(), 0);
        constantBufferBuffer = new Buffer(Globals.Renderer.Device, SharpDX.Utilities.SizeOf<TriangleConstantBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
    }

    private unsafe void Render()
    {
        if (Globals.Renderer?.DeviceContext == null || vertexBuffer == null || indexBuffer == null || constantBufferBuffer == null || layout == null)
        {
            return;
        }

        var vertexShader = ShaderSingleton.GetVertexShader(ShaderSingleton.Shader.Triangle);
        var pixelShader = ShaderSingleton.GetPixelShader(ShaderSingleton.Shader.Triangle);
        if (vertexShader == null || pixelShader == null)
        {
            Service.Logger.Warning("Triangle shaders not available");
            return;
        }

        // Update constant buffer
        constantBuffer.ViewProjection = Globals.Renderer.ViewProjectionMatrix.ToSharpDX();
        constantBuffer.ViewProjection.Transpose();

        Globals.Renderer.DeviceContext.UpdateSubresource(ref constantBuffer, constantBufferBuffer);

        // Set up pipeline
        Globals.Renderer.DeviceContext.InputAssembler.InputLayout = layout;
        Globals.Renderer.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        Globals.Renderer.DeviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
        Globals.Renderer.DeviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

        Globals.Renderer.DeviceContext.VertexShader.Set(vertexShader);
        Globals.Renderer.DeviceContext.PixelShader.Set(pixelShader);

        Globals.Renderer.DeviceContext.VertexShader.SetConstantBuffer(0, constantBufferBuffer);

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
        }

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

            Globals.Renderer.DeviceContext.DrawIndexed(TriangleIndices.Length, 0, 0);
        }
    }

    public void OnFrame(double _time)
    {
        Render();
    }
}