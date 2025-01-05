using System.Collections.Generic;
using System.Text.RegularExpressions;
using BattleTech;
using fastJSON;
using Newtonsoft.Json.Linq;

namespace TisButAScratch.Framework
{
    public class GlobalVars
    {
        public static bool rollDie(int diecount, int sides, int success, out int sum)
        {
            sum = 0;
            for (int i = 0; i < diecount; i++)
            {
                var currentRoll = UnityEngine.Random.Range(1, sides + 1);
                sum += currentRoll;
            }

            return sum > success;
        }

        public static Regex TBAS_SimBleedStatMod =
            new Regex("^TBAS_SimBleed__(?<type>.*?)__(?<value>.*?)$", //    __(?<operation>.*?)$",
                RegexOptions.Compiled); //shamelessly stolen from BlueWinds

        public static SimGameState sim;
        public const string aiPilotFlag = "AI_TEMP_";
        public const string iGUID = "iGUID_";

        public const string injuryStateTag = "injuryState_";

        //public const string DEBILITATEDTAG = "DEBILITATED";
        public const string MissionKilledStat = "MissionKilled";
        public const string PermanentlyIncapacitated = "PermanentlyIncapacitated";
        public const string DisableDebilEventsTag = "disable_debilHeal";
        public const string DebilitatedPrefix = "DEBILITATED";
        public const string DebilitatedHead = "DEBILITATED_Head";
        public const string DebilitatedArmL = "DEBILITATED_ArmL";
        public const string DebilitatedTorso = "DEBILITATED_Torso";
        public const string DebilitatedArmR = "DEBILITATED_ArmR";
        public const string DebilitatedLegL = "DEBILITATED_LegL";
        public const string DebilitatedLegR = "DEBILITATED_LegR";
        public const string DebilitatedStat = "IsDebilitated";
        public static List<string> DebilLocationList = new List<string>()
        {
            "DEBILITATED_Head", "DEBILITATED_ArmL", "DEBILITATED_Torso", "DEBILITATED_ArmR", "DEBILITATED_LegL",
            "DEBILITATED_LegR"
        };
   
        public static Dictionary<string, string> DebilitatingInjuryDescriptions = new Dictionary<string, string>()
        {
            {
                "DEBILITATED_Head",
                "This pilot has suffered extensive damage to their head, and is unable to deploy without rehabilitation."
            },
            {
                "DEBILITATED_ArmL",
                "This pilot has suffered extensive damage to their left arm, and is unable to deploy without rehabilitation."
            },
            {
                "DEBILITATED_Torso",
                "This pilot has suffered extensive damage to their torso, and is unable to deploy without rehabilitation."
            },
            {
                "DEBILITATED_ArmR",
                "This pilot has suffered extensive damage to their right arm, and is unable to deploy without rehabilitation."
            },
            {
                "DEBILITATED_LegL",
                "This pilot has suffered extensive damage to their left leg, and is unable to deploy without rehabilitation."
            },
            {
                "DEBILITATED_LegR",
                "This pilot has suffered extensive damage to their right leg, and is unable to deploy without rehabilitation."
            },
        };


        public enum InjuryLoc
        {
            NOT_SET,
            Psych,
            Head,
            ArmL,
            Torso,
            ArmR,
            LegL,
            LegR,
        }
    }
}