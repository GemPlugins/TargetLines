using Dalamud.Game.Command;
using DrahsidLib;

namespace TargetLines.UI;

public static class Commands {
    public static void Initialize() {
        Service.CommandManager.AddHandler("/ptlines", new CommandInfo(OnPTLines)
        {
            ShowInHelp = true,
            HelpMessage = "Toggle the configuration window."
        });

        Service.CommandManager.AddHandler("/ttl", new CommandInfo(OnTTL)
        {
            ShowInHelp = true,
            HelpMessage = "Toggle target line overlay."
        });
    }

    public static void Dispose() {
        Service.CommandManager.RemoveHandler("/ptlines");
        Service.CommandManager.RemoveHandler("/ttl");
    }


    public static void ToggleConfig() {
        TargetLines.UI.Windows.Windows.Config.IsOpen = !TargetLines.UI.Windows.Windows.Config.IsOpen;
    }

    public static void OnPTLines(string command, string args) {
        TargetLines.UI.Windows.Windows.Config.IsOpen = !TargetLines.UI.Windows.Windows.Config.IsOpen;
    }

    private static void OnTTL(string command, string args) {
        string str = "on";
        Globals.Config.saved.ToggledOff = !Globals.Config.saved.ToggledOff;

        if (Globals.Config.saved.ToggledOff) {
            str = "off";
        }

        Service.ChatGui.Print($"Target Lines overlay toggled {str}");
    }
}
