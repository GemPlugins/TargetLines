using System.Collections.Generic;

namespace TargetLines.GameLogic; 
public static class ClassJobHelper {
    public enum TargetFlags : int {
        /* 0x0001 */ Any = (1 << 0),        // any entity
        /* 0x0002 */ Player = (1 << 1),     // any Player character
        /* 0x0004 */ Enemy = (1 << 2),      // any enemy
        /* 0x0008 */ NPC = (1 << 3),        // any npc
        /* 0x0010 */ Alliance = (1 << 4),   // any Alliance member (Excluding party)
        /* 0x0020 */ Party = (1 << 5),      // any Party member
        /* 0x0040 */ Self = (1 << 6),       // local player
        /* 0x0080 */ DPS = (1 << 7),        // include DPS
        /* 0x0100 */ Healer = (1 << 8),     // include Healer
        /* 0x0200 */ Tank = (1 << 9),       // include Tank
        /* 0x0400 */ CrafterGatherer = (1 << 10),   // include Crafter/Gatherers
        /* 0x0800 */ MeleeDPS = (1 << 11),          // include Melee DPS
        /* 0x1000 */ PhysicalRangedDPS = (1 << 12), // include Physical Ranged DPS
        /* 0x2000 */ MagicalRangedDPS = (1 << 13),  // include Magical Ranged DPS
        /* 0x4000 */ PureHealer = (1 << 14),        // include Pure Healer
        /* 0x8000 */ ShieldHealer = (1 << 15),      // include Shield Healer
    }

    public static string[] TargetFlagDescriptions = {
        "Unconditionally draw this line",
        "Draw this line when a player is the target",
        "Draw this line when an enemy is the target",
        "Draw this line when an NPC is the target",
        "Draw this line when an alliance member is the target",
        "Draw this line when a party member is the target",
        "Draw this line when you are the target",
        "Draw this line when a player with a DPS role is the target",
        "Draw this line when a player with a Healer role is the target",
        "Draw this line when a player with a Tank role is the target",
        "Draw this line when a player who is a crafter or gatherer is the target",
        "Draw this line when a player with a Melee DPS role is the target (nullifies DPS flag)",
        "Draw this line when a player with a Physical Ranged DPS role is the target (nullifies DPS flag)",
        "Draw this line when a player with a Magical Ranged DPS role is the target (nullifies DPS flag)",
        "Draw this line when a player with a Pure Healer role is the target (nullifies Healer flag)",
        "Draw this line when a player with a Shield Healer role is the target (nullifies Healer flag)"
    };


    public enum ClassJob : uint {
        /*  0 */ Adventurer = 0,
        /*  1 */ Gladiator,
        /*  2 */ Pugilist,
        /*  3 */ Marauder,
        /*  4 */ Lancer,
        /*  5 */ Archer, 
        /*  6 */ Conjurer,
        /*  7 */ Thaumaturge,
        /*  8 */ Carpenter,
        /*  9 */ Blacksmith,
        /* 10 */ Armorer,
        /* 11 */ Goldsmith,
        /* 12 */ Leatherworker,
        /* 13 */ Weaver,
        /* 14 */ Alchemist,
        /* 15 */ Culinarian,
        /* 16 */ Miner,
        /* 17 */ Botanist,
        /* 18 */ Fisher,
        /* 19 */ Paladin,
        /* 20 */ Monk,
        /* 21 */ Warrior,
        /* 22 */ Dragoon,
        /* 23 */ Bard,
        /* 24 */ WhiteMage,
        /* 25 */ BlackMage,
        /* 26 */ Arcanist,
        /* 27 */ Summoner,
        /* 28 */ Scholar,
        /* 29 */ Rogue,
        /* 30 */ Ninja,
        /* 31 */ Machinist,
        /* 32 */ DarkKnight,
        /* 33 */ Astrologian,
        /* 34 */ Samurai,
        /* 35 */ RedMage,
        /* 36 */ BlueMage,
        /* 37 */ Gunbreaker,
        /* 38 */ Dancer,
        /* 39 */ Reaper,
        /* 40 */ Sage,
        /* 41 */ Viper,
        /* 42 */ Pictomancer,
        Count
    };

    public static ulong ClassJobToBit(ClassJob id) {
        if (id >= ClassJob.Count || id < 0) {
            return 0;
        }

        return (ulong)(1UL << (int)id);
    }

    public static ulong ClassJobToBit(int id) {
        if (id >= (int)ClassJob.Count || id < 0) {
            return 0;
        }

        return (ulong)(1UL << id);
    }

    public static List<ClassJob> DPSJobs = new List<ClassJob> {
        ClassJob.Adventurer, ClassJob.Pugilist, ClassJob.Lancer, ClassJob.Archer,
        ClassJob.Thaumaturge, ClassJob.Monk, ClassJob.Dragoon, ClassJob.Bard,
        ClassJob.BlackMage, ClassJob.Arcanist, ClassJob.Summoner, ClassJob.Rogue,
        ClassJob.Ninja, ClassJob.Machinist, ClassJob.Samurai, ClassJob.RedMage,
        ClassJob.BlueMage, ClassJob.Dancer, ClassJob.Reaper, ClassJob.Viper, ClassJob.Pictomancer
    };

    public static List<ClassJob> HealerJobs = new List<ClassJob> {
        ClassJob.Conjurer, ClassJob.WhiteMage, ClassJob.Scholar, ClassJob.Astrologian,
        ClassJob.Sage
    };

    public static List<ClassJob> TankJobs = new List<ClassJob> {
        ClassJob.Gladiator, ClassJob.Marauder, ClassJob.Paladin, ClassJob.Warrior,
        ClassJob.DarkKnight, ClassJob.Gunbreaker
    };

    public static List<ClassJob> CrafterGathererJobs = new List<ClassJob> {
        ClassJob.Carpenter, ClassJob.Blacksmith, ClassJob.Armorer, ClassJob.Goldsmith,
        ClassJob.Leatherworker, ClassJob.Weaver, ClassJob.Alchemist, ClassJob.Culinarian,
        ClassJob.Miner, ClassJob.Botanist, ClassJob.Fisher
    };

    public static List<ClassJob> MeleeDPSJobs = new List<ClassJob> {
        ClassJob.Monk, ClassJob.Dragoon, ClassJob.Ninja, ClassJob.Samurai,
        ClassJob.Reaper, ClassJob.Pugilist, ClassJob.Lancer, ClassJob.Rogue,
        ClassJob.Adventurer, ClassJob.Viper
    };

    public static List<ClassJob> PhysicalRangedDPSJobs = new List<ClassJob> {
        ClassJob.Bard, ClassJob.Machinist, ClassJob.Dancer, ClassJob.Archer
    };

    public static List<ClassJob> MagicalRangedDPSJobs = new List<ClassJob> {
        ClassJob.BlackMage, ClassJob.Summoner, ClassJob.RedMage, ClassJob.BlueMage,
        ClassJob.Thaumaturge, ClassJob.Arcanist, ClassJob.Pictomancer
    };

    public static List<ClassJob> PureHealerJobs = new List<ClassJob> {
        ClassJob.WhiteMage, ClassJob.Astrologian, ClassJob.Conjurer
    };

    public static List<ClassJob> ShieldHealerJobs = new List<ClassJob> {
        ClassJob.Scholar, ClassJob.Sage
    };

    public static bool CompareTargetSettings(ref TargetSettings goal, ref TargetSettings entity) {
        TargetFlags gflags = goal.Flags;
        TargetFlags eflags = entity.Flags;
        bool ret = false;

        // entity does not matter if this rule accepts any entity
        if ((gflags & TargetFlags.Any) != 0) {
            return true;
        }

        // entity must be Player, check if their job is on the job list, if the job list has any entries
        if (goal.Jobs != 0 && (eflags & TargetFlags.Player) != 0) {
            bool invalid_job = true;
            for (int index = 0; index < (int)ClassJob.Count; index++) {
                ulong jobflag = ClassJobToBit(index);
                if ((goal.Jobs & jobflag) != 0 && (entity.Jobs & jobflag) != 0) {
                    invalid_job = false;
                    break;
                }
            }

            if (invalid_job) {
                return false;
            }
        }

        // nullify role flags when specific roles are selected
        if ((gflags & TargetFlags.DPS) != 0) {
            if ((gflags & TargetFlags.MeleeDPS) != 0 || (gflags & TargetFlags.PhysicalRangedDPS) != 0 || (gflags & TargetFlags.MagicalRangedDPS) != 0) {
                gflags &= ~TargetFlags.DPS;
            }
        }

        if ((gflags & TargetFlags.Healer) != 0) {
            if ((gflags & TargetFlags.PureHealer) != 0 || (gflags & TargetFlags.ShieldHealer) != 0) {
                gflags &= ~TargetFlags.Healer;
            }
        }

        // check if any other flags are true
        for (int index = 1; index < 16; index++) {
            bool gbit = ((int)gflags & (1 << index)) != 0;
            bool ebit = ((int)eflags & (1 << index)) != 0;

            if (gbit && ebit) {
                ret = true;
                break;
            }
        }

        return ret;
    }
}
