# Town Of Host
![TownOfHost-Title](https://user-images.githubusercontent.com/51523918/147845737-440bc415-0d0f-42eb-b1d4-6aab36937bd4.jpg)

このREADMEは英語版です。<br>
! My English isn't very good, so if this readme is wrong, please use Google Translator to Japanese readme. !

## Regarding this mod
This mod is unofficial, and Innersloth, the developer of Among Us, has no involvement in the development of this mod.
Please do not contact the official team regarding any issues with this mod.

## Features
This mod only needs to be installed on the host's client to work, and works regardless of whether or not other client mods have been installed, and regardless of the type of terminal.
Unlike mods that use custom servers, there is no need to add servers by editing URLs or files.

However, please note that the following restrictions will apply as we are using a mechanism to replace the official additional roles.

- The same additional role or role that is being replaced can not be introduced at the same time.
- If the host changes due to factors such as a host leaving in the middle of a session, the processing related to the additional role may not work properly.
- If a special role is used, the settings for that special role will be rewritten. (Example:Remove cooldown for vent, Disable vitals for Scientist, etc.)

Note that if a player other than the host plays with this mod installed, the following changes will be made.

- Display of the special role's own start screen.
- Display of the normal victory screen for the special role.
- Add additional settings.
- etc.

Please note that if a player without the mod gets a special role, it will only be displayed as Scientist/Engineer, so please explain the role before the game starts.

## Custom Settings Menu
Pressing the Tab key in the standby lobby will change the room setting screen to a setting screen dedicated to Town Of Host.
| Key | Action |
| :---: | ---- |
| Tab | Open/Close Custom Settings Menu |
| Up | Corsor Up |
| Down | Cursor Down |
| Right | Execute Item |
| Left | Go Back One |
| Number | Enter A Value |

However, The numeric keypad is not supported.

## Roles

### Jester

Team：Neutral<br>
Replace From：Scientist<br>
Victory Conditions：Voted out<br>

They are the neutral role that becomes the sole winner when they are banished by vote.
If the game ends without being banished, or if they are killed, they are defeated.
They can not use their vital.<br>

### Madmate

Team：Imposter
Replace From：Engineer

Belongs to the Imposter team, but Madmate does not know who the Imposter is.
Imposter also doesn't know who Madmate is.
They can not kill or sabotage, but they can enter the vent.

(There are special settings for them.)

### Bait

Team：Crewmates
Replace From：Scientist

When they are killed, they can force the person who killed them to report their corpse.
They can not use their vital.

### Terrorist

Team：Neutral<br>
Replace from：Engineer<br>
Victory conditions：Finish all tasks, then die<br>

They are the neutral role where they win the game alone if they die with all their tasks completed.
Any cause of death is acceptable.
If they die without completing their tasks, or if the game ends without they dying, they lose.

### Sidekick

Team：Impostor<br>
Replace From：Shapeshifter<br>

Can vent, sabotage, and transform initially, but can not kill.
If all of the Imposters who are not Sidekicks die, the Sidekick will be able to kill.
If the Sidekick is not killable, they will still have a kill button, but they can not be killable.
They can continue to transform even after the kill is enabled.

### Vampire

Team：Impostor<br>
Replace From：Impostor<br>

They are the role where the kill actually occurs 10 seconds after they press the kill button.
Teleportation does not occur when a kill is made.
Also, if a meeting starts before 10 seconds have passed since they pressed the kill button, the kill will occur at that moment.
However, only if they kill Bait will it be a normal kill and they will be forced to report it.

(There are special settings for them.)

### SabotageMaster

Team：Crewmates
Replace From：Scientist

Reactors meltdown, oxygen disturbance and MIRA HQ's communication disturbance can both be fixed by repairing one of them.
Power failures can all be fixed by touching a single lever.
Opening a door in Polus or The Airship will open all the doors in that room.

(There are special settings for them.)

### MadGuardian

Team：Imposter
Replace From：Scientist

Belongs to the Imposter team, but MadGuardian does not know who the Imposter is.
Imposter also doesn't know who MadGuardian is.
However, if they complete all of their own tasks, they will no longer be killed.
They can not kill, sabotage, and to enter the vent.

## モード

### HideAndSeek/鬼ごっこモード

#クルー陣営(青色)勝利条件
全てのタスクを完了させること。
※幽霊のタスクはカウントされません。

#インポスター陣営(赤色)勝利条件
全てのクルーをキルすること。
※クルーとインポスターが同数であってもクルーが全滅していないと試合は終わりません。

#Fox(紫色)勝利条件
Trollを除くいずれかの陣営が勝利したときに生き残っていること。

#Troll(緑色)勝利条件
インポスターにキルされること。

#禁止事項
・サボタージュ
・アドミン
・カメラ
・幽霊が生存者に位置情報を伝える行為
・待ち伏せ(クルーのタスク勝利が不可能となる可能性があるため。)

#できないこと
・死体の通報
・緊急会議ボタン
・サボタージュ

(専用の設定があります。)

### NoGameEnd/デバッグモード

#クルー陣営勝利条件
なし

#インポスター陣営勝利条件
なし

#禁止事項
なし

#できないこと
ホストのSHIFT+L以外でのゲーム終了。

勝利判定が存在しないデバッグ用のモードです。

### SyncButtonMode/ボタン回数同期モード

プレイヤー全員のボタン回数が同期されているモードです。






## Credits

Bait, Vampire, and more tips to modding：https://github.com/Eisbison/TheOtherRoles<br>
Jester and Madmate：https://au.libhalt.net<br>
Terrorist(Trickstar + Joker = Terrorist)：https://github.com/MengTube/Foolers-Mod<br>

Twitter：https://twitter.com/XenonBottle
