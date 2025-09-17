using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using TargetLines.GameLogic;
using DrahsidLib;
using TargetLines.Utilities;

namespace TargetLines.Rendering.Actors;

public unsafe class LineStateMachine
{
    public enum LineState
    {
        NewTarget,   // new target (from no target)
        Dying,       // no target, fading away
        Dying2,      // render flags 0x800
        Switching,   // switching to different target
        Idle,        // being targeted
        Dead         // prevent looping focus target line death animation
    }

    public LineState State { get; private set; } = LineState.NewTarget;
    public bool Sleeping { get; private set; } = true;
    public IGameObject? Self { get; set; }
    public bool FocusTarget { get; set; }

    private float StateTime = 0.0f;
    private bool HasTarget = false;
    private bool HadTarget = false;
    private ulong LastTargetId = 0;

    public Vector3 LastTargetPosition { get; private set; } = new Vector3();
    public Vector3 LastTargetPosition2 { get; private set; } = new Vector3();
    public float LastTargetHeight { get; private set; } = 0.0f;
    public float MidHeight { get; private set; } = 0.0f;
    public float LastMidHeight { get; private set; } = 0.0f;

    // Animation parameters
    public float AnimationAlpha { get; private set; } = 0.0f;

    // Death animation tracking
    private bool DeathAnimationStarted = false;

    public void Initialize(IGameObject gameObject, bool focusTarget = false)
    {
        Self = gameObject;
        FocusTarget = focusTarget;
        HasTarget = false;
        HadTarget = false;
        LastTargetId = 0;
        State = LineState.NewTarget;
        StateTime = 0.0f;
        Sleeping = false;
        DeathAnimationStarted = false;

        var target = GetTargetObject();
        if (target != null && target.IsValid())
        {
            LastTargetId = GetTargetId();
            LastTargetPosition = target.Position;
            LastTargetPosition2 = LastTargetPosition;
            LastTargetHeight = target.GetHeadHeight();
        }
        else if (Self != null)
        {
            LastTargetPosition = Self.Position;
            LastTargetPosition2 = LastTargetPosition;
        }
    }

    public void Update()
    {
        if (Sleeping || Self == null) return;

        var target = GetTargetObject();
        HasTarget = target != null && target.IsValid();

        // Check for render flags death state
        var csObj = Self.GetClientStructGameObject();
        if (csObj != null && ((csObj->RenderFlags & 0x800) != 0 || Self.IsDead) && State != LineState.Dying2)
        {
            State = LineState.Dying2;
            StateTime = 0;
            DeathAnimationStarted = true;
        }

        // Handle focus target death
        if (FocusTarget && Service.ClientState.LocalPlayer?.IsDead == true && State != LineState.Dead)
        {
            if (State == LineState.Dying || State == LineState.Dying2)
            {
                State = LineState.Dead;
            }
            HadTarget = HasTarget;
            return;
        }

        // Check for target changes
        if (HasTarget != HadTarget)
        {
            if (HasTarget)
            {
                if (State == LineState.Dying)
                {
                    LastTargetPosition = LastTargetPosition2;
                }

                LastTargetId = GetTargetId();
                State = LineState.NewTarget;
                StateTime = 0;
                DeathAnimationStarted = false;
            }
            else
            {
                if (State == LineState.Switching || State == LineState.NewTarget)
                {
                    LastTargetPosition = LastTargetPosition2;
                }

                State = LineState.Dying;
                StateTime = 0;
                DeathAnimationStarted = true;
            }
        }

        // Check for target switching
        if (HasTarget && HadTarget)
        {
            var currentTargetId = GetTargetId();
            bool newTarget = currentTargetId != LastTargetId && LastTargetId != 0 && currentTargetId != 0xE0000000;

            if (newTarget)
            {
                if (State == LineState.Switching)
                {
                    LastTargetPosition = LastTargetPosition2;
                }

                State = LineState.Switching;
                LastMidHeight = MidHeight;
                StateTime = 0;
                LastTargetId = currentTargetId;
            }
        }

        // Update state-specific logic
        UpdateStateLogic();

        StateTime += Globals.Framework->FrameDeltaTime;
        HadTarget = HasTarget;
    }

    private void UpdateStateLogic()
    {
        switch (State)
        {
            case LineState.NewTarget:
                UpdateNewTargetState();
                break;
            case LineState.Dying:
            case LineState.Dying2:
                UpdateDyingState();
                break;
            case LineState.Switching:
                UpdateSwitchingState();
                break;
            case LineState.Idle:
                UpdateIdleState();
                break;
            case LineState.Dead:
                break;
        }
    }

    private void UpdateNewTargetState()
    {
        var target = GetTargetObject();
        if (target == null) return;

        AnimationAlpha = Math.Max(0, Math.Min(1, StateTime / Globals.Config.saved.NewTargetEaseTime));

        LastTargetHeight = target.GetHeadHeight();
        MidHeight = (Self.GetCursorHeight() + target.GetCursorHeight()) * 0.5f;

        if (AnimationAlpha >= 1)
        {
            State = LineState.Idle;
            LastTargetId = GetTargetId();
        }

        LastTargetPosition2 = target.Position;
    }

    private void UpdateDyingState()
    {
        AnimationAlpha = Math.Max(0, Math.Min(1, StateTime / Globals.Config.saved.NoTargetFadeTime));

        // Update mid height animation
        if (DeathAnimationStarted)
        {
            float animAlpha = Math.Min(1, (StateTime / Globals.Config.saved.NoTargetFadeTime) * Globals.Config.saved.DeathAnimationTimeScale);
            float start_height = Self.GetCursorHeight();
            float mid_height = (start_height + LastTargetHeight) * 0.5f;

            switch (Globals.Config.saved.DeathAnimation)
            {
                case LineDeathAnimation.Linear:
                    MidHeight = MathUtils.Lerpf(mid_height, 0, animAlpha);
                    break;
                case LineDeathAnimation.Square:
                    MidHeight = MathUtils.QuadraticLerpf(mid_height, 0, animAlpha);
                    break;
                case LineDeathAnimation.Cube:
                    MidHeight = MathUtils.CubicLerpf(mid_height, 0, animAlpha);
                    break;
            }
        }

        if (AnimationAlpha >= 1)
        {
            Sleeping = true;
        }
    }

    private void UpdateSwitchingState()
    {
        var target = GetTargetObject();
        if (target == null) return;

        AnimationAlpha = Math.Max(0, Math.Min(1, StateTime / Globals.Config.saved.NewTargetEaseTime));

        float start_height = Self.GetCursorHeight();
        float end_height = target.GetCursorHeight();
        float mid_height = (start_height + end_height) * 0.5f;

        MidHeight = MathUtils.Lerpf(LastMidHeight, mid_height, AnimationAlpha);
        LastTargetHeight = end_height;

        if (AnimationAlpha >= 1)
        {
            State = LineState.Idle;
            LastTargetId = GetTargetId();
        }

        LastTargetPosition2 = Vector3.Lerp(LastTargetPosition, target.Position, AnimationAlpha);
    }

    private void UpdateIdleState()
    {
        var target = GetTargetObject();
        if (target == null) return;

        AnimationAlpha = 1.0f;
        LastTargetHeight = target.GetCursorHeight();
        MidHeight = (Self.GetCursorHeight() + target.GetCursorHeight()) * 0.5f;

        LastTargetPosition = target.Position;
        LastTargetPosition2 = LastTargetPosition;
    }

    public ulong GetTargetId()
    {
        if (Self == null) return 0;

        if (FocusTarget)
        {
            if (Self != Service.ClientState.LocalPlayer)
            {
                return 0xE0000000;
            }

            var target = TargetSystem.Instance();
            if (target != null && target->FocusTarget != null && target->FocusTarget->EntityId != Service.ClientState.LocalPlayer.EntityId)
            {
                return target->FocusTarget->EntityId;
            }
            return 0;
        }
        else
        {
            return Self.TargetObject?.EntityId ?? 0;
        }
    }

    public IGameObject? GetTargetObject()
    {
        if (Self == null) return null;

        if (FocusTarget)
        {
            if (Self != Service.ClientState.LocalPlayer) return null;

            var target = TargetSystem.Instance();
            if (target != null && target->FocusTarget != null)
            {
                return Service.ObjectTable.SearchById(target->FocusTarget->EntityId);
            }
            return null;
        }
        else
        {
            return Self.TargetObject;
        }
    }

    public bool IsActive()
    {
        return !Sleeping && Self != null && Self.IsValid();
    }
}