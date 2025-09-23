using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using DrahsidLib;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;
using static TargetLines.GameLogic.ClassJobHelper;

namespace TargetLines.GameLogic;

public static class GameObjectExtensions {
    const int CursorHeightOffset = 0x124;
    const float HeadHeightOffset = -0.2f;

    public static unsafe bool TargetIsTargetable(this IGameObject obj) {
        if (obj.TargetObject == null) {
            return false;
        }
        CSGameObject* targetobj = (CSGameObject*)obj.TargetObject.Address;
        return targetobj->GetIsTargetable();
    }

    public static float GetHeadHeight(this IGameObject obj) => Marshal.PtrToStructure<float>(obj.Address + CursorHeightOffset) + HeadHeightOffset;
    public static bool GetIsPlayerCharacter(this IGameObject obj) => obj.ObjectKind == ObjectKind.Player;
    public static bool GetIsBattleNPC(this IGameObject obj) => obj.ObjectKind == ObjectKind.BattleNpc;
    public static bool GetIsBattleChara(this IGameObject obj) => obj is IBattleChara;
    public static IPlayerCharacter? GetPlayerCharacter(this IGameObject obj) => obj as IPlayerCharacter;
    public static unsafe CSGameObject* GetClientStructGameObject(this IGameObject obj) => (CSGameObject*)obj.Address;

    public static unsafe TargetSettings GetTargetSettings(this IGameObject obj) {
        TargetSettings settings = new TargetSettings();
        settings.Flags = TargetFlags.Any;

        if (Service.ClientState.LocalPlayer != null) {
            if (obj.EntityId == Service.ClientState.LocalPlayer.EntityId) {
                settings.Flags |= TargetFlags.Self;
            }
        }

        if (obj.GetIsPlayerCharacter()) {
            GroupManager* gm = GroupManager.Instance();
            settings.Flags |= TargetFlags.Player;
            foreach (PartyMember member in gm->MainGroup.PartyMembers) {
                if (member.EntityId == obj.EntityId) {
                    settings.Flags |= TargetFlags.Party;
                }
            }

            if ((gm->MainGroup.AllianceFlags & 1) != 0 && (settings.Flags & TargetFlags.Party) != 0) {
                foreach (PartyMember member in gm->MainGroup.AllianceMembers) {
                    if (member.EntityId == obj.EntityId) {
                        settings.Flags |= TargetFlags.Alliance;
                    }
                }
            }

            ClassJob ID = (ClassJob)obj.GetPlayerCharacter().ClassJob.RowId;
            settings.Jobs = ClassJobToBit(ID);
            if (DPSJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.DPS;
                if (MeleeDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.MeleeDPS;
                }
                else if (PhysicalRangedDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.PhysicalRangedDPS;
                }
                else if (MagicalRangedDPSJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.MagicalRangedDPS;
                }
            }
            else if (HealerJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.Healer;
                if (PureHealerJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.PureHealer;
                }
                else if (ShieldHealerJobs.Contains(ID)) {
                    settings.Flags |= TargetFlags.ShieldHealer;
                }
            }
            else if (TankJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.Tank;
            }
            else if (CrafterGathererJobs.Contains(ID)) {
                settings.Flags |= TargetFlags.CrafterGatherer;
            }
        }
        else if (obj.GetIsBattleNPC()) {
            settings.Flags |= TargetFlags.Enemy;
        }
        else {
            settings.Flags |= TargetFlags.NPC;
        }

        return settings;
    }
}

