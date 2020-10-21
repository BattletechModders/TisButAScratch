# TisButAScratch

This mod overhauls the Battletech injury system, and lets modders apply different stat effects based on injuries a pilot receives. When a pilot receives an injury, a roll is made to determine both the location of the injury and the injury itself. Injuries are specific to a location; Valid `injuryLoc`s are `Head`, `ArmL`, `ArmR`, `Torso`, `LegL` and `LegR`. This mod is fully save-game compatible, and pilots with existing injuries will roll random injury effects on the first time the mod is loaded.

## Features included:

<b>Injured Piloting</b>: Pilots with injuries are (mostly) allowed to drop on contracts; however, they suffer the penalties their injuries entail. In addition, they are at greater risk of being `DEBILITATED` (see below).

Injuries can by checked be hovering over the red "injured" indicator in pilot portraits, both in the barracks roster list and the lance selection panel, or by hovering over the "injured" status bar in the pilot details panel of the barracks. e.g.:

<b>Pilot Portrait</b>

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/portraitstatus.png)

<b>Barracks</b>

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/barracksstatus.png)

<b>Debilitating Injuries</b>: if an injury severity in a single location exceeds a given threshold, a pilot may become `DEBILITATED` which incapacitates them for the current mission. Pilots that are `DEBILITATED` are unable to drop on contracts, even after their injuries have healed. `DEBILITATED` is a pilot tag, and can therefore be removed by events (or other actions that alter tags). A setting is provided that allows `DEBILITATED` to heal given enough time. An example event is included wherein the player can choose to pay for a "prosthesis" which removes `DEBILITATED`, allowing the pilot to be used in contracts, although they will still suffer the effects of the initial injury until that injury heals.

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/debil.png)

<b>NEW</b> - a line has been added to these tooltips to indicate the current severity of injuries and the threshold for becoming debilitated:

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/severity.png)

<b>Mission Killed Injuries</b>: If the total severity of injuries <i>regardless of location</i> exceeds a given threshold, a pilot can be Mission Killed, which incapacitates them for the current mission but does <i>not</i> prevent them from deploying on subsequent contracts. Think of it like "overcome by pain".

<b>Bleeding Out Injuries</b>: Certain injuries can be defined that inflict an informal <b>Bleeding Out</b> status. These injuries have `durationData` defined that, when expired, render the pilot incapacitated and/or lethally injured (depending on settings). The status effect gives an indicator of many rounds, activations, etc. remain before the pilot bleeds out. End the mission or eject the pilot to avoid death/incapacitation.

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/bleedingout.png)


<b>Increased Injury Heal Time</b>: Injuries take longer to heal, defined in the settings below.

Injuries are defined in the settings.json, and have the following structure:
```
"InjuryEffectsList": [
		{
			"injuryID" : "ArmLBrokenCompound",
			"injuryID_Post" : "ArmLBroken",
			"injuryName" : "Compound Fracture - Left Arm",
			"injuryLoc" : "ArmL",
			"couldBeThermal" : false,
			"severity" : 1,
			"description" : "This pilot has a compound fracture in their arm, and is suffering an accuracy penalty.",
			"effectDataJO" : [
				{
					"durationData": {
						"duration": 4,
						"ticksOnActivations": true,
						"useActivationsOfTarget": true,
						"stackLimit": 1
					},
					"targetingData": {
						"effectTriggerType": "Passive",
						"effectTargetType": "Creator",
						"showInStatusPanel": true
					},
					"effectType": "StatisticEffect",
					"Description": {
						"Id": "ArmLCmpdBroken_bleedout",
						"Name": "Broken Arm, Compound - Left",
						"Details": "This pilot has a compound fracture in their arm, and is suffering an accuracy penalty.",
						"Icon": "brokenarm"
					},
					"nature": "Buff",
					"statisticData": {
						"modType": "System.Single",
						"modValue": "7.0",
						"operation": "Float_Add",
						"statName": "AccuracyModifier",
						"targetAmmoCategory": "NotSet",
						"targetCollection": "Weapon",
						"targetWeaponCategory": "NotSet",
						"targetWeaponSubType": "NotSet",
						"targetWeaponType": "NotSet"
					}
				}
			]
		},
```

`injuryID` -  the unique ID of this injury.

`injuryName` - the human-legible name of this injury.

`injuryID_Post` - optionally defines the injuryID of an injury that will <i>replace</i> this injury after combat has ended; required for injuries that inflict <b>Bleeding Out</b>.

`injuryLoc` - the location of the injury. Valid `injuryLoc`s are `Head`, `ArmL`, `ArmR`, `Torso`, `LegL` and `LegR`

`couldBeThermal` - used to determine if this injury can occur due to overheating or knockdown (it wouldn't make sense to have a broken arm from overheating, or to recieve severe burns from being knocked down, for example). <b>IMPORTANT:</b> There needs to be at least one injury defined for both every location and value of `couldBeThermal`.

`severity` - used in conjunction with both the below settings `missionKillSeverityThreshold` and `cripplingSeverityThreshold`. Although injured pilots are no longer prevented from piloting mechs, particularly severe or repeated injuries to the same location can result in the pilot becoming incapacitated, `DEBILITATED`, and unable to pilot if the total `severity` of injuries in a given location exceeds the value set in `debilSeverityThreshold` (value < 1 disables debilitating injuries). Similarly, a pilot will become incapacitated if the total `severity` of injuries <i>sustained in the current contract</i> exceeds the value set in `missionKillSeverityThreshold` (value < 1 disables this feature).

`description` - human-legible description of this injury and its effects.

`effectDataJO` - list of status effects this injury applies. Importantly, `durationData` is used in conjunction with the status effect name suffix and `BleedingOutSuffix` setting below to note than an injury should inflict <b>Bleeding Out</b>, and either incapacitate or kill the pilot on expiration. 

## General Settings:

```
{
"enableLogging" : true,
"enableLethalTorsoHead" : true,
"debilIncapacitates" : false,
"BleedingOutLethal" : false,
"BleedingOutSuffix" : "_bleedout",
"BleedingOutTimerString" : "rounds",
"enableInternalDmgInjuries" : true,
"internalDmgStatName" : "InjureOnStructDmg",
"internalDmgInjuryLimit" : 1,
"internalDmgLvlReq" : 20,
"timeHealsAllWounds" : true,
"missionKillSeverityThreshold" : 6,
"debilSeverityThreshold" : 3,
"severityCost" : 360,
"debilitatedCost" : 4320,
"medtechDebilMultiplier" : 0.5,
"injuryHealTimeMultiplier" : 5.0,	
"internalDmgInjuryLocs" : ["Head", "CenterTorso"],
"InjuryEffectsList": [],
"InternalDmgInjuries": []
 
```

`enableLogging` - bool, enables logging.

`enableLethalTorsoHead` - bool, if `true`, CRIPPLED Torso or Head is lethal.

`debilIncapacitates` - bool, if 'true', becoming debilitated will immediately incapacitate pilots during missions

`enableInternalDmgInjuries` - bool, if `true`, enables a feature that injures pilots when they recieve structure damage if certain equipment is mounted (i.e DNI or EI cockpits).

`BleedingOutLethal` - bool, determines whether <b>Bleeding Out</b> from an injury is lethal (`true`) or merely incapacitates (`false`)

`BleedingOutSuffix` - string, ending string of <i>status effect Id, not the `injuryID`</i> to denote whether the injury should inflict <b>Bleeding Out</b> and incapacitate or kill the pilot on expiration (per `BleedingOutLethal`)

`BleedingOutTimerString` - string, what word to use in <b>Bleeding Out</b> status tooltip; e.g., if durationData for the injuryeffect uses `ticksOnActivations`, you may want to set this string to `"activations"`, as the tooltip would say "Unit is bleeding out, 4 `activations` remaining!".

`internalDmgStatName` - name of bool statistic being used in gear to determine whether internal structure damage results in injuries.
Example stat effect added to DNI cockpit given below: 

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

`internalDmgLvlReq` - float, required single-point internal damage for pilot to be injured. e.g., if this was set to 20, LRMs would <i>never</i> inflict an injury; even though the total damage of the salvo might by >20, no single missile inflicts >20 damage.

`timeHealsAllWounds` - bool, if true debilitating injuries will heal with time. if false, pilots will remain `DEBILITATED` until the tag is removed via event.

`missionKillSeverityThreshold` - int, as discussed above defines the total `severity` of injuries required for a pilot to be incapacitated. Disabled if < 1.

`debilSeverityThreshold` - int, as discussed above defines the total `severity` of injuries in a single location required for a pilot to be `DEBILITATED`. Disabled if < 1.

`severityCost` - int, increases healing time required as a factor of severity

`debilitatedCost` - int, increases healing time required as a factor of pilot having `DEBILITATED` tag

`medtechDebilMultiplier` - float, multiplier for medtech skill divisor of `crippledCost`. E.g. for `debiledCost = 2000`,  `MedTechSkill = 10`, and `medtechDebilMultiplier = 0.5`, injury healing cost would be `2000/ (10 * .5)`

`injuryHealTimeMultiplier` - float, multiplier for vanilla healing time (`severityCost` and `debiledCost` are added after this multiplier)

`internalDmgInjuryLocs` - List<string>, internal damage must be in one of these ChassisLocations in order to inflict injuries from `enableInternalDmgInjuries`. If empty, all locations can inflict an injury.

`InjuryEffectsList` - List<Injury>, list of all possible injuries. All injury locations need to have an injury for each value of `couldBeThermal` represented, with the exception of `Head`. Overheating will never inflict a head injury, so `Head` does not need an Injury where `couldBeThermal :true`

`InternalDmgInjuries` - List<Injury>, list of all possible injuries from internal structure damage.
	
A note on injury healing time: in vanilla, healing time is a function of the # of injuries, whether a pilot was incapactiated or had a "lethal injury", and pilot health. All things being equal, a pilot with health 3 heals slower than a pilot with health 4. This behavior is not changed. The formula for injury healing cost follows, with vanilla settings prefixed by `SimGameConstants`:

`(SimGameConstants.BaseInjuryDamageCost / pilothealth / [SimGameConstants.DailyHealValue +  {SimGameConstants.MedTechSkillMod * #medtech}]) * injuryHealTimeMultiplier + (severity * severityCost) + (debilitatedCost / [medtechDebilMultiplier * #medtech])`
