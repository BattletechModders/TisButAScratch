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
            new Regex("^TBAS_SimBleed__(?<type>.*?)__(?<value>.*?)$",//    __(?<operation>.*?)$",
                RegexOptions.Compiled); //shamelessly stolen from BlueWinds

        public static SimGameState sim;
        public const string aiPilotFlag = "AI_TEMP_";
        public const string iGUID = "iGUID_";
        public const string injuryStateTag = "injuryState_";
        public const string DEBILITATEDTAG = "DEBILITATED";
        public const string MissionKilledStat = "MissionKilled";
        public const string PermanentlyIncapacitated = "PermanentlyIncapacitated";

        public static Injury DEBIL = new Injury
        {
            injuryID = "DEBILITATED",
            injuryName = "DEBILITATED",
            injuryLoc = InjuryLoc.NOT_SET,
            couldBeThermal = false,
            description = "Whether due to amputation or extensive tissue damage, this pilot is debilitated and is unable to deploy without rehabilitation.",
            severity = 100,
            effects = new List<EffectData>(),
            effectDataJO = new List<JObject>()
        };

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();


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
