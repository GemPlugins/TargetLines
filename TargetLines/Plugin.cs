using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DrahsidLib;
using TargetLines.GameLogic;
using TargetLines.Rendering;
using TargetLines.UI;

namespace TargetLines;

public class Plugin : IDalamudPlugin {
    private IDalamudPluginInterface PluginInterface;
    private IChatGui Chat { get; init; }
    private IClientState ClientState { get; init; }
    private ICommandManager CommandManager { get; init; }

    public string Name => "TargetLines";

    private bool PlayerWasNull = true;

    public const ImGuiWindowFlags OVERLAY_WINDOW_FLAGS =
          ImGuiWindowFlags.NoInputs
        | ImGuiWindowFlags.NoNav
        | ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoBackground;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IChatGui chat, IClientState clientState) {
        PluginInterface = pluginInterface;
        Chat = chat;
        ClientState = clientState;
        CommandManager = commandManager;

        DrahsidLib.DrahsidLib.Initialize(pluginInterface, DrawTooltip);

        InitializeCommands();
        InitializeConfig();
        Globals.Initialize();
        InitializeUI();

        TargetLineManager.InitializeTargetLines();
    }

    public static void DrawTooltip(string text) {
        if (ImGui.IsItemHovered() && Globals.Config.HideTooltips == false) {
            ImGui.SetTooltip(text);
        }
    }

    private void InitializeCommands() {
        Commands.Initialize();
    }

    private void InitializeConfig() {
        Globals.Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Globals.Config.Initialize();
    }

    private void InitializeUI() {
        TargetLines.UI.Windows.Windows.Initialize();
        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi += Commands.ToggleConfig;
    }

    private unsafe void DrawOverlay() {
        TargetLineManager.DrawOverlay();
    }

    private void OnDraw() {
        if (ShaderSingleton.ShowingProgress) {
            ShaderSingleton.DrawShaderCompilationProgress();
        }

        Globals.Renderer?.OnStartFrame();
        TargetLines.UI.Windows.Windows.System.Draw();

        if (Service.ClientState.LocalPlayer == null) {
            PlayerWasNull = true;
            return;
        }

        if (PlayerWasNull)
        {
            TargetLineManager.InitializeTargetLines();
            PlayerWasNull = false;
        }

        bool combat_flag = Service.Condition[ConditionFlag.InCombat];

        if (Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Service.Condition[ConditionFlag.WatchingCutscene])
        {
            return;
        }

        if (!Globals.Config.saved.OnlyUnsheathed
            || (Service.ClientState.LocalPlayer.StatusFlags & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.WeaponOut) != 0) {
            if ((Globals.Config.saved.OnlyInCombat == InCombatOption.None
                || (Globals.Config.saved.OnlyInCombat == InCombatOption.InCombat && combat_flag))
                || (Globals.Config.saved.OnlyInCombat == InCombatOption.NotInCombat && !combat_flag)) {
                if (Globals.Config.saved.ToggledOff == false) {
                    ImGuiUtils.WrapBegin("##TargetLinesOverlay", OVERLAY_WINDOW_FLAGS, DrawOverlay);
                }
            }
        }

        Globals.Renderer?.OnEndFrame();
    }

#region IDisposable Support
    protected virtual void Dispose(bool disposing) {
        if (!disposing) {
            return;
        }

        PluginInterface.SavePluginConfig(Globals.Config);

        PluginInterface.UiBuilder.Draw -= OnDraw;
        Globals.Dispose();
        TargetLines.UI.Windows.Windows.Dispose();
        PluginInterface.UiBuilder.OpenConfigUi -= Commands.ToggleConfig;

        Commands.Dispose();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
#endregion
}
