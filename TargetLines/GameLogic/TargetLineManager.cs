using DrahsidLib;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using TargetLines.Rendering.Actors;
using TargetLines.Rendering;
using TargetLines.Utilities.Extensions;

namespace TargetLines.GameLogic;

public static class TargetLineManager
{
    public static LineActor[] LineActorArray { get; set; } = null!;
    public static LineActor? FocusLineActor { get; set; } = null;

    public static int RenderedLineCount { get; set; } = 0;
    public static int ProcessedLineCount { get; set; } = 0;

    private static TriangleActor? TestTriangle = null;
    private static LineActor? TestLine = null;
    private static RenderTargetSetup? DebugActor = null;

    public static void InitializeTargetLines()
    {
        Vector3Extensions.Tests();
        //ResetDebugActors();
    }

    public static void InitializeLineActors()
    {
        if (!ShaderSingleton.Initialized || ShaderSingleton.Fail)
        {
            Service.Logger.Warning("Shaders not ready");
            return;
        }

        LineActorArray = new LineActor[Service.ObjectTable.Length];
        for (int index = 0; index < LineActorArray.Length; index++)
        {
            LineActorArray[index] = new LineActor();
        }

        FocusLineActor = new LineActor();

        if (Globals.Renderer != null)
        {
            Globals.Renderer.OnFrameEvent += OnFrameRender;
        }
        else
        {
            Service.Logger.Verbose("Renderer null");
        }
    }

    public static void DisposeLineActors()
    {
        if (Globals.Renderer != null)
        {
            Globals.Renderer.OnFrameEvent -= OnFrameRender;
        }

        if (LineActorArray != null)
        {
            foreach (var actor in LineActorArray)
            {
                actor?.Dispose();
            }
            LineActorArray = null!;
        }

        FocusLineActor?.Dispose();
        FocusLineActor = null;
    }

    private static void OnFrameRender(double time)
    {
        if (LineActorArray == null || FocusLineActor == null) return;
        if (Service.ClientState.LocalPlayer == null || Service.ClientState.IsPvP) return;
        if (!ShaderSingleton.Initialized || ShaderSingleton.Initializing) return;

        ProcessLineActors();
        var activeLines = new List<LineActor>();

        if (FocusLineActor.IsActive)
        {
            activeLines.Add(FocusLineActor);
        }

        for (int index = 0; index < LineActorArray.Length; index++)
        {
            var lineActor = LineActorArray[index];
            if (lineActor.IsActive)
            {
                activeLines.Add(lineActor);
            }
        }

        if (activeLines.Count > 0)
        {
            RenderLinesBatch(activeLines);
        }
    }

    private static unsafe void ProcessLineActors()
    {
        if (LineActorArray == null || FocusLineActor == null) return;

        int renderedLineCount = 0;
        int processedLineCount = 0;

        var activeLines = new List<LineActor>();
        var target = TargetSystem.Instance();
        if (target != null)
        {
            if (!FocusLineActor.IsActive && !Service.ClientState.LocalPlayer.IsDead)
            {
                if (target->FocusTarget != null && target->FocusTarget->EntityId != Service.ClientState.LocalPlayer.EntityId)
                {
                    FocusLineActor.InitializeTargetLine(Service.ClientState.LocalPlayer, true);
                }
            }

            if (FocusLineActor.IsActive)
            {
                FocusLineActor.Update();
                if (FocusLineActor.IsActive)
                {
                    activeLines.Add(FocusLineActor);
                    renderedLineCount++;
                }
                processedLineCount++;
            }
        }

        int maxIndex = Math.Min(448, LineActorArray.Length); // Stop at 448 since it is where client objects ends
        for (int index = 0; index < maxIndex; index++)
        {
            ProcessSingleActor(index, ref processedLineCount, ref renderedLineCount, activeLines);
        }

        RenderedLineCount = activeLines.Count;
        ProcessedLineCount = processedLineCount;
    }

    private static void ProcessSingleActor(int index, ref int processedLineCount, ref int renderedLineCount, List<LineActor> activeLines)
    {
        var lineActor = LineActorArray[index];

        // Handle active actors first (most common case after init)
        if (lineActor.IsActive)
        {
            lineActor.Update();
            processedLineCount++;

            if (lineActor.IsActive)
            {
                activeLines.Add(lineActor);
                renderedLineCount++;
            }
            return;
        }

        // Check if we need to initialize this actor
        var gameObject = Service.ObjectTable[index];

        // Early exit conditions
        if (gameObject == null || !gameObject.IsValid() || gameObject.IsDead)
            return;

        var targetObject = gameObject.TargetObject;
        if (targetObject == null || !targetObject.IsValid())
            return;

#if !PROBABLY_BAD
        if (!gameObject.IsTargetable || !targetObject.IsTargetable)
            return;
#endif

        lineActor.InitializeTargetLine(gameObject, false);
    }

    private static void RenderLinesBatch(List<LineActor> activeLines)
    {
        if (activeLines.Count == 0) return;
        RenderTargetSetup.SetupBackBuffer();
        RenderTargetSetup.SetupDepthTexture();

        // Sort lines back-to-front for better blending
        var cameraPos = Globals.WorldCamera_GetPos();
        var sortedLines = activeLines.OrderByDescending(line =>
        {
            var midPoint = (line.StartPoint + line.EndPoint) * 0.5f; // Use the midpoint of the line for depth sorting
            return Vector3.DistanceSquared(midPoint, cameraPos);
        }).ToList();

        foreach (var line in sortedLines)
        {
            line.RenderLine();
        }
    }

    public static void ResetDebugActors()
    {
        DebugActor?.Dispose();
        DebugActor = null;

        TestTriangle?.Dispose();
        TestTriangle = null;

        TestLine?.Dispose();
        TestLine = null;

        DisposeLineActors();

        Service.Logger.Info("Debug actors reset due to configuration change");
    }

    public static void DrawOverlay() {
        Update();
    }

    public static void Update() {
        if (Service.ClientState.LocalPlayer == null || Service.ClientState.IsPvP || !ShaderSingleton.Initialized || ShaderSingleton.Initializing) {
            return;
        }

        // Initialize LineActors if needed and not in debug mode
        if (LineActorArray == null && !Globals.Config.saved.DebugDepthTexture && !Globals.Config.saved.DebugUIMaskTexture)
        {
            InitializeLineActors();
        }

        // Always create debug actor (needed for both depth texture access and debug visualization)
        if (DebugActor == null && !ShaderSingleton.Fail)
        {
            try
            {
                DebugActor = new RenderTargetSetup();
                Service.Logger.Info("RenderTargetSetup created");
            }
            catch (Exception ex)
            {
                Service.Logger.Error($"Failed to create RenderTargetSetup: {ex}");
            }
        }

        // Create other actors if not in debug mode
        /*if (!Globals.Config.saved.DebugDepthTexture && !Globals.Config.saved.DebugUIMaskTexture)
        {
            if (TestTriangle == null && !ShaderSingleton.Fail) {
                try
                {
                    TestTriangle = new TriangleActor();
                }
                catch (Exception ex)
                {
                    Service.Logger.Error(ex.ToString());
                }
            }

            // Test LineActor
            if (TestLine == null && !ShaderSingleton.Fail) {
                try
                {
                    TestLine = new LineActor();
                    Service.Logger.Info("LineActor test instance created");
                }
                catch (Exception ex)
                {
                    Service.Logger.Error($"Failed to create LineActor: {ex}");
                }
            }
        }*/

        if (TestLine != null)
        {
            TestLine.StartPoint = Service.ClientState.LocalPlayer.GetHeadPosition();
            if (Service.ClientState.LocalPlayer.TargetObject != null)
            {
                TestLine.EndPoint = Service.ClientState.LocalPlayer.TargetObject.GetHeadPosition();
            }
            else
            {
                TestLine.EndPoint = new Vector3(1.0f, 0.1f, 5.0f);
            }
        }
    }
}
