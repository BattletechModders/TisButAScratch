# TisButAScratch

This mod overhauls the Battletech injury system, and lets modders apply different stat effects based on injuries a pilot receives. When a pilot receives an injury, a roll is made to determine both the location of the injury and the injury itself. Possible injury locations are: NOT_SET, Head, ArmL, Torso, ArmR, LegL, and LegR.

Injuries are defined in the settings.json, and have the following structure:
```
"InjuryEffectsList": [
		{
			"injuryID" : "HeadConcussion",
			"injuryName" : "Concussion",
			"injuryLoc" : "Head",
			"couldBeThermal" : false,
			"severity" : 1,
			"description" : "This pilot is concussed. Things are spinny.",
			"effectDataJO" : [
				{
					"durationData": {
					},
					"targetingData": {
						"effectTriggerType": "Passive",
						"effectTargetType": "Creator",
						"showInStatusPanel": true
					},
					"effectType": "StatisticEffect",
					"Description": {
						"Id": "Concussed",
						"Name": "Concussed Instability",
						"Details": "This pilot is concussed. Things are spinny.",
						"Icon": "knockout"
					},
					"nature": "Buff",
					"statisticData": {
						"statName": "UnsteadyThreshold",
						"operation": "Float_Multiply",
						"modValue": "0.75",
						"modType": "System.Single"
					}
				}
			]
		},
```

Of note, `couldBeThermal` is used to determine if this injury can occur due to overheating or knockdown (it wouldn't make sense to have a broken arm from overheating, or to recieve severe burns from being knocked down, for example). `severity` is used in conjunction with both the `missionKillSeverityThreshold` and `cripplingSeverityThreshold`. Although injured pilots are no longer prevented from piloting mechs, particularly severe or repeated injuries to the same location can result in the pilot becoming incapacitated, `CRIPPLED`, and unable to pilot if the total `severity` of injuries in a given location exceeds the value set in `cripplingSeverityThreshold` (value < 1 disables crippling injuries). Similarly, a pilot will become incapacitated if the total `severity` of injuries <i>sustained in the current contract</i> exceeds the value set in `missionKillSeverityThreshold` (value < 1 disables this feature).

Other settings available follow:

```
{
"enableLogging" : true,
"enableLethalTorsoHead" : true,
"enableInternalDmgInjuries" : true,
"internalDmgStatName" : "InjureOnStructDmg",
"internalDmgInjuryLimit" : 1,
"internalDmgLvlReq" : 2.9,
"missionKillSeverityThreshold" : 4,
"cripplingSeverityThreshold" : 2,
"severityCost" : 360,
"injuryHealTimeMultiplier" : 2.5,	
"internalDmgInjuryLocs" : ["Head", "CenterTorso"],
"InjuryEffectsList": [],
"InternalDmgInjuries": []
    
```

`enableLogging` - bool, enables logging.

`enableLethalTorsoHead` - bool, if `true`, CRIPPLED Torso or Head is lethal.

`enableInternalDmgInjuries` - bool, if `true`, enables a feature that injures pilots when they recieve structure damage if certain equipment is mounted (i.e DNI or EI cockpits).

`internalDmgStatName` - name of bool statistic being used in gear to determine whether internal structure damage results in injuries. Example stat effect given below: 

```
{
            "durationData": {
                "duration": -1,
                "stackLimit": -1
            },
            "targetingData": {
                "effectTargetsCreator": true,
                "effectTriggerType": "Passive",
                "effectTargetType": "Creator"
            },
            "effectType": "StatisticEffect",
            "Description": {
                "Id": "DNI-Penalty",
                "Name": "InjureOnStructDmg",
                "Details": "Pilot will recieve injury when internal structure damage is sustained.",
                "Icon": "uixSvgIcon_equipment_Cockpit"
            },
            "nature": "Debuff",
            "statisticData": {
                "statName": "InjureOnStructDmg",
                "operation": "Set",
                "modValue": "true",
                "modType": "System.Boolean"
            }
        },
```

`internalDmgInjuryLimit` - int, defines the maximum number of injuries a pilot can recieve due to the above effect. Disabled if < 1.

`internalDmgLvlReq` - float, required single-point internal damage for pilot to be injured. e.g., if this was set to 50, LRMs would <i>never</i> inflict an injury.

`missionKillSeverityThreshold` - int, as discussed above defines the total `severity` of injuries required for a pilot to be incapacitated. Disabled if < 1.

`cripplingSeverityThreshold` - int, as discussed above defines the total `severity` of injuries in a single location required for a pilot to be `CRIPPLED`. Disabled if < 1.

`severityCost` - int, increases healing time required as a factor of severity

`injuryHealTimeMultiplier` - float, multiplier for vanilla healing time (`severityCost` is added after this multiplier)

`internalDmgInjuryLocs` - List<string>, internal damage must by in one of these ChassisLocations in order to inflict injuries from `enableInternalDmgInjuries`. If empty, all locations can inflict an injury.

`InjuryEffectsList` - List<Injury>, list of all possible injuries. All injury locations need to have an injury for each value of `couldBeThermal` represented, with the exception of `Head`. Overheating will never inflict a head injury, so `Head` does not need an Injury where `couldBeThermal :true`

`InternalDmgInjuries` - List<Injury>, list of all possible injuries from internal structure damage.
