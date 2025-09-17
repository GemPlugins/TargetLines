using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using DrahsidLib;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TargetLines.Rendering;

public static class ShaderSingleton {
    public enum Shader {
        Line,
        Triangle,
        DepthDebug,
        UIMaskDebug,
        Count
    }

    public const int SHADER_COUNT = (int)Shader.Count;
    public const int SHADER_TYPE_COUNT = 3; // vertex, pixel, geometry

    private const string ShaderPreambleVertex = "VertexShader";
    private const string ShaderPreamblePixel = "PixelShader";
    private const string ShaderPreambleGeometry = "GeometryShader";

    public static ShaderBytecode[] VertexByteCode = new ShaderBytecode[SHADER_COUNT];
    public static ShaderBytecode[] PixelByteCode = new ShaderBytecode[SHADER_COUNT];
    public static ShaderBytecode[] GeometryByteCode = new ShaderBytecode[SHADER_COUNT];
    public static VertexShader[] VertexShaders = new VertexShader[SHADER_COUNT];
    public static PixelShader[] PixelShaders = new PixelShader[SHADER_COUNT];
    public static GeometryShader[] GeometryShaders = new GeometryShader[SHADER_COUNT];

    public static bool Initialized = false;
    public static bool Initializing = false;
    public static bool Fail = false;

    public static bool ShowingProgress = false;
    public static int ShadersCompiled = 0;
    public static int TotalShaders = 0;
    public static string CurrentShaderName = "";

    public static async Task InitializeAsync(Device device) {
        if (Initialized || Initializing) {
            return;
        }

        Initializing = true;
        ShowingProgress = true;
        CurrentShaderName = "Starting...";
        ShadersCompiled = 0;
        TotalShaders = SHADER_COUNT * SHADER_TYPE_COUNT;

        await Task.Run(() => {
            var shaderPath = Path.Combine(Service.Interface.AssemblyLocation.Directory?.FullName!, "Data/Shaders");
            for (int index = 0; index < (int)Shader.Count; index++) {
                var vertexFile = $"{shaderPath}/{ShaderPreambleVertex}{((Shader)index)}.hlsl";
                var pixelFile = $"{shaderPath}/{ShaderPreamblePixel}{((Shader)index)}.hlsl";
                var geoFile = $"{shaderPath}/{ShaderPreambleGeometry}{((Shader)index)}.hlsl";

                try {
                    if (File.Exists(vertexFile))
                    {
                        CurrentShaderName = $"Vertex {((Shader)index)}";
                        Service.Logger.Verbose($"Compiling {vertexFile}...");
                        VertexByteCode[index] = ShaderBytecode.CompileFromFile(vertexFile, "Main", "vs_5_0");
                        VertexShaders[index] = new VertexShader(device, VertexByteCode[index]);
                        Service.Logger.Verbose("OK!");
                    }
                    else
                    {
                        Service.Logger.Verbose($"No {ShaderPreambleVertex} for {(Shader)index}!");
                    }
                    ShadersCompiled++;
                }
                catch (Exception ex) {
                    Service.Logger.Error($"Failed to compile {ShaderPreambleVertex}? {vertexFile}: {ex.Message}");
                    Fail = true;
                }

                try {
                    if (File.Exists(pixelFile))
                    {
                        CurrentShaderName = $"Pixel {((Shader)index)}";
                        Service.Logger.Verbose($"Compiling {pixelFile}...");
                        PixelByteCode[index] = ShaderBytecode.CompileFromFile(pixelFile, "Main", "ps_5_0");
                        PixelShaders[index] = new PixelShader(device, PixelByteCode[index]);
                        Service.Logger.Verbose("OK!");
                    }
                    else
                    {
                        Service.Logger.Verbose($"No {ShaderPreamblePixel} for {(Shader)index}!");
                    }
                    ShadersCompiled++;
                }
                catch (Exception ex) {
                    Service.Logger.Error($"Failed to compile {ShaderPreamblePixel}? {pixelFile}: {ex.Message}");
                    Fail = true;
                }

                try {
                    if (File.Exists(geoFile))
                    {
                        CurrentShaderName = $"Geometry {((Shader)index)}";
                        Service.Logger.Verbose($"Compiling {geoFile}...");
                        GeometryByteCode[index] = ShaderBytecode.CompileFromFile(geoFile, "Main", "gs_5_0");
                        GeometryShaders[index] = new GeometryShader(device, GeometryByteCode[index]);
                        Service.Logger.Verbose("OK!");
                    }
                    else
                    {
                        Service.Logger.Verbose($"No {ShaderPreambleGeometry} for {(Shader)index}!");
                    }
                    ShadersCompiled++;
                }
                catch (Exception ex) {
                    Service.Logger.Error($"Failed to compile {ShaderPreambleGeometry}? {geoFile}: {ex.Message}");
                    Fail = true;
                }

                Task.Delay(50).Wait();
            }
        });

        Initialized = true;
        Initializing = false;
        ShowingProgress = false;
        CurrentShaderName = "Complete!";
        Service.Logger.Info("Shader compilation complete!");
    }

    public static void Dispose() {
        for (int index = 0; index < (int)Shader.Count; index++) {
            VertexShaders[index]?.Dispose();
            VertexByteCode[index]?.Dispose();
            PixelShaders[index]?.Dispose();
            PixelByteCode[index]?.Dispose();
            GeometryShaders[index]?.Dispose();
            GeometryByteCode[index]?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShaderBytecode GetVertexShaderBytecode(Shader id) => VertexByteCode[(int)id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShaderBytecode GetPixelShaderBytecode(Shader id) => PixelByteCode[(int)id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShaderBytecode GetGeometryShaderBytecode(Shader id) => GeometryByteCode[(int)id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VertexShader GetVertexShader(Shader id) => VertexShaders[(int)id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PixelShader GetPixelShader(Shader id) => PixelShaders[(int)id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GeometryShader GetGeometryShader(Shader id) => GeometryShaders[(int)id];

    public static void DrawShaderCompilationProgress()
    {
        var progress = TotalShaders > 0 ? (float)ShadersCompiled / TotalShaders : 0.0f;

        ImGuiHelpers.ForceNextWindowMainViewport();
        var size = new Vector2(300, 112.5f) * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter() - (size / 2));
        ImGui.SetNextWindowSize(size);

        if (ImGui.Begin("##ShaderCompilationProgress", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
        {
            var windowWidth = ImGui.GetWindowWidth();

            var text1 = "TargetLines: Compiling shaders...";
            var textWidth1 = ImGui.CalcTextSize(text1).X;
            ImGui.SetCursorPosX((windowWidth - textWidth1) * 0.5f);
            ImGui.Text(text1);
            ImGui.Spacing();

            ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{ShadersCompiled}/{TotalShaders}");

            ImGui.Spacing();
            var text2 = $"Current: {CurrentShaderName}";
            var textWidth2 = ImGui.CalcTextSize(text2).X;
            ImGui.SetCursorPosX((windowWidth - textWidth2) * 0.5f);
            ImGui.Text(text2);

            ImGui.End();
        }
    }
}
