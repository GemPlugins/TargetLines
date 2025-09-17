using Dalamud.Configuration;
using DrahsidLib;
using FFXIVClientStructs.FFXIV.Common.Math;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static TargetLines.GameLogic.ClassJobHelper;

namespace TargetLines;

public enum InCombatOption {
    None = 0,
    InCombat,
    NotInCombat,
    Count
}

public enum LineDeathAnimation {
    Linear,
    Square,
    Cube,
    Count
};

public enum LinePartyMode
{
    None = 0,
    PartyOnly,
    PartyOnlyInAlliance,
    AllianceOnly,
    Count
}

public class TargetSettings {
    public TargetFlags Flags = 0;
    public UInt64 Jobs = 0; // bitfield from ClassJob

    public TargetSettings() {
        Flags = 0;
        Jobs = 0;
    }

    public TargetSettings(TargetFlags flags, UInt64 jobs = 0) {
        Flags = flags;
        Jobs = jobs;
    }
};

[StructLayout(LayoutKind.Explicit)]
public struct RGBA {
    [FieldOffset(0x00)] public uint raw;
    [FieldOffset(0x00)] public byte r;
    [FieldOffset(0x01)] public byte g;
    [FieldOffset(0x02)] public byte b;
    [FieldOffset(0x03)] public byte a;

    public RGBA(byte A, byte B, byte G, byte R) {
        r = R;
        g = G;
        b = B;
        a = A;
    }

    [JsonIgnore]
    public Vector4 Color {
        get {
            return new Vector4((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
        }
        set {
            a = (byte)(value.W * 255.0f);
            b = (byte)(value.Z * 255.0f);
            g = (byte)(value.Y * 255.0f);
            r = (byte)(value.X * 255.0f);
        }
    }
}

public class LineColor {
    public RGBA Color = new RGBA(0x80, 0x00, 0xBF, 0xFF);
    public RGBA OutlineColor = new RGBA(0x80, 0x00, 0x00, 0x00);
    public bool Visible = true;
    public bool UseQuad = false;

    public LineColor() {
        Color = new RGBA(0x80, 0x00, 0xBF, 0xFF);
        OutlineColor = new RGBA(0x80, 0x00, 0x00, 0x00);
        Visible = true;
        UseQuad = false;
    }

    public LineColor(RGBA color, RGBA outline, bool visible = true, bool usequad = false) {
        Color = color;
        OutlineColor = outline;
        Visible = visible;
        UseQuad = usequad;
    }

    public LineColor(RGBA color, bool visible = true, bool usequad = false) {
        Color = color;
        Visible = visible;
        UseQuad = usequad;
    }
}

public class TargetSettingsPair {
    public TargetSettings From = new TargetSettings();
    public TargetSettings To = new TargetSettings();
    public LineColor LineColor = new LineColor();
    public int Priority = -1;
    public bool FocusTarget = false;
    public Guid UniqueId = Guid.Empty;
    
    public TargetSettingsPair(TargetSettings from, TargetSettings to, LineColor lineColor, int priority = -1, bool focus = false) {
        From = from;
        To = to;
        LineColor = lineColor;
        Priority = priority;
        FocusTarget = focus;
        UniqueId = Guid.NewGuid();
    }

    public int GetPairPriority(bool focus, bool sort = false) {
        int priority = Priority;

        if (priority == -1) {
            for (int index = 0; index < 16; index++) {
                int bit = 1 << index;
                if (((int)From.Flags & bit) != 0) {
                    priority += index;
                }
                if (((int)To.Flags & bit) != 0) {
                    priority += index;
                }
            }

            if (From.Jobs != 0) {
                priority += 1;
            }

            if (To.Jobs != 0) {
                priority += 1;
            }
        }

        // fix any -> any
        if (priority == -1)
        {
            priority = 0;
        }

        if (!sort)
        {
            if (focus != FocusTarget)
            {
                return -1;
            }
        }

        return priority;
    }
}

public class SavedConfig {
    public float ArcHeightScalar = 1.0f;
    public float PlayerHeightBump = 0.0f;
    public float EnemyHeightBump = 0.0f;
    public float LineThickness = 16.0f;
    public float OutlineThickness = 0.5f;
    public float OutlineDistance = 0.5f;
    public float OutlineFadeRatio = 0.5f;
    public float NewTargetEaseTime = 0.25f;
    public float NoTargetFadeTime = 0.25f;
    public float WaveAmplitudeOffset = 0.175f;
    public float WaveFrequencyScalar = 3.0f;
    public float FadeToEndScalar = 0.2f;
    public float HeightScale = 1.0f;
    public int TextureCurveSampleCount = 23;
    public int TextureCurveSampleCountMin = 5;
    public int TextureCurveSampleCountMax = 23;
    public InCombatOption OnlyInCombat = InCombatOption.None;
    public bool OnlyUnsheathed = false;
    public bool SolidColor = false;
    public bool FadeToEnd = true;
    public bool ToggledOff = false;
    public bool OcclusionCulling = false;
    public bool PulsingEffect = true;
    public bool BreathingEffect = true;
    public bool CompactFlagDisplay = false;
    public bool UIOcclusion = true;
    public bool DynamicSampleCount = true;
    public bool UseScreenSpaceLOD = true;
    public bool ViewAngleSampling = true;
    public LinePartyMode LinePartyMode = LinePartyMode.None;
    public int MaxTargetLines = 100;

    public bool DebugDynamicSampleCount = false;
    public bool DebugUICollision = false;
    public bool DebugUIFinalRectList = true;
    public bool DebugUICollisionArea = true;
    public bool DebugUIInitialRectList = true;
    public bool DebugUIMergedRectList = true;
    [Obsolete] public bool DebugDXLines = false;
    public bool DebugDepthTexture = false;
    public bool DebugUIMaskTexture = false;

    public LineColor LineColor = new LineColor(new RGBA(0xC0, 0x80, 0x80, 0x80), new RGBA(0x80, 0x00, 0x00, 0x00), true); // fallback color
    public LineDeathAnimation DeathAnimation = LineDeathAnimation.Linear;
    public float DeathAnimationTimeScale = 1.0f;

    [Obsolete] public RGBA? PlayerPlayerLineColor;
    [Obsolete] public RGBA? PlayerEnemyLineColor;
    [Obsolete] public RGBA? EnemyPlayerLineColor;
    [Obsolete] public RGBA? EnemyEnemyLineColor;
    [Obsolete] public RGBA? OtherLineColor;
    [Obsolete] public RGBA? OutlineColor;
    [Obsolete] public bool? OnlyTargetingPC;
}

public class Configuration : IPluginConfiguration {
    int IPluginConfiguration.Version { get; set; }

    #region Saved configuration values
    public SavedConfig saved = new SavedConfig();
    public List<TargetSettingsPair> LineColors = new List<TargetSettingsPair>();
    public bool HideTooltips = false;
    #endregion

    public void SortLineColors() {
        Globals.Config.LineColors = Globals.Config.LineColors.OrderByDescending(obj => obj.GetPairPriority(false, false)).ToList();
    }

    public void InitializeDefaultLineColorsConfig() {
        LineColors = new List<TargetSettingsPair>() {
                // player -> player default
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Player),
                    new TargetSettings(TargetFlags.Player),
                    new LineColor(new RGBA(0xC0, 0x50, 0xAF, 0x4C), true, false) // greenish
                ),
                // player -> party (Focus) default
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Player),
                    new TargetSettings(TargetFlags.Party),
                    new LineColor(new RGBA(0xC0, 0xB0, 0x27, 0x9C), true, true), -1, true // greenish
                ),
                // player -> enemy default
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Player),
                    new TargetSettings(TargetFlags.Enemy),
                    new LineColor(new RGBA(0x80, 0x36, 0x43, 0xF4), true, false) // reddish
                ), 
                // enemy -> player default
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Enemy),
                    new TargetSettings(TargetFlags.Player),
                    new LineColor(new RGBA(0xC0, 0x00, 0x00, 0xFF), true, true) // red
                ),
                // enemy -> enemy default
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Enemy),
                    new TargetSettings(TargetFlags.Enemy),
                    new LineColor(new RGBA(0xC0, 0xB0, 0x27, 0x9C), true, true) // purpleish
                ),
                // any -> any default (invisible)
                new TargetSettingsPair(
                    new TargetSettings(TargetFlags.Any),
                    new TargetSettings(TargetFlags.Any),
                    new LineColor(new RGBA(0xC0, 0x80, 0x80, 0x80), false, false), // grey
                    0
                )
            };
    }

    public void Initialize() {
        int LineColorsWasNull = 0;

        // manage upgrades
        if (saved == null) {
            saved = new SavedConfig();
        }

        if (LineColors == null) {
            LineColors = new List<TargetSettingsPair>();
            LineColorsWasNull = 1;
        }

        if (saved.OnlyInCombat is bool) {
            saved.OnlyInCombat = InCombatOption.None;
            Service.ChatGui.Print("Warning! If you had the \"OnlyInCombat\" setting set, you will have to reenable it!");
        }

        if (saved.PlayerPlayerLineColor != null) {
            TargetSettings from =  new TargetSettings(TargetFlags.Player, 0);
            TargetSettings to = new TargetSettings(TargetFlags.Player, 0);

            LineColorsWasNull++;
            LineColors.Add(new TargetSettingsPair(from, to, new LineColor((RGBA)saved.PlayerPlayerLineColor, (RGBA)saved.OutlineColor, true)));

            saved.PlayerPlayerLineColor = null;
        }

        if (saved.PlayerEnemyLineColor != null) {
            TargetSettings from = new TargetSettings(TargetFlags.Player, 0);
            TargetSettings to = new TargetSettings(TargetFlags.Enemy, 0);

            LineColorsWasNull++;
            LineColors.Add(new TargetSettingsPair(from, to, new LineColor((RGBA)saved.PlayerEnemyLineColor, (RGBA)saved.OutlineColor, true)));
            saved.PlayerEnemyLineColor = null;
        }

        if (saved.EnemyPlayerLineColor != null) {
            TargetSettings from = new TargetSettings(TargetFlags.Enemy, 0);
            TargetSettings to = new TargetSettings(TargetFlags.Player, 0);

            LineColorsWasNull++;
            LineColors.Add(new TargetSettingsPair(from, to, new LineColor((RGBA)saved.EnemyPlayerLineColor, (RGBA)saved.OutlineColor, true)));
            saved.EnemyPlayerLineColor = null;
        }

        if (saved.EnemyEnemyLineColor != null) {
            TargetSettings from = new TargetSettings(TargetFlags.Enemy, 0);
            TargetSettings to = new TargetSettings(TargetFlags.Enemy, 0);

            LineColorsWasNull++;
            LineColors.Add(new TargetSettingsPair(from, to, new LineColor((RGBA)saved.EnemyEnemyLineColor, (RGBA)saved.OutlineColor, true)));
            saved.EnemyEnemyLineColor = null;
        }

        // Initialize values if we didn't upgrade from an old config
        if (LineColorsWasNull == 1) {
            InitializeDefaultLineColorsConfig();
        }

        for (int index = 0; index < LineColors.Count; index++) {
            if (LineColors[index].UniqueId == null || LineColors[index].UniqueId == Guid.Empty) {
                LineColors[index].UniqueId = Guid.NewGuid();
            }
        }
    }

    public void Save() {
        Service.Interface.SavePluginConfig(this);
    }
}
