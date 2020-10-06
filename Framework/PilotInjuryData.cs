using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using TisButAScratch.Framework;
using static TisButAScratch.Framework.GlobalVars;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TisButAScratch.Framework
{
    public class Injury
    {
        public string injuryID = "";
        public string injuryName = "";
        public InjuryLoc injuryLoc = InjuryLoc.INVALID_UNSET;
        public bool couldBeThermal = false;
        public int severity = 1;
        public string description = "";

        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectDataJO = new List<JObject>();
    }
    
    public class PilotInjuryHolder
    {
        private static PilotInjuryHolder _instance;
        public Dictionary<string, List<string>> pilotInjuriesMap;

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
        }


        //serialize injurymap (dictionary) to tag and save to company
            internal void SerializeInjuryState()
        {
            var injuryState = sim.CompanyTags.FirstOrDefault(( x) => x.StartsWith(injuryStateTag));
            GlobalVars.sim.CompanyTags.Remove(injuryState);
            injuryState = $"{injuryStateTag}{JsonConvert.SerializeObject(pilotInjuriesMap)}";
            ModInit.modLog.LogMessage($"Serialized injuryState and adding to company tags");
            GlobalVars.sim.CompanyTags.Add(injuryState);
        }

        //deserialize injurymap (dictionary) from tag and save to PilotInjuryHolder.Instance
        internal void DeserializeInjuryState()
        {
            if (sim.CompanyTags.Any(x => x.StartsWith(injuryStateTag)))
            {
                var injuryState = sim.CompanyTags.FirstOrDefault((x) => x.StartsWith(injuryStateTag)).Substring(12);
                HolderInstance.pilotInjuriesMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(injuryState);
                ModInit.modLog.LogMessage($"Deserializing injuryState and removing from company tags");
                GlobalVars.sim.CompanyTags.Remove(injuryState);
            }
            else
            {
                ModInit.modLog.LogMessage($"No injuryState to deserialize. Hopefully this is the first time you're running TBAS!");
            }
        }
    }
}
