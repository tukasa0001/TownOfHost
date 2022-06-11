# Town Of Host

[![TownOfHost-Title](./Images/TownOfHost-Title.png)](https://youtu.be/IGguGyq_F-c)

<p align="center"><a href="https://github.com/tukasa0001/TownOfHost/releases/"><img src="https://badgen.net/github/release/tukasa0001/TownOfHost"></a></p>

この README は英語版です。<br>
! My English isn't very good, so if this readme is wrong, please use Google Translator to Japanese readme. !<br>

## Regarding this mod

This mod is unofficial, and Innersloth, the developer of Among Us, has no involvement in the development of this mod.<br>
Please do not contact the official team regarding any issues with this mod.<br>

[![Discord](./Images/TownOfHost-Discord.png)](https://discord.gg/W5ug6hXB9V)

## Releases

**Latest Version: [Here](https://github.com/tukasa0001/TownOfHost/releases/latest)**

Old Versions: [Here](https://github.com/tukasa0001/TownOfHost/releases)

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

## Features
### Hotkeys

#### All Clients
| HotKey      | Function                                                               | Usable Scene |
| ----------- | ---------------------------------------------------------------------- | ------------ |
| `Tab`       | Option list page feed                                                  | Lobby        |
| `Ctrl`+`F1` | Output log to desktop                                                  | Anywhere     |
| `F11`       | Change resolution<br>480x270 → 640x360 → 800x450 → 1280x720 → 1600x900 | Anywhere     |
| `Ctrl`+`C`  | Copy the text                                                          | Chat         |
| `Ctrl`+`V`  | Paste the text                                                         | Chat         |
| `Ctrl`+`X`  | Cut the text                                                           | Chat         |

#### Host only
| HotKey              | Function                      | Usable Scene  |
| ------------------- | ----------------------------- | ------------- |
| `Shift`+`L`+`Enter` | Force End Game                | In Game       |
| `Shift`+`M`+`Enter` | Skip meeting to end           | In Game       |
| `Ctrl`+`N`          | Show active role descriptions | Lobby&In Game |
| `C`                 | Abort game start              | In Countdown  |
| `Shift`             | Start the game immediately    | In Countdown  |

### Chat Commands
Chat commands are commands that can be typed in chat.
| Command                                               | Function                                          |
| ----------------------------------------------------- | ------------------------------------------------- |
| /winner<br>/win                                       | Show winner                                       |
| /lastresult<br>/l                                     | Show game result                                  |
| /now<br>/n                                            | Show active settings                              |
| /rename <string><br>/r <string>                       | Change my name                                    |
| /dis <crewmate/impostor>                              | Ending the match as a Crewmate/Impostor severance |
| /template <tag><br>/t <tag>                           | Display the canned text corresponding to tag      |
| /messagewait <sec><br>/mw <sec>                       | Set message send interval                         |
| /help<br>/h                                           | Show command description                          |
| /help roles <role><br>/help r <role>                  | Display role description                          |
| /help attributes <attribute><br>/help att <attribute> | Show attribute description                        |
| /help modes <mode><br>/help m <mode>                  | Display mode description                          |
| /help now<br>/help n                                  | Show active setting descriptions                  |

## Roles

| Impostors                           | Crewmates                         | Neutral                           |
| ----------------------------------- | --------------------------------- | --------------------------------- |
| [BountyHunter](#BountyHunter)       | [Bait](#Bait)                     | [Arsonist](#Arsonist)             |
| [Evil Watcher](#Watcher)            | [Dictator](#Dictator)             | [Egoist](#Egoist)                 |
| [FireWorks](#FireWorks)             | [Doctor](#Doctor)                 | [Executioner](#Executioner)       |
| [Mare](#Mare)                       | [Lighter](#Lighter)               | [Jester](#Jester)                 |
| [Puppeteer](#Puppeteer)             | [Mayor](#Mayor)                   | [Lovers](#Lovers)                 |
| [SerialKiller](#SerialKiller)       | [Nice Watcher](#Watcher)          | [Opportunist](#Opportunist)       |
| [ShapeMaster](#ShapeMaster)         | [SabotageMaster](#SabotageMaster) | [Terrorist](#Terrorist)           |
| [Sniper](#Sniper)                   | [Sheriff](#Sheriff)               | [SchrodingerCat](#SchrodingerCat) |
| [TimeThief](#TimeThief)             | [Snitch](#Snitch)                 |                                   |
| [Vampire](#Vampire)                 | [SpeedBooster](#SpeedBooster)     |                                   |
| [Warlock](#Warlock)                 | [Trapper](#Trapper)               |                                   |
| [Witch](#Witch)                     |                                   |                                   |
| [Mafia](#Mafia)                     |                                   |                                   |
| [Madmate](#Madmate)                 |                                   |                                   |
| [MadGuardian](#MadGuardian)         |                                   |                                   |
| [MadSnitch](#MadSnitch)             |                                   |                                   |
| [SidekickMadmate](#SidekickMadmate) |                                   |                                   |


### BountyHunter

Team : Impostors<br>
Decision : Impostor<br>

If the BountyHunters kill the indicated target, their next kill cooldown will be halved.<br>
If they kill the player who is not their target, they will keep their next kill cooldown.<br>

#### Settings

| Settings Name                                                 |
| ------------------------------------------------------------- |
| Time to change target(s)                                      |
| Cooldown time after killing the target(s)                     |
| Cooldown time after killing anything other than the target(s) |
| Kill cooldown time other than BountyHunter(s)                 |

### FireWorks

Team : Impostors<br>
Decision : Shapeshifter<br>

The FireWorks can set off fireworks and kill a lot. <br>
You Install up to 3 fireworks at the timing of shape-shift.  <br>
After Install all the fireworks, you can set off all fireworks at once at the timing of the shape-shift when you becomes the last Imposter.  <br>
You can't kill until the fireworks are Installing and Set off fireworks. <br>
Even if you get caught up in fireworks, you win if you annihilate the enemy. <br>

#### Settings

| Settings Name       |
| ------------------- |
| FireWorks Max Count |
| FireWorks Radius    |

### Mare

Team : Impostor<br>
Decision : Impostor<br>

No kills can be made except in the event of a power outage.<br>
However, if the kill is successful, the KillCooldown is halved.<br>
And the movement speed will also increase. And name is displayed in red<br>

### Puppeteer

Team : Impostors<br>
Decision : Impostor<br>

The target of the kill is made to kill the next Crewmate that the target approaches.<br>
If the target is the one that is triggered at the moment the opponent is killed, the effect is reflected on the target.<br>
It is not possible to perform normal kills.<br>

### SerialKiller

Team : Impostor<br>
Decision : Shapeshifter<br>

SerialKiller's killcooldown is shorter than defalt Impostor.<br>
If he can not kill on deadline, he will kill him.<br>

| Settings Name            |
| ------------------------ |
| Kill cool down time(s)   |
| Time to self-destruct(s) |

### ShapeMaster

Team : Impostor<br>
Decision : ShapeShifter<br>

Shape Master ignores the cooldown after a transformation and can transform again.<br>
Normally, the transformation lasts only 10 seconds, but the duration of the transformation can be changed through settings.<br>

#### Settings

| Settings Name         |
| --------------------- |
| Transformable time(s) |

### Sniper

Team : Impostors<br>
Decision : Shapeshifter<br>

Sniper can long-range shooting. <br>
Kills targets that are on the extension of the shape-shifted point to the released point. <br>
The crew on the line of sight will be notified of the shooting sound. <br>
You cannot normally kill until the bullet is cut off. <br>

Precision Shooting:OFF<BR>
![off](https://user-images.githubusercontent.com/96226646/172194283-5482db76-faab-4185-9898-ac741b132112.png)<br>
Precision Shooting:ON<BR>
![on](https://user-images.githubusercontent.com/96226646/172194317-6c47b711-a870-4ec0-9062-2abbf953418b.png)<br>

#### Settings

| Settings Name             |
| ------------------------- |
| Sniper Bullet Count       |
| Sniper Precision Shooting |

### TimeThief

Team : Impostors<br>
Decision : Impostor<br>

Killing a player decreases the meeting time.<br>
Also, when a TimeThief is expelled or killed, the lost meeting time is returned.<br>

#### Settings

| Settings Name                        |
| ------------------------------------ |
| TimeThief Decrease Meeting Time(s)   |
| TimeThief Lower Limit Voting Time(s) |

### Vampire

Team : Impostors<br>
Decision : Impostor<br>

The Vampires are the role where the kill actually occurs after a certain amount of time has passed since the kill button was pressed.<br>
There is no teleportation when a kill is made.<br>
Also, if a meeting starts before the set time has elapsed after the kill button is pressed, the kill will occur at that moment.<br>
However, only if they kill the [Baits](#Bait) will it be a normal kill and they will be forced to report it.<br>

#### Settings

| Settings Name         |
| --------------------- |
| Vampire Kill Delay(s) |

### Warlock

Team : Impostor<br>
Decision : Shapeshifter<br>

If warlock kills before shapeshifting, the target will be cursed.<br>
If he try to shapeshift again, the nearest crewmate will be killed<br>

### Witch

Team : Impostors<br>
Decision : Impostor<br>

Pressing the kill button toggles between kill mode and spell mode, and pressing the kill button while in spell mode allows them to cast a spell on the target.<br>
The target will be given a special mark at the meeting and will die if the Witches cannot be banished during the meeting.<br>

### Mafia

Team : Impostors<br>
Decision : Shapeshifter<br>

The Mafias can initially use vents, sabotage, and transformations, but they can not kill.<br>
Once all Impostors who are not them are dead, they will be able to kill.<br>
If they can not kill, they will still have a kill button, but they can not kill.<br>
They can transform after becoming to able to kill.<br>

### Madmate

Team : Impostors<br>
Decision : Engineer<br>

The Madmates belong to the Impostors team, but they do not know who the Impostors are.<br>
The Impostors do not know who the they are too.<br>
They can not kill or sabotage, but they can use vents.<br>

### MadGuardian

Team : Impostors<br>
Decision : Crewmate<br>

The MadGuardians belong to the Impostors team, but they do not know who the Impostors are.<br>
The Impostors do not know who the they are too.<br>
However, if they complete all of their own tasks, they will not be killed.<br>
They can not kill, sabotage, and using vents.<br>

#### Settings

| Settings Name                           |
| --------------------------------------- |
| MadGuardian Can See Own Cracked Barrier |

### MadSnitch

Team : Impostor<br>
Decision : Crewmate or Engineer<br>

Belongs to the Impostor team, but MadSnitch does not know who the Impostor is.<br>
Impostors also doesn't know who MadSnitch is.<br>
Once all tasks are completed, the impostor can be recognized from the MadSnitch.<br>

#### Settings

| Settings Name          |
| ---------------------- |
| MadSnitch Can Use Vent |
| MadSnitch Tasks        |

### SidekickMadmate

Team : Impostor<br>
Decision : Change before Role<br>

This role is created when roles with the ability to shape-shift is shape-shifted.<br>
Belongs to the Impostor team, but SidekickMadmate does not know who the Impostor is.<br>
Impostors also doesn't know who SidekickMadmate is.<br>


There is also a common setting for Madmate type Roles.

| Settings Name                             |
| ----------------------------------------- |
| Madmate Can Fix Lights Out                |
| Madmate Can Fix Comms                     |
| Madmate vision is as long as Impostor one |
| Madmate Vent Cooldown                     |
| Madmate In Vent Max Time                  |

### Watcher

Team : Impostors or Crewmates<br>
Decision : Impostor or Crewmates<br>

The Watcher is a player capable of seeing everyone's votes during meetings.<br>

#### Settings

| Settings Name     |
| ----------------- |
| EvilWatcherChance |

### Bait

Team : Crewmates<br>
Decision : Crewmate<br>

When the Baits are killed, they can force the player who killed them to report their bodies.<br>

### Dictator

Team : Crewmates<br>
Decision : Crewmate<br>

If you vote for someone during the meeting, you can force the meeting to end and hang the person you are voting for.<br>
The dictator dies at the time of the vote.<br>

#### Settings

| Settings Name   |
| --------------- |
| Block Move Time |

### Doctor

Team : Crewmates<br>
Decision : Scientist<br>

You can know why players died.And you can use vitals anywhere with gadget charges.<br>

#### Settings
| Settings Name                      |
| ---------------------------------- |
| Doctor TaskCompletedBattery Charge |

### Lighter

Team : Crewmates<br>
Decision : Crewmate<br>

Upon completion of the task, one's field of vision expands and is no longer affected by the power outage's reduction in visibility.<br>

### Mayor

Team : Crewmates<br>
Decision : Crewmate<br>

The Mayors have multiple votes, which can be grouped together and put into a single player or skip.<br>

#### Settings

| Settings Name                |
| ---------------------------- |
| Mayor Additional Votes Count |
| Mayor Has Portable Button    |
| Mayor Number Of Use Button   |

### SabotageMaster

Team : Crewmates<br>
Decision : Crewmate<br>

The SabotageMasters can fix sabotage faster.<br>
Communications in MIRA HQ, reactor and O2 can both be fixed by fixing one of them.<br>
Lights can be fixed by touching a single lever.<br>
Opening a door in Polus or The Airship will open all the doors in that room.<br>

#### Settings

| Settings Name                                             |
| --------------------------------------------------------- |
| SabotageMaster Fixes Sabotage Limit(Ignore Closing Doors) |
| SabotageMaster Can Fixes Multiple Doors                   |
| SabotageMaster Can Fixes Both Reactors                    |
| SabotageMaster Can Fixes Both O2                          |
| SabotageMaster Can Fixes Both Communications In MIRA HQ   |
| SabotageMaster Can Fixes Lights Out All At Once           |

### Sheriff

Team : Crewmates<br>
Decision : Crewmate(Only host is the Crewmate)<br>

The Sheriffs can kill Impostors.<br>
However, if they kill the Crewmates, they will die.<br>
They do not have tasks.<br>

#### Settings

| Settings Name                                |
| -------------------------------------------- |
| Sheriff Can Kill [Arsonist](#Arsonist)       |
| Sheriff Can Kill [Madmate](#Madmate)         |
| Sheriff Can Kill [Jester](#Jester)           |
| Sheriff Can Kill [Terrorist](#Terrorist)     |
| Sheriff Can Kill [Opportunist](#Opportunist) |
| Sheriff Can Kill [Egoist](#Egoist)           |
| Sheriff Can Kill Crewmates As It             |
| Sheriff Shot Limit                           |

### Snitch

Team : Crewmates<br>
Decision : Crewmate<br>

When the Snitches complete their tasks, the name of the Impostors will change to red,can see the direction with the arrow.<br>
However, when the number of their tasks are low, it will be notified to the Impostors.<br>

#### Settings

| Settings Name                  |
| ------------------------------ |
| Snitch Can Get Arrow Color     |
| Snitch Can Find Neutral Killer |

### SpeedBooster

Team : Crewmates<br>
Decision : Crewmate<br>

Completing the task will make a random surviving player speed up.<br>

#### Settings

| Settings Name     |
| ----------------- |
| Speed at speed up |

### Trapper

Team : Crewmates<br>
Decision : Crewmate<br>

When killed, it immobilizes the killed player for a few seconds.<br>

### Arsonist

Team : Neutral<br>
Decision : Impostor<br>
Victory Conditions : Douse all alive crewmates<br>

When they use kill button and being close to target, they can douse oil to crewmate.<br>
If they finish dousing to all alive crewmates and enter vents, they will win.<br>

#### Settings

| Settings Name |
| ------------- |
| Dousing time  |
| Cooldown      |

### Egoist

Team : Neutral<br>
Decision : Shapeshifter<br>
Victory Conditions : Achieve the Impostor victory conditions after the Impostor annihilation.<br>

Impostor knows the egoist.<br>
Egoist also know Impostor.<br>
Impostor and Egoist cannot kill each other.<br>
You win when the other Impostor are wiped out.<br>
If the Egoist wins, the Impostor will be defeated.<br>

The conditions for defeat are as follows.<br>

1.Egoist dies.<br>
2.Imposter victory with allies remaining.<br>
3.Other Neutrals win.<br>

### Executioner


Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Target Voted Out<br>

The target is marked with a diamond that is visible only from here.<br>
If the vote expels the person with diamonds on his/her head, he/she wins alone.<br>
If the target is killed, the position changes.<br>
If the target is a jester, it wins an additional victory.<br>

### Jester

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Get Voted Out<br>

The Jesters are the neutral role which can win by getting voted out.<br>
If the game ends without getting voted out., or if they are killed, they lose.<br>

### Opportunist

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : Aliving when one of the teams wins<br>

The Opportunists are the Neutral role, with an additional win if thay are still alive at the end of the game.<br>
They do not have tasks.<br>

### SchrodingerCat

Team : Neutral<br>
Decision : Crewmate<br>
Victory Conditions : None<br>

By default, it has no victory condition, and only when the condition is met does it have a victory condition.<br>

1.If you are killed by an Imposter, you prevent a kill and become an Imposter.<br>
2.If you are killed by a sheriff, you prevent a kill and become a crewmate.<br>
3.If you are killed by a neutral, you prevent the kill and become a neutral.<br>
4.If you are expelled, your position does not change and you die with the same victory conditions as before.<br>
5.If you are killed by a warlock's ability, the victory condition remains the same and you die.<br>
6.If a player is killed by suicide kills (except vampire kills), the victory condition remains the same and the player dies.<br>

Also common to all Schrodinger's cats, there are no tasks.<br>

#### Settings

| Settings Name                                              |
| ---------------------------------------------------------- |
| SchrodingerCat Before The Change CanWin As A Crewmate Team |
| SchrodingerCat Exiled Team Changes                         |

#### Settings

| Settings Name                               |
| ------------------------------------------- |
| Executioner Can be Target Impostor          |
| Executioner Change Role After Target Killed |

### Terrorist

Team : Neutral<br>
Decision : Engineer<br>
Victory Conditions : Finish All Tasks, Then Die<br>

They are the Neutral role where they win the game alone if they die with all their tasks completed.<br>
Any cause of death is acceptable.<br>
If they die without completing their tasks, or if the game ends without they dying, they lose.<br>

## Attribute

### LastImpostor

This is the attribute given to the last Impostor.<br>
Not given to BountyHunter, SerialKiller, or Vampire.<br>

| Settings Name             |
| ------------------------- |
| LastImpostor KillCooldown |

### Lovers

Team : Neutral<br>
Decision : -<br>
Victory Conditions : Alive at the end of the game. (other than task completion)<br>

Two of all players will be cast. (Duplicate to other positions) <br>
If a position with a crew camp task becomes a lover, the task will disappear. <br>
There is a heart symbol after each other's name. <br>
If one dies, the other will die afterwards. <br>
If the lover dies in the vote, the other will also die and become an unreportable corpse. <br>

Example of overlapping job titles: <br>
・ Terrorist lover: If you have a task and die after completing the task, you will win as a terrorist. <br>
・ Mad Snitch Lover: Have a task, and if you complete the task, you can see the Impostor. <br>
・ Snitch lover: No task, Impostor remains unknown. <br>
・ Sheriff Lover: You can kill Impostors as usual. Whether or not you can kill depends on the position of the duplicate source. (Impostor lover can be killed. Crewmate lover cannot be killed) <br>
・ Opportunist lover: Win if you survive. <br>
・ Jester Lover: If Jester Lover is banished, you will win as Jester. If the lover is banished by voting, Jester's lover is defeated. <br>
・ Bait lover: When the lover is killed and the bait lover dies afterwards, the lover immediately reports the bait lover. <br>

## SabotageTimeControl

The time limit for some sabotage can be changed.

| Settings Name             |
| ------------------------- |
| Polus Reactor TimeLimit   |
| Airship Reactor TimeLimit |

## Mode

### DisableTasks

It is possible to disable certain tasks.<br>

| Settings Name              |
| -------------------------- |
| Disable StartReactor Tasks |
| Disable SubmitScan Tasks   |
| Disable SwipeCard Tasks    |
| Disable UnlockSafe Tasks   |
| Disable UploadData Tasks   |

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

| Settings Name             |
| ------------------------- |
| Allow Closing Doors       |
| Impostors Waiting Time(s) |
| Ignore Cosmetics          |
| Ignore Using Vents        |

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

| Settings Name     |
| ----------------- |
| Added The Skeld   |
| Added MIRA HQ     |
| Added Polus       |
| Added The Airship |

### SyncButtonMode

This is the mode in which all players' button counts are synchronised.<br>

#### Settings

| Settings Name    |
| ---------------- |
| Max Button Count |

## OtherSettings

| Settings Name  |
| -------------- |
| When Skip Vote |
| When Non-Vote  |

#### Client Settings

## Hide Codes

By activating, you can hide the lobby code.

You can rewrite the`Hide Game Code Name`in the config file (BepInEx\config\com.emptybottle.townofhost.cfg) to display any character you like when HideCodes are enabled.
You can also change the text color as you like by rewriting`Hide Game Code Color`.

## Force Japanese

Activating forces the menu to be in Japanese, regardless of the language setting.

## Japanese Role Name

By activating, the job title can be displayed in Japanese.
If the client language is English, this setting is meaningless unless `Force Japanese` is enabled.

## Credits

[BountyHunter](#BountyHunter),[Mafia](#Mafia),[Vampire](#Vampire),[Witch](#Witch),[Bait](#Bait),[Mayor](#Mayor),[Sheriff](#Sheriff),[Snitch](#Snitch),[Lighter](#Lighter)roles and more tips to modding : https://github.com/Eisbison/TheOtherRoles<br>
[Opportunist](#Opportunist),[Watcher](#Watcher) roles : https://github.com/yukinogatari/TheOtherRoles-GM<br>
[SchrodingerCat](#SchrodingerCat) role : https://github.com/haoming37/TheOtherRoles-GM-Haoming<br>
[Doctor](#Doctor) role : https://github.com/Dolly1016/Nebula<br>
[Jester](#Jester) and [Madmate](#Madmate) roles : https://au.libhalt.net<br>
[Terrorist](#Terrorist)(Trickstar + Joker) : https://github.com/MengTube/Foolers-Mod<br>

Twitter : https://twitter.com/XenonBottle<br>

Translated with https://www.deepl.com<br>
