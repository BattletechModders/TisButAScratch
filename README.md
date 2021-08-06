# TisButAScratch

#### Requires CustomComponents and CustomAmmoCategories

This mod overhauls the Battletech injury system, and lets modders apply different stat effects based on injuries a pilot receives. When a pilot receives an injury, a roll is made to determine both the location of the injury and the injury itself. Injuries are specific to a location; Valid `injuryLoc`s are `Head`, `ArmL`, `ArmR`, `Torso`, `LegL` and `LegR`. This mod is fully save-game compatible, and pilots with existing injuries will roll random injury effects on the first time the mod is loaded.

## Features included:

### <b>Injured Piloting</b>:
Pilots with injuries are (mostly) allowed to drop on contracts; however, they suffer the penalties their injuries entail. In addition, they are at greater risk of being `DEBILITATED` (see below).

Injuries can by checked be hovering over the red "injured" indicator in pilot portraits, both in the barracks roster list and the lance selection panel, or by hovering over the "injured" status bar in the pilot details panel of the barracks. e.g.:

<b>Pilot Portrait</b>

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/portraitstatus.png)

<b>Barracks</b>

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/barracksstatus.png)

### <b>Debilitating Injuries</b>:
If an injury severity in a single location exceeds a given threshold, a pilot may become `DEBILITATED` which incapacitates them for the current mission. Pilots that are `DEBILITATED` are unable to drop on contracts, even after their injuries have healed. `DEBILITATED` is a pilot tag, and can therefore be removed by events (or other actions that alter tags). A setting is provided that allows `DEBILITATED` to heal given enough time. An example event is included wherein the player can choose to pay for a "prosthesis" which removes `DEBILITATED`, allowing the pilot to be used in contracts, although they will still suffer the effects of the initial injury until that injury heals.

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/debil.png)

<b>NEW</b> - a line has been added to these tooltips to indicate the current severity of injuries and the threshold for becoming debilitated:

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/severity.png)

### <b>Mission Killed Injuries</b>:
If the total severity of injuries <i>regardless of location</i> exceeds a given threshold, a pilot can be Mission Killed, which incapacitates them for the current mission but does <i>not</i> prevent them from deploying on subsequent contracts. Think of it like "overcome by pain".

### <b>Bleeding Out Injuries</b>:
Certain injuries can be defined that inflict an informal <b>Bleeding Out</b> status. ~~These injuries have `durationData` defined that, when expired, render the pilot incapacitated and/or lethally injured (depending on settings)~~ <b>In versions 1.0.2.1 and higher, `durationData` for bleeding out has been replaced by calculations per-pilot based on Guts, Health, and other settings</b>. Pilots now have a calculated "BloodBank". If their BloodBank reaches 0 at the end of their activation, they die. BloodBank is decreased every round by the severity of the "Bleeding Out" injury, modified by `additiveBleedingFactor` for any additional "Bleeding Out" injuries after the first.

In addition, escalating penalties can be defined, inflicting debuffs in-mission on pilots the longer they bleed out. Lastly, semi-persistant debuffs can optionally be inflicted on pilots the longer they bleed out, e.g. -3 Piloting for 20 days.

The status effect gives an indicator of activations remain before the pilot bleeds out. End the mission or eject the pilot to avoid death/incapacitation.

![TextPop](https://github.com/ajkroeg/TisButAScratch/blob/main/doc/bleedingout.png)

### <b>Advanced Life Support Gear</b>:
Mech gear to influence injury effects:
```
	"statisticData": {
		"statName": "DisablesBleeding",
		"operation": "Set",
		"modValue": "true",
		"modType": "System.Boolean"
	}
```
Gear that sets `DisablesBleeding` to True will prevent the pilot from taking bleeding injuries.

```
	"statisticData": {
		"statName": "NullifiesInjuryEffects",
		"operation": "Set",
		"modValue": "true",
		"modType": "System.Boolean"
	}
```
Gear that alters `BleedingRateMulti` will decrease (if < 1) or increase (if >1) the rate at which pilots bleed out, e.g.:
```
	"statisticData": {
		"statName": "BleedingRateMulti",
		"operation": "Float_Multiply",
		"modValue": "0.25",
		"modType": "System.Single"
		"targetCollection": "Pilot"
	}
```

Gear that sets `NullifiesInjuryEffects` to True will prevent injuries from causing maluses in combat (and necessarily precludes taking bleeding injuries, although regular injuries will still be sustained).

### <b>Pain Shunt</b>:
Pilots with a specific tag will ignore injuries from overheating, component/ammo explosions, or neural feedback from DNI/EI cockpits.

### <b>Piloting Requirements</b>:
Units with certain component tags can require pilots have certain pilot tags in order to pilot those units. Intended to prevent just any old grunt from using a DNI/EI cockpit (pilot would need the "implant" tag), but could also be used to require "ECM Training" for a pilot to use a mech with advanced ECM gear, etc.

### <b>Increased Injury Heal Time</b>:
Injuries take longer to heal, defined in the settings below.

In vanilla, healing time is a function of the # of injuries, whether a pilot was incapactiated or had a "lethal injury", and pilot health. All things being equal, a pilot with health 3 heals slower than a pilot with health 4. This behavior is not changed. The formula for injury healing cost follows, with vanilla settings prefixed by `SimGameConstants`:

`([{SimGameConstants.BaseInjuryDamageCost / pilothealth] * injuryHealTimeMultiplier} + [severity * severityCost] + [debilitatedCost / {medtechDebilMultiplier * #medtech}]) / (SimGameConstants.DailyHealValue +  [SimGameConstants.MedTechSkillMod * #medtech])`

### Vehicle Pilot Injuries:

The setting `crewOrCockpitCustomID` defines components that when damaged/destroyed automatically injure pilots, including vehicle pilots.

The setting `injureVehiclePilotOnDestroy` controls vehicle pilot injury behavior when vehicles are destroyed. Possible values are "MAX", "HIGH", "SINGLE", "OFF". MAX functions the same as a CT destruction for mechs (max injuries = pilot <i>usually</i> dies.) "HIGH" is effectively MAX - 1; pilot will be severely injured but not dead. "SINGLE" inflicts a single injury on Vehicle destruction. "OFF" means no guaranteed injury on vehicle destruction (does not disable injuries from `crewOrCockpitCustomID` however).


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

`severity` - used in conjunction with both the below settings `missionKillSeverityThreshold` and `debilSeverityThreshold`. Although injured pilots are no longer prevented from piloting mechs, particularly severe or repeated injuries to the same location can result in the pilot becoming incapacitated, `DEBILITATED`, and unable to pilot if the total `severity` of injuries in a given location exceeds the value set in `debilSeverityThreshold` (value < 1 disables debilitating injuries). Similarly, a pilot will become incapacitated if the total `severity` of injuries <i>sustained in the current contract</i> exceeds their `missionKillSeverityThreshold`. The threshold set missionKillSeverityThreshold is the "default" threshold for all pilots. This value can be modified for individual pilots/units by altering the <i>Pilot</i> statistic "MissionKilledThreshold" via equipment, traits, etc.

`description` - human-legible description of this injury and its effects.

`effectDataJO` - list of status effects this injury applies. Importantly, `durationData` is used in conjunction with the status effect name suffix and `BleedingOutSuffix` setting below to note than an injury should inflict <b>Bleeding Out</b>, and either incapacitate or kill the pilot on expiration. 

## General Settings:

```
{
"enableLogging" : true,
"enableTrace" : true,
"enableLethalTorsoHead" : true,
"debilIncapacitates" : false,
"BleedingOutLethal" : false,
"BleedingOutSuffix" : "_bleedout",
"enableInternalDmgInjuries" : true,
"pilotPainShunt": "pilot_PainShunt",
"pilotingReqs": [
	{
		"ComponentTag": "PilotNeedsSpecialTag",
		"PilotTag": "commander_player",
		"PilotTagDisplay": "Level 5 Fancy Pants"
	}
],

"internalDmgStatName" : "InjureOnStructDmg",
"internalDmgInjuryLimit" : 1,
"internalDmgLvlReq" : 20,
"timeHealsAllWounds" : true,
"missionKillSeverityThreshold" : 6,
"reInjureWeightAppliesCurrentContract" : false,
"reInjureLocWeight" : 11,

"debilSeverityThreshold" : 3,
"severityCost" : 360,
"debilitatedCost" : 4320,
"medtechDebilMultiplier" : 0.5,
"injuryHealTimeMultiplier" : 5.0,

"crewOrCockpitCustomID": ["Cockpit", "CrewCompartment", "LifeSupportA", "LifeSupportB", "SensorsA", "SensorsB"],
"lifeSupportCustomID": ["LifeSupportA", "LifeSupportB"],
"OverheatInjuryStat": "InjureOnOverheat",
"isTorsoMountStatName": "isTorsoMount",
"lifeSupportSupportsLifeTM": true,

"internalDmgInjuryLocs" : ["Head", "CenterTorso"],
"InjuryEffectsList": [],
"InternalDmgInjuries": [],

"additiveBleedingFactor": 0.75,
"minBloodBank": 2,
"baseBloodBankAdd": 0,
"UseGutsForBloodBank": true,
"factorBloodBankMult": 1,
"UseBleedingEffects": true,
"BleedingEffects": [],
"UseSimBleedingEffects": true,
"SimBleedingEffects": []
```

`enableLogging` - bool, enables logging.

`enableTrace` - bool, enables trace logging (not usually needed).

`enableLethalTorsoHead` - bool, if `true`, debilitated Torso or Head is lethal.

`debilIncapacitates` - bool, if 'true', becoming debilitated will immediately incapacitate pilots during missions

`enableInternalDmgInjuries` - bool, if `true`, enables a feature that injures pilots when they recieve structure damage if certain equipment is mounted (i.e DNI or EI cockpits).

`pilotPainShunt` - string, pilot tag that denotes pilot has a "Pain Shunt", rendering them immune to injuries from ammo/component explosions, overheating, and neural feedback.

`pilotingReqs` - List of PilotingReqs which contain the following fields:

	`ComponentTag` - the tag on the component
	`PilotTag` - the tag a pilot must have in order to use a mech/vehicle with the above component mounted
	`PilotTagDisplay` - Human-legible display name for PilotTag (so players know what the pilot is missing when the popup happens)

`BleedingOutLethal` - bool, determines whether <b>Bleeding Out</b> from an injury is lethal (`true`) or merely incapacitates (`false`)

`BleedingOutSuffix` - string, ending string of <i>status effect Id, not the `injuryID`</i> to denote whether the injury should inflict <b>Bleeding Out</b> and incapacitate or kill the pilot on expiration (per `BleedingOutLethal`)

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

`reInjureWeightAppliesCurrentContract` - bool, determines whether additional likelihood of injuring a given pilot location (e.g, Left Leg) applies for injuries sustained in the current contract (true), or only for injuries sustained in previous contracts (false).

`reInjureLocWeight` - int, additional weight for currently injured locations to be more likely to be RE-injured. Effectively makes Debilitating Injuries more likely if you're already injured. Table for various values follows:

| Weight                                                                                                                                                                                                                              | Odds of Location Reinjury (x : 1 Odds) | % Chance of Location Reinjury |
|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------|-------------------------------|
| 0                                                                                                                                                                                                                                   | 0.166666667                            | 14.29                         |
| 1                                                                                                                                                                                                                                   | 0.333333333                            | 25.00                         |
| 2                                                                                                                                                                                                                                   | 0.5                                    | 33.33                         |
| 3                                                                                                                                                                                                                                   | 0.666666667                            | 40.00                         |
| 4                                                                                                                                                                                                                                   | 0.833333333                            | 45.45                         |
| 5                                                                                                                                                                                                                                   | 1                                      | 50.00                         |
| 6                                                                                                                                                                                                                                   | 1.166666667                            | 53.85                         |
| 7                                                                                                                                                                                                                                   | 1.333333333                            | 57.14                         |
| 8                                                                                                                                                                                                                                   | 1.5                                    | 60.00                         |
| 9                                                                                                                                                                                                                                   | 1.666666667                            | 62.50                         |
| 10                                                                                                                                                                                                                                  | 1.833333333                            | 64.71                         |
| 11                                                                                                                                                                                                                                  | 2                                      | 66.67                         |
| 12                                                                                                                                                                                                                                  | 2.166666667                            | 68.42                         |
| 13                                                                                                                                                                                                                                  | 2.333333333                            | 70.00                         |
| 14                                                                                                                                                                                                                                  | 2.5                                    | 71.43                         |
| 15                                                                                                                                                                                                                                  | 2.666666667                            | 72.73                         |
| 16                                                                                                                                                                                                                                  | 2.833333333                            | 73.91                         |
| 17                                                                                                                                                                                                                                  | 3                                      | 75.00                         |
| 18                                                                                                                                                                                                                                  | 3.166666667                            | 76.00                         |
| 19                                                                                                                                                                                                                                  | 3.333333333                            | 76.92                         |
| 20                                                                                                                                                                                                                                  | 3.5                                    | 77.78                         |
| 21                                                                                                                                                                                                                                  | 3.666666667                            | 78.57                         |
| 22                                                                                                                                                                                                                                  | 3.833333333                            | 79.31                         |
| 23                                                                                                                                                                                                                                  | 4                                      | 80.00                         |
| 24                                                                                                                                                                                                                                  | 4.166666667                            | 80.65                         |
| 25                                                                                                                                                                                                                                  | 4.333333333                            | 81.25                         |
| 26                                                                                                                                                                                                                                  | 4.5                                    | 81.82                         |
| 27                                                                                                                                                                                                                                  | 4.666666667                            | 82.35                         |
| 28                                                                                                                                                                                                                                  | 4.833333333                            | 82.86                         |
| 29                                                                                                                                                                                                                                  | 5                                      | 83.33                         |

`debilSeverityThreshold` - int, as discussed above defines the total `severity` of injuries in a single location required for a pilot to be `DEBILITATED`. Disabled if < 1.

`severityCost` - int, increases healing time required as a factor of severity

`debilitatedCost` - int, increases healing time required as a factor of pilot having `DEBILITATED` tag

`medtechDebilMultiplier` - float, multiplier for medtech skill divisor of `crippledCost`. E.g. for `debiledCost = 2000`,  `MedTechSkill = 10`, and `medtechDebilMultiplier = 0.5`, injury healing cost would be `2000/ (10 * .5)`

`injuryHealTimeMultiplier` - float, multiplier for vanilla healing time (`severityCost` and `debiledCost` are added after this multiplier)

`crewOrCockpitCustomID` - List<string> - list of CustomId (from CustomComponents) that defines "cockpit components" which will inflict injuries on critical hits. Only for vehicles and non-head-mounted cockpits (in the case of head-mounted cockpits, injuries are inflicted by normal "head hit" system).

`injureVehiclePilotOnDestroy` - string.  Controls vehicle pilot injury behavior when vehicles are destroyed. Possible values are "MAX", "HIGH", "SINGLE", "OFF". MAX functions the same as a CT destruction for mechs (max injuries = pilot <i>usually</i> dies.) "HIGH" is effectively MAX - 1; pilot will be severely injured but not dead. "SINGLE" inflicts a single injury on Vehicle destruction. "OFF" means no guaranteed injury on vehicle destruction (does not disable injuries from `crewOrCockpitCustomID` however).

`lifeSupportCustomID` - List<string> - list of CustomId (from CustomComponents) that defines specific life support components which, if a torso-mounted cockpit is used and `"lifeSupportSupportsLifeTM": true` will cause an injury or pilot death when life support is critted or destroyed, respectively.

`OverheatInjuryStat` - string. name of bool statistic (on AbstractActor statcollection) set by critical effects, etc to note that a pilot should take an injury immediately upon overheating. primarily used by damaged life support, etc. similar setup as below.
	
`isTorsoMountStatName` - string, name of bool statistic being used in gear to determine whether a torso-mounted cockpit is being used. Example stat effect added to torso-mount cockpit component below:

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
                "Id": "Torso-Mounted",
                "Name": "isTorsoMount",
                "Details": "Torso mounted cockpit, no injuries on head hits.",
                "Icon": "uixSvgIcon_equipment_Cockpit"
            },
            "nature": "Debuff",
            "statisticData": {
                "statName": "isTorsoMount",
                "operation": "Set",
                "modValue": "true",
                "modType": "System.Boolean"
            }
        }
```

`lifeSupportSupportsLifeTM` - bool, determines whether damage and/or destruction of life-support (as defined in `lifeSupportCustomID`) causes injuries/death when torso-mounted cockpit is being used.

`internalDmgInjuryLocs` - List<string>, internal damage must be in one of these ChassisLocations in order to inflict injuries from `enableInternalDmgInjuries`. If empty, all locations can inflict an injury.

`InjuryEffectsList` - List<Injury>, list of all possible injuries. All injury locations need to have an injury for each value of `couldBeThermal` represented, with the exception of `Head`. Overheating will never inflict a head injury, so `Head` does not need an Injury where `couldBeThermal :true`

`InternalDmgInjuries` - List<Injury>, list of all possible injuries from internal structure damage.

`additiveBleedingFactor` - float; if < 0, gets rounded to a whole number. this value is then subtracted from the timer of any preexisting bleeding injuries. if the value is between 0 and 1, then the timer of preexisting bleeding injuries is <i>multiplied</i> by this value. In all cases, the resulting "bleedout timer" will not become <1, ensuring players still have a chance to eject.

`minBloodBank` - int, defines a minimum "BloodBank" for pilots regardless of Guts/Health calculations.

`baseBloodBankAdd` - int, defines a baseline addition to "BloodBank" on top of Guts/Health calculations.

`UseGutsForBloodBank` - bool, if True - Guts level is used in BloodBank calculation. if False, Health is used in BloodBank calculation.

`factorBloodBankMult` - float, multiplier for BloodBank calculation; if `UseGutsForBloodCap = True`, final BloodBank formula is `(Guts * factorBloodBankMult) + baseBloodBankAdd`. If `UseGutsForBloodCap = False`, final BloodBank formula is `(Health * factorBloodBankMult) + baseBloodBankAdd`

`UseBleedingEffects` - bool, if True enables escalating bleeding effects applied in the current contract (defined in `BleedingEffects` below).

`BleedingEffects` - List<BleedingEffect> - list of bleeding effects to be applied to pilots as they bleed out. In the BleedingEffect, the field `bleedingEffectLvl` defines the point at which that bleeding effect is applied, by converting the level to a % of the total number of bleeding effects defined in the settings + 1. If a pilot's % of their BloodBank goes below that thtreshold, the effect is applied.
	
For example, if 4 BleedingEffects are defined in settings, a BleedingEffect with `bleedingEffectLvl: 1` would equate to a BloodBank % threshold of 0.8 (or 80%). If a pilot has BloodBank of 6 out of a starting BloodBank of 8, the effect would be applied (Pilot BloodBank 75% <= 80%). If more than one BleedingEffect is defined with the same `bleedingEffectLvl`, one of the effects will be chosen at random to apply.
	
Bleeding effects have the following structure:

```
{
	"bleedingEffectID": "BleedingShock",
	"bleedingName": "Shock",
	"bleedingEffectLvl": 1,
	"description": "This pilot has suffered minor blood loss and has reduced accuracy.",
	"effectDataJO": [
		{
			"durationData": {},
			"targetingData": {
				"effectTriggerType": "Passive",
				"effectTargetType": "Creator",
				"showInStatusPanel": true
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "BleedShock",
				"Name": "Blood Loss: Shock",
				"Details": "This pilot has suffered minor blood loss and has gone into minor shock, reducing accuracy.",
				"Icon": "crackedskull"
			},
			"nature": "Buff",
			"statisticData": {
				"modType": "System.Single",
				"modValue": "5.0",
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
}
```	

`UseSimBleedingEffects` - bool, if True enables escalating bleeding effects that are applied on subsequent contracts for a specified period of time (defined in `SimBleedingEffects` below).

`SimBleedingEffects` - List<SimBleedingEffect> - list of bleeding effects to be applied to pilots during subsequent contract. SimBleedingEffects have the following structure:
	
```
{
	"simBleedingEffectID": "BleedGutsLvl1",
	"bleedingEffectLvl": 1,
	"simResultJO": [
		{
			"Scope": "MechWarrior",
			"Requirements": null,
			"AddedTags": {
				"tagSetSourceFile": "",
				"items": [
					"TBAS_SimBleed__Guts__-1"
				]
			},
			"RemovedTags": {
				"tagSetSourceFile": "",
				"items": []
			},
			"Stats": [],
			"Actions": [],
			"ForceEvents": null,
			"TemporaryResult": true,
			"ResultDuration": 10
		}
	]
},
```
Similar to `BleedingEffects`, SimBleedingEffects use `bleedingEffectLvl` to define if/when the effects are to be applied to a pilot that is/has been bleeding out, int the same manner. The remainder of the structure is identical to a `SimGameEventResult` that gives the pilot a specific temporary tag. The format of the tag (when present) is then parsed at the start of combat to generate certain effects. These tags are of the basic structure `TBAS_SimBleed__StatName__Amount`. In the above example, a Pilots' Guts statistic would be reduced by 1 at the start of the contract. Temporarily reducing skills directly in SimGame would efffectively allow players to purchase additional levels of the skill at discounted XP costs, which is why we are parsing a temporary tag instead; the tags are processed at contract start rather than directly through the `SimGameEventResult` to prevent "easy" leveling up. Of course the downside is that these stat mods are limited to statistics present on Pilots rather than AbstractActors (e.g. skill stats, but not things like `AccuracyModifier`), and the only available operation on the stat is Adding or Subtracting (if Amount is negative).
