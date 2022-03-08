# Town Of Host
[![TownOfHost-Title](./Images/TownOfHost-Title.png)](https://youtu.be/IGguGyq_F-c)

このREADMEは英語版です。<br>
! My English isn't very good, so if this readme is wrong, please use Google Translator to Japanese readme. !<br>

## Regarding this mod
This mod is unofficial, and Innersloth, the developer of Among Us, has no involvement in the development of this mod.<br>
Please do not contact the official team regarding any issues with this mod.<br>

[![Discord](./Images/TownOfHost-Discord.png)](https://discord.gg/v8SFfdebpz)

## Features
This mod only needs to be installed on the host's client to work, and works regardless of whether or not other client mods have been installed, and regardless of the type of terminal.<br>
Unlike mods that use custom servers, there is no need to add servers by editing URLs or files.<br>

However, please note that the following restrictions apply.<br>

- If the host changes due to factors such as a host leaving in the middle of a session, the processing related to the additional role may not work properly.
- If a special role is used, the settings for that special role will be rewritten. (Example : Remove cooldown for vent, etc.)

Note that if a player other than the host plays with this mod installed, the following changes will be made.<br>

- Display of the special role's own start screen.
- Display of the normal victory screen for the special role.
- Add additional settings.
- etc.

## Custom Settings Menu
Pressing the Tab key in the standby lobby will change the room setting screen to a setting screen dedicated to Town Of Host.<br>

| Key | Action |
| :---: | ---- |
| Tab | Open/Close Custom Settings Menu |
| Up | Corsor Up |
| Down | Cursor Down |
| Right | Execute Item |
| Left | Go Back One |
| Number | Enter A Value |

## Roles

| Impostors | Crewmates | Neutral |
|----------|-------------|-----------------|
| [BountyHunter](###BountyHunter) | [Bait](###Bait) | [Jester](###Jester) |
| [Mafia](###Mafia) | [Mayor](###Mayor) | [Opportunist](###Opportunist) |
| [Vampire](###Vampire) | [SabotageMaster](###SabotageMaster) | [Terrorist](###Terrorist) |
| [Witch](###Witch) | [Sheriff](###Sheriff) |  |
| [Madmate](###Madmate) | [Snitch](###Snitch) |  |
| [MadGuardian](###MadGuardian) |  |  |

### BountyHunter

Team : Impostors<br>
Decision : Impostor<br>

If the BountyHunters kill the indicated target, their next kill cooldown will be halved.<br>
If they kill the player who is not their target, they will keep their next kill cooldown.<br>

### Mafia

Team : Impostors<br>
Decision : Shapeshifter<br>

The Mafias can initially use vents, sabotage, and transformations, but they can not kill.<br>
Once all Impostors who are not them are dead, they will be able to kill.<br>
If they can not kill, they will still have a kill button, but they can not kill.<br>
They can transform after becoming to able to kill.<br>

### Vampire

Team : Impostors<br>
Decision : Impostor<br>

The Vampires are the role where the kill actually occurs after a certain amount of time has passed since the kill button was pressed.<br>
There is no teleportation when a kill is made.<br>
Also, if a meeting starts before the set time has elapsed after the kill button is pressed, the kill will occur at that moment.<br>
However, only if they kill the [Baits](###Bait) will it be a normal kill and they will be forced to report it.<br>

#### Settings

| Settings Name |
|----------|
| Vampire Kill Delay(s) |

### Witch

Team : Impostors<br>
Decision : Impostor<br>

Pressing the kill button toggles between kill mode and spell mode, and pressing the kill button while in spell mode allows them to cast a spell on the target.<br>
The target will be given a special mark at the meeting and will die if the Witches cannot be banished during the meeting.<br>

### Madmate

Team : Impostors<br>
Decision : Engineer<br>

The Madmates belong to the Imposters team, but they do not know who the Imposters are.<br>
The Impostors do not know who the they are too.<br>
They can not kill or sabotage, but they can use vents.<br>

#### Settings

| Settings Name |
|----------|
| Madmate([MadGuardian](###MadGuardian)) Can Fix Lights Out |

### MadGuardian

Team : Impostors<br>
Decision : Crewmate<br>

The MadGuardians belong to the Imposters team, but they do not know who the Imposters are.<br>
The Impostors do not know who the they are too.<br>
However, if they complete all of their own tasks, they will not be killed.<br>
They can not kill, sabotage, and using vents.<br>

#### Settings

| Settings Name |
|----------|
| [Madmate](###Madmate)(MadGuardian) Can Fix Lights Out |
| MadGuardian Can See Own Cracked Barrier |

### Bait

Team : Crewmates<br>
Decision : Crewmate<br>

When the Baits are killed, they can force the player who killed them to report their bodies.<br>

### Mayor

Team : Crewmates<br>
Decision : Crewmate<br>

The Mayors have multiple votes, which can be grouped together and put into a single player or skip.<br>

#### Settings

| Settings Name |
|----------|
| Mayor Additional Votes Count |

### SabotageMaster

Team : Crewmates<br>
Decision : Crewmate<br>

The SabotageMasters can fix sabotage faster.<br>
Communications in MIRA HQ, reactor and O2 can both be fixed by fixing one of them.<br>
Lights can be fixed by touching a single lever.<br>
Opening a door in Polus or The Airship will open all the doors in that room.<br>

#### Settings

| Settings Name |
|----------|
| SabotageMaster Fixes Sabotage Limit(Ignore Closing Doors) |
| SabotageMaster Can Fixes Multiple Doors |
| SabotageMaster Can Fixes Both Reactors |
| SabotageMaster Can Fixes Both O2 |
| SabotageMaster Can Fixes Both Communications In MIRA HQ |
| SabotageMaster Can Fixes Lights Out All At Once |

### Sheriff

Team : Crewmates<br>
Decision : Crewmate(Only host is the Crewmate)<br>

The Sheriffs can kill Impostors.<br>
However, if they kill the Crewmates, they will die.<br>
They do not have tasks.<br>

#### Settings

| Settings Name |
|----------|
| Sheriff Can Kill [Jester](###Jester) |
| Sheriff Can Kill [Terrorist](###Terrorist) |
| Sheriff Can Kill [Opportunist](###Opportunist) |

### Snitch

Team : Crewmates<br>
Decision : Crewmate<br>

When the Snitches complete their tasks, the name of the Impostors will change to red.<br>
However, when the number of their tasks are low, it will be notified to the Impostors.<br>

### Jester

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Get Voted Out<br>

The Jesters are  the neutral role which can win by getting voted out.<br>
If the game ends without getting voted out., or if they are killed, they lose.<br>

### Opportunist

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Aliving when one of the teams wins<br>

The Opportunists are the Neutral role, with an additional win if thay are still alive at the end of the game.<br>
They do not have tasks.<br>

### Terrorist

Team : Neutral<br>
Decision : Engineer<br>
Victory Conditions : Finish All Tasks, Then Die<br>

They are the Neutral role where they win the game alone if they die with all their tasks completed.<br>
Any cause of death is acceptable.<br>
If they die without completing their tasks, or if the game ends without they dying, they lose.<br>

## Mode

### DisableTasks

It is possible to disable certain tasks.<br>

#### Settings

| Settings Name |
|----------|
| Disable StartReactor Tasks |
| Disable SubmitScan Tasks |
| Disable SwipeCard Tasks |
| Disable UnlockSafe Tasks |
| Disable UploadData Tasks |

### HideAndSeek

#### Crewmates Team (Blue) Victory Conditions
Completing all tasks.<br>
※Ghosts's tasks are not counted.<br>

#### Impostor Team (Red) Victory Conditions
Killing all Crewmates.<br>
※Even if there are equal numbers of Crewmates and Impostors, the match will not end unless all the Crewmates have been wiped out.<br>

#### Fox (Purple) Victory Conditions
Aliving when one of the teams (Except Troll) wins.<br>

#### Troll (Green) Victory Conditions
Being killed by Impostors.<br>

#### Prohibited items
・Sabotage<br>
・Admin<br>
・Camera<br>
・The act of a ghosts giving its location to a survivor<br>
・Ambush (This may make it impossible for the Crewmates to win with the tasks.)<br>

#### What you can't do
・Reporting a dead bodies<br>
・Emergency conference button<br>
・Sabotage<br>

#### Settings

| Settings Name |
|----------|
| Allow Closing Doors |
| Impostors Waiting Time(s) |
| Ignore Cosmetics |
| Ignore Using Vents |

### NoGameEnd

#### Crewmates Team Victory Conditions
None<br>

#### Impostor Team Victory Conditions
None<br>

#### Prohibited items
None<br>

#### What you can't do
Exiting the game with anything other than host's SHIFT+L+Enter.<br>

This is a debug mode where there is no win decision.<br>

### RandomMapsMode

The RandomMapsMode changes the maps at random.<br>

#### Settings

| Settings Name |
|----------|
| Added The Skeld |
| Added MIRA HQ |
| Added Polus |
| Added The Airship |

### SyncButtonMode

This is the mode in which all players' button counts are synchronised.<br>

#### Settings

| Settings Name |
|----------|
| Max Button Count |

## OtherSettings

| Settings Name |
|----------|
| When Skip Vote |
| When Non-Vote |

## Credits

[BountyHunter](###BountyHunter),[Mafia](###Mafia),[Vampire](###Vampire),[Witch](###Witch),[Bait](###Bait),[Mayor](###Mayor),[Sheriff](###Sheriff),[Snitch](###Snitch) roles and more tips to modding : https://github.com/Eisbison/TheOtherRoles<br>
[Opportunist](###Opportunist) role : https://github.com/yukinogatari/TheOtherRoles-GM<br>
[Jester](###Jester) and [Madmate](###Madmate) roles : https://au.libhalt.net<br>
[Terrorist](###Terrorist)(Trickstar + Joker) : https://github.com/MengTube/Foolers-Mod<br>

Twitter : https://twitter.com/XenonBottle<br>

Translated with https://www.deepl.com<br>
