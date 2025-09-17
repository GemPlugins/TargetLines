using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using System;

namespace TargetLines.UI;

public class ImGuiUtils {
    public static bool WrapBegin(string name, ImGuiWindowFlags flags, Action fn) {
        bool began = false;

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().Pos);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        if (ImGui.Begin(name, flags)) {
            began = true;
            fn();
            ImGui.End();
        }

        return began;
    }
}
