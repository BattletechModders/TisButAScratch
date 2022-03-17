using System.Collections.Generic;
using System.Linq;
using BattleTech;
using static TisButAScratch.Framework.GlobalVars;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TisButAScratch.Framework
{
    public class PilotingReq
    {
        public string ComponentTag = "";
        public string PilotTag = "";
        public string PilotTagDisplay = "";
    }
    public class Injury
    {
        public string injuryID = "";
        public string injuryID_Post = "";
        public string injuryName = "";
        public InjuryLoc injuryLoc = InjuryLoc.NOT_SET;
        public bool couldBeThermal = false;
        public int severity = 1;
        public string description = "";

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
    }

    public class BleedingEffect
    {
        public string bleedingEffectID = "";
        public string bleedingName = "";
        public int bleedingEffectLvl = 0;

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
    }

    public class SimBleedingEffect
    {
        public string simBleedingEffectID = "";
        public int bleedingEffectLvl = 0;

        [JsonIgnore]
        public List<SimGameEventResult> simResult = new List<SimGameEventResult>();
        public List<JObject> simResultJO = new List<JObject>();
    }
    
    public class PilotInjuryHolder
    {
        private static PilotInjuryHolder _instance;
        public Dictionary<string, List<string>> pilotInjuriesMap;
        public Dictionary<string, List<string>> combatInjuriesMap; //added for temprary injury storage to allow clean combat restarts
        public Dictionary<string, float> bloodStatForSimGame; //added for temprary injury storage to allow clean combat restarts
        public int injuryStat;

        public static PilotInjuryHolder HolderInstance
        {
            get
            {
                if (_instance == null) _instance = new PilotInjuryHolder();
                return _instance;
            }
        }

        internal void Initialize()
        {
            pilotInjuriesMap = new Dictionary<string, List<string>>();
            combatInjuriesMap = new Dictionary<string, List<string>>();
            bloodStatForSimGame = new Dictionary<string, float>();
        }

        
        //serialize injurymap (dictionary) to tag and save to company
        internal void SerializeInjuryState()
        {
            var injuryState = sim.CompanyTags.FirstOrDefault(( x) => x.StartsWith(injuryStateTag));
            sim.CompanyTags.Remove(injuryState);
            injuryState = $"{injuryStateTag}{JsonConvert.SerializeObject(HolderInstance.pilotInjuriesMap)}";
            ModInit.modLog?.Info?.Write($"Serialized injuryState and adding to company tags. State was {injuryState}");
            sim.CompanyTags.Add(injuryState);
        }

        //deserialize injurymap (dictionary) from tag and save to PilotInjuryHolder.Instance
        internal void DeserializeInjuryState()
        {
            if (sim.CompanyTags.Any(x => x.StartsWith(injuryStateTag)))
            {
                var InjuryStateCTag = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(injuryStateTag));
                var injuryState = InjuryStateCTag?.Substring(injuryStateTag.Length);
                HolderInstance.pilotInjuriesMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(injuryState);
                ModInit.modLog?.Info?.Write($"Deserializing injuryState and removing from company tags. State was {injuryState}");
                sim.CompanyTags.Remove(InjuryStateCTag);
            }
            else
            {
                ModInit.modLog?.Info?.Write($"No injuryState to deserialize. Hopefully this is the first time you're running TBAS!");
            }
        }
    }
}
