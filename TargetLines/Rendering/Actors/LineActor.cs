using DrahsidLib;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Diagnostics;
using System.Numerics;
using TargetLines.GameLogic;

namespace TargetLines.Rendering.Actors;

public unsafe class LineActor : IDisposable
{
    // Components
    private LineStateMachine stateMachine;
    private LineRenderer renderer;

    //
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    public Vector3 MiddlePoint;

    public TargetSettingsPair LineSettings { get; set; } = new TargetSettingsPair(new TargetSettings(), new TargetSettings(), new LineColor());
    public RGBA LineColor = new RGBA(0, 0, 0, 0);
    public RGBA OutlineColor = new RGBA(0, 0, 0, 0);
    public RGBA LastLineColor = new RGBA(0, 0, 0, 0);
    public RGBA LastOutlineColor = new RGBA(0, 0, 0, 0);

    private Stopwatch FPPTransition = new Stopwatch();
    private float FPPLastTransition = 0.0f;

    // Public accessors
    public bool IsActive => stateMachine.IsActive();
    public bool Sleeping => !stateMachine.IsActive();
    public IGameObject? Self => stateMachine.Self;
    public bool FocusTarget => stateMachine.FocusTarget;

    public LineActor()
    {
        stateMachine = new LineStateMachine();
        renderer = new LineRenderer();
    }

    public unsafe void InitializeTargetLine(IGameObject gameObject, bool focusTarget = false)
    {
        if (gameObject == null) return;

        stateMachine.Initialize(gameObject, focusTarget);
        Service.Logger.Debug($"Initialized LineActor for {gameObject.Name}");
    }

    public unsafe void Update()
    {
        if (!stateMachine.IsActive()) return;

        stateMachine.Update();
        UpdatePositions();
        UpdateColors();
    }

    private void UpdatePositions()
    {
        if (stateMachine.Self == null) return;

        var target = stateMachine.GetTargetObject();
        if (target == null && stateMachine.State != LineStateMachine.LineState.Dying &&
            stateMachine.State != LineStateMachine.LineState.Dying2) return;

        bool fppSource, fppTarget;
        Vector3 sourcePos = CalculatePosition(
            stateMachine.Self.Position,
            stateMachine.Self.GetHeadHeight(),
            stateMachine.Self.EntityId == Service.ClientState.LocalPlayer?.EntityId,
            out fppSource
        );

        Vector3 targetPos;
        if (target != null)
        {
            targetPos = CalculatePosition(
                target.Position,
                target.GetHeadHeight(),
                target.EntityId == Service.ClientState.LocalPlayer?.EntityId,
                out fppTarget
            );
        }
        else
        {
            targetPos = stateMachine.LastTargetPosition;
            fppTarget = false;
        }

        // Apply height scaling
        float sourceHeightScaled = (fppSource ? 0 : stateMachine.Self.GetCursorHeight()) * Globals.Config.saved.HeightScale;
        float targetHeightScaled = (fppTarget ? 0 : stateMachine.LastTargetHeight) * Globals.Config.saved.HeightScale;

        sourcePos.Y += sourceHeightScaled;
        targetPos.Y += targetHeightScaled;

        // Update positions based on state
        switch (stateMachine.State)
        {
            case LineStateMachine.LineState.NewTarget:
                StartPoint = sourcePos;
                EndPoint = Vector3.Lerp(sourcePos, targetPos, stateMachine.AnimationAlpha);
                break;
            case LineStateMachine.LineState.Dying:
            case LineStateMachine.LineState.Dying2:
                StartPoint = sourcePos;
                EndPoint = Vector3.Lerp(targetPos, sourcePos, stateMachine.AnimationAlpha);
                break;
            case LineStateMachine.LineState.Switching:
                StartPoint = sourcePos;
                EndPoint = Vector3.Lerp(stateMachine.LastTargetPosition, targetPos, stateMachine.AnimationAlpha);
                break;
            case LineStateMachine.LineState.Idle:
                StartPoint = sourcePos;
                EndPoint = targetPos;
                break;
            default:
                StartPoint = sourcePos;
                EndPoint = targetPos;
                break;
        }

        // Calculate middle point with arc
        MiddlePoint = CalculateMidPosition(
            StartPoint,
            EndPoint,
            stateMachine.MidHeight,
            stateMachine.State,
            stateMachine.AnimationAlpha
        );

        // Add player/enemy height bumps
        if (stateMachine.Self.GetIsPlayerCharacter())
        {
            MiddlePoint.Y += Globals.Config.saved.PlayerHeightBump;
        }
        else if (stateMachine.Self.GetIsBattleChara())
        {
            MiddlePoint.Y += Globals.Config.saved.EnemyHeightBump;
        }
    }

    private void UpdateColors()
    {
        UpdateColorsInternal(stateMachine.Self, stateMachine.GetTargetObject(), stateMachine.FocusTarget);
        ApplyStateFading(stateMachine.State, stateMachine.AnimationAlpha);
    }

    public void Dispose()
    {
        renderer?.Dispose();
        renderer = null;
    }


    public void RenderLine()
    {
        if (!stateMachine.IsActive()) return;

        var renderData = new LineRenderData
        {
            StartPoint = StartPoint,
            EndPoint = EndPoint,
            MiddlePoint = MiddlePoint,
            Color = GetLineColorAsVector4(),
            OutlineColor = GetOutlineColorAsVector4(),
            LineThickness = Globals.Config.saved.LineThickness,
            OutlineThickness = Globals.Config.saved.OutlineThickness,
            UseQuadratic = LineSettings.LineColor.UseQuad
        };
        renderer.RenderLine(renderData);
    }

    // Position calculation methods
    private Vector3 CalculatePosition(Vector3 tppPosition, float height, bool isPlayer, out bool fpp)
    {
        Vector3 position = tppPosition;
        fpp = false;
        if (isPlayer)
        {
            fpp = Globals.IsInFirstPerson();
            var cam = Service.CameraManager->Camera;
            float transition = cam->Transition;
            if (fpp || transition != 0 || FPPTransition.IsRunning)
            {
                Vector3 cameraPosition = Globals.WorldCamera_GetPos() + (-2.0f * Globals.WorldCamera_GetForward());
                cameraPosition.Y -= height;
                position = GetTransitionPosition(tppPosition, cameraPosition, transition, fpp);
            }
        }
        return position;
    }

    private Vector3 GetTransitionPosition(Vector3 startPosition, Vector3 endPosition, float transition, bool isFPP)
    {
        if (transition == 0.0f)
        {
            FPPTransition.Reset();
            if (isFPP)
            {
                return endPosition;
            }
        }
        else
        {
            if (!FPPTransition.IsRunning || MathF.Sign(transition) != MathF.Sign(FPPLastTransition))
            {
                FPPTransition.Restart();
            }
            FPPLastTransition = transition;
        }

        float t = (FPPTransition.ElapsedMilliseconds / 1000.0f) / 0.49f;
        if (transition < 0)
        {
            t *= 0.5f;
        }
        else
        {
            t *= 2.0f;
        }

        if (t > 1)
        {
            t = 1;
        }

        return Vector3.Lerp(transition > 0 ? startPosition : endPosition, transition > 0 ? endPosition : startPosition, t);
    }

    private Vector3 CalculateMidPosition(Vector3 position, Vector3 targetPosition, float midHeight, LineStateMachine.LineState state, float stateTime)
    {
        Vector3 midPosition = (position + targetPosition) * 0.5f;

        float heightFix = 0.75f;
        if (LineSettings.LineColor.UseQuad)
        {
            heightFix = 1.0f;
        }

        if (state == LineStateMachine.LineState.Dying)
        {
            float alpha = stateTime / Globals.Config.saved.NoTargetFadeTime;
            heightFix *= 1.0f - alpha;
        }
        else if (state == LineStateMachine.LineState.NewTarget)
        {
            float alpha = stateTime / Globals.Config.saved.NewTargetEaseTime;
            heightFix *= alpha;
        }

        midPosition.Y += (midHeight * Globals.Config.saved.ArcHeightScalar) * heightFix;

        return midPosition;
    }

    // Color management methods
    private void UpdateColorsInternal(IGameObject? self, IGameObject? target, bool focusTarget)
    {
        LastLineColor.raw = LineColor.raw;
        LastOutlineColor.raw = OutlineColor.raw;

        if (self == null || target == null)
        {
            return;
        }

        var selfSettings = self.GetTargetSettings();
        var targSettings = target.GetTargetSettings();

        TargetSettingsPair? selectedSettings = SelectBestSettings(selfSettings, targSettings, focusTarget);

        if (selectedSettings != null && selectedSettings.LineColor.Visible)
        {
            ApplySettings(selectedSettings);
        }
        else if (Globals.Config.saved.LineColor.Visible)
        {
            ApplyFallbackSettings();
        }
        else
        {
            SetInvisible();
            return;
        }

        ApplyAlphaEffects();
    }

    private TargetSettingsPair? SelectBestSettings(TargetSettings selfSettings, TargetSettings targSettings, bool focusTarget)
    {
        TargetSettingsPair? bestSettings = null;
        int highestPriority = -1;

        foreach (var settings in Globals.Config.LineColors)
        {
            if (settings == null)
            {
                continue;
            }

            int currentPriority = settings.GetPairPriority(focusTarget);
            if (currentPriority > highestPriority && currentPriority != -1)
            {
                bool matchesFrom = ClassJobHelper.CompareTargetSettings(ref settings.From, ref selfSettings);
                bool matchesTo = ClassJobHelper.CompareTargetSettings(ref settings.To, ref targSettings);

                if (matchesFrom && matchesTo)
                {
                    highestPriority = currentPriority;
                    bestSettings = settings;
                }
            }
        }

        return bestSettings;
    }

    private void ApplySettings(TargetSettingsPair settings)
    {
        LineSettings = settings;
        LineColor.raw = settings.LineColor.Color.raw;
        OutlineColor.raw = settings.LineColor.OutlineColor.raw;
    }

    private void ApplyFallbackSettings()
    {
        LastLineColor.raw = LineColor.raw;
        LastOutlineColor.raw = OutlineColor.raw;

        LineSettings.LineColor = Globals.Config.saved.LineColor;
        LineColor.raw = LineSettings.LineColor.Color.raw;
        OutlineColor.raw = LineSettings.LineColor.OutlineColor.raw;
    }

    private void SetInvisible()
    {
        LineColor.a = 0;
        OutlineColor.a = 0;
    }

    private void ApplyAlphaEffects()
    {
        float alpha = 1.0f;

        if (Globals.Config.saved.BreathingEffect)
        {
            float breathingOffset = 1.0f - Globals.Config.saved.WaveAmplitudeOffset;
            float breathingAmplitude = (float)Math.Cos(Globals.Renderer.Time * Globals.Config.saved.WaveFrequencyScalar);
            alpha = breathingOffset + breathingAmplitude * Globals.Config.saved.WaveAmplitudeOffset;
        }

        LineColor.a = (byte)(LineColor.a * alpha);
        OutlineColor.a = (byte)(OutlineColor.a * alpha);
    }

    private void ApplyStateFading(LineStateMachine.LineState state, float animationAlpha)
    {
        if (state == LineStateMachine.LineState.Dying || state == LineStateMachine.LineState.Dying2)
        {
            LineColor.a = (byte)(LineColor.a * (1.0f - animationAlpha));
            OutlineColor.a = (byte)(OutlineColor.a * (1.0f - animationAlpha));
        }
    }

    private Vector4 GetLineColorAsVector4()
    {
        return new Vector4(
            LineColor.r / 255.0f,
            LineColor.g / 255.0f,
            LineColor.b / 255.0f,
            LineColor.a / 255.0f
        );
    }

    private Vector4 GetOutlineColorAsVector4()
    {
        return new Vector4(
            OutlineColor.r / 255.0f,
            OutlineColor.g / 255.0f,
            OutlineColor.b / 255.0f,
            OutlineColor.a / 255.0f
        );
    }
}