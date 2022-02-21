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

However, please note that the following restrictions will apply as we are using a mechanism to replace the official additional roles.<br>

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

However, The numeric keypad is not supported.<br>

## Roles

### Roles correspondence chart
| Name of additional role | Decision |
| ---- | ---- |
| Jester | Crewmate |
| Madmate | Engineer |
| MadGuardian | Crewmate |
| Bait | Crewmate |
| Terrorist | Engineer |
| Mafia | ShapeShifter |
| Vampire | Impostor |
| SabotageMaster | Crewmate |
| Mayor | Crewmate |
| Opportunist | Crewmate |
| Snitch | Crewmate |
| Sheriff | Impostor(Only host is the Crewmate) |
| BountyHunter | Impostor |

### Jester

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Voted Out<br>

They are the neutral role that becomes the sole winner when they are banished by vote.<br>
If the game ends without being banished, or if they are killed, they are defeated.<br>

### Madmate

Team : Impostor<br>
Decision : Engineer<br>

Belongs to the Impostor team, but Madmate does not know who the Impostor is.<br>
Impostors also doesn't know who Madmate is.<br>
They can not kill or sabotage, but they can enter the vent.<br>

(There are special settings for them.)<br>

### MadGuardian

Team : Impostor<br>
Decision : Crewmate<br>

Belongs to the Impostor team, but MadGuardian does not know who the Impostor is.<br>
Impostors also doesn't know who MadGuardian is.<br>
However, if they complete all of their own tasks, they will no longer be killed.<br>
They can not kill, sabotage, and to enter the vent.<br>

### Bait

Team : Crewmates<br>
Decision : Crewmate<br>

When they are killed, they can force the person who killed them to report their corpse.<br>

### Terrorist

Team : Neutral<br>
Decision : Engineer<br>
Victory Conditions : Finish All Tasks, Then Die<br>

They are the neutral role where they win the game alone if they die with all their tasks completed.<br>
Any cause of death is acceptable.<br>
If they die without completing their tasks, or if the game ends without they dying, they lose.<br>

### Mafia

Team : Impostor<br>
Decision : Shapeshifter<br>

Can vent, sabotage, and transform initially, but can not kill.<br>
If all of the Impostors who are not Mafias die, the Mafia will be able to kill.<br>
If the Mafia is not killable, they will still have a kill button, but they can not be killable.<br>
They can continue to transform even after the kill is enabled.<br>

### Vampire

Team : Impostor<br>
Decision : Impostor<br>

They are the role where the kill actually occurs 10 seconds after they press the kill button.<br>
Teleportation does not occur when a kill is made.<br>
Also, if a meeting starts before 10 seconds have passed since they pressed the kill button, the kill will occur at that moment.<br>
However, only if they kill Bait will it be a normal kill and they will be forced to report it.<br>

(There are special settings for them.)<br>

### SabotageMaster

Team : Crewmates<br>
Decision : Crewmate<br>

Reactors meltdown, oxygen disturbance and MIRA HQ's communication disturbance can both be fixed by repairing one of them.<br>
Power failures can all be fixed by touching a single lever.<br>
Opening a door in Polus or The Airship will open all the doors in that room.<br>

(There are special settings for them.)<br>

### Mayor

Team : Crewmates<br>
Decision : Crewmate<br>

They have more than one vote and can put them together into one person or skip.<br>

(There are special settings for them.)<br>

### Opportunist

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Aliving when one of the teams wins<br>

This is the neutral position, with an additional win if thay are still alive at the end of the game.<br>
They don't have tasks.<br>

### Snitch

Team : Crewmates<br>
Decision : Crewmate<br>

When they completes a task, the name of the evildoer will change to red.<br>
However, when their tasks becomes low, their name will appear to change from the evildoer.<br>

### Sheriff

Team : Crewmates<br>
Decision : Crewmate(Only host is the Crewmate)<br>

They can kill the evildoers.<br>
However, if they kill the Crewmate, they will die.<br>
They don't have tasks.<br>

(There are special settings for them.)

### BountyHunter

Team : Impostor<br>
Decision : Impostor<br>

When they first tries to make a kill, a target will be chosen.<br>
Killing the indicated target will halve the next killcool.<br>
If they kill someone who is not their target, they will still keep their kill rule.<br>

### Witch

Team : Impostor<br>
Decision : Impostor<br>

When Meeting has ended, spelled player will be killed<br>
## Mode

### HideAndSeek

#Crewmates Team (Blue) Victory Conditions<br>
Completing all tasks.<br>
※Ghosts's tasks are not counted.<br>

#Impostor Team (Red) Victory Conditions<br>
Killing all Crewmates.<br>
※Even if there are equal numbers of Crewmates and Impostors, the match will not end unless all the Crewmates have been wiped out.<br>

#Fox (Purple) Victory Conditions<br>
Aliving when one of the teams (Except Troll) wins.<br>

#Troll (Green) Victory Conditions<br>
Being killed by Impostors.<br>

#Prohibited items<br>
・Sabotage<br>
・Admin<br>
・Camera<br>
・The act of a ghosts giving its location to a survivor<br>
・Ambush (This may make it impossible for the Crewmates to win with the tasks.)<br>

#What you can't do<br>
・Reporting a dead bodies<br>
・Emergency conference button<br>
・Sabotage<br>

(There are special settings for them.)<br>

### NoGameEnd

#Crewmates Team Victory Conditions<br>
None<br>

#Impostor Team Victory Conditions<br>
None<br>

#Prohibited items<br>
None<br>

#What you can't do<br>
Exiting the game with anything other than host's SHIFT+L+Enter.<br>

This is a debug mode where there is no win decision.<br>

### SyncButtonMode

This is the mode in which all players' button counts are synchronised.<br>

(There are special settings for them.)<br>

## Credits

Bait, Vampire roles and more tips to modding : https://github.com/Eisbison/TheOtherRoles<br>
Opportunist role : https://github.com/yukinogatari/TheOtherRoles-GM<br>
Jester and Madmate roles : https://au.libhalt.net<br>
Terrorist(Trickstar + Joker) : https://github.com/MengTube/Foolers-Mod<br>

Twitter : https://twitter.com/XenonBottle<br>

Translated with https://www.deepl.com<br>
