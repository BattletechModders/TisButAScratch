using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;
using BattleTech.Data;
using System.Threading.Tasks;

using fastJSON;
using Newtonsoft.Json.Linq;

namespace TisButAScratch.Framework
{
    public class GlobalVars
    {
        internal static SimGameState sim;
        internal static CombatGameState combat;
        internal const string aiPilotFlag = "AI_TEMP_";
        internal const string iGUID = "iGUID_";
        internal const string injuryStateTag = "injuryState_";
        internal const string CrippledTag = "CRIPPLED";
        internal const string MissionKilledStat = "MissionKilled";


        internal static Injury CRIPPLED = new Injury
        {
            injuryID = "CRIPPLED",
            injuryName = "CRIPPLED",
            injuryLoc = InjuryLoc.INVALID_UNSET,
            couldBeThermal = false,
            description = "Whether due to amputation or extensive tissue damage, this pilot is crippled and is unable to deploy without rehabilitation.",
            severity = 100,
            effects = new List<EffectData>(),
            effectDataJO = new List<JObject>()
        };

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();


        public enum InjuryLoc
        {
            INVALID_UNSET,
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
