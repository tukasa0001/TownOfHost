NEW BUGS AFTER FIRST TESTING SESSION:
- [ ] ~~Client player names don't render at all~~ Only occurs on modded server
- [ ] ~~Host player name sometimes renders during meeting / chatting~~ also might be modded server only
- [ ] ~~All players show up as "wwww" for host~~
- [ ] Arsonist kills everyone on ignite
- [ ] Desync impostor for host
- [ ] No sabotage win condition
- [ ] End screen not working like at all
- [ ] Setting roles with a count higher than 2 does not work
- [ ] Host settings are out of sync
- [ ] Host kills straight away with vampire
- [ ] Vampire should GA target not self
- [ ] Time thief not stealing time accurately (investigate)
- [ ] Miner pet does not teleport back to vent
- [ ] Donks in discussions skin when voted out
- [ ] Host skin doesnt change when voted out and is desynced





- [ ] Move all role interactions/actions/code to respective code
- [ ] Fix desync impostor code (we are NOT using TOH's desync code)
- [ ] Re-implement custom death reasons (probably move to PlayerPlus/PlayerState)
- [x] ~~End of game screen is probably broken and needs fixing~~
- [x] ~~Setup custom winners utilizing factions ++ maybe some exccptions~~
- [x] ~~Create new RPCs for modded clients and/or setup interactions for modded clients~~
- [x] ~~Correct and setup name rendering within meetings and correctly for teammates (DynamicName.cs)~~
- [x] ~~Explore possibly using AU HnS timer for specific roles (Serial Killer suicide, etc)~~
- [ ] Fully implement PlayerState / PlayerPlus over original TOH PlayerState this is probably bad code practice
- [ ] First round of testing after implementing all roles
- [ ] Cleanup old/unused code

- [x] ~~Implement Custom Gamemode Support~~
- [x] ~~Allow for gamemodes across addons~~
- [x] ~~Gamemodes should also be able to change option tabs~~

- [x] ~~Add Reactor "RpcLocalHandling" to ModRPC~~
- [x] ~~Support optional parameters for ModRPC~~


Minor:
- [ ] Add option propagation (display) to /m command
- [ ] Dynamic Name renders for a brief period before the Intro cutscene starts
- [x] ~~After-game lobby spams "TownOfHost.Extensions.PlayerControlExtensionsRewrite.GetRoleName Invalid Custom Role"~~
- [ ] FIX Roles that extend other roles (Blood Knight extending Impostor) show both role's tasks in the "Tasks" panel
- [ ] Allow removing of "Maximum" setting from roles
- [ ] Deprecate AsNormalOptions()
- [ ] Boot non-vent-allowed players out of vent
- [ ] **RETHINKING** ~~Gamemodes should always have unique options regardless of if they use the same tab as other gamemodes or not~~
- [ ] **RETHINKING** ~~To a similar extent, gamemodes should have unique config files (to store their options)~~
- [ ] Switching gamemode should enforce the current gamemode's settings
  - Currently setting an option in one gamemode overrides the option in other gamemodes if they're referencing the same values



Even Minor(er):
- [x] ~~Vampire should kill all bitten when meeting is called~~
- [x] ~~Miner LastEnteredVentLocation (should also actually be a local variable to miner)~~

- [ ] Possible attempt to make transporter into ToU transporter (select players to transport). I REFUSE to force the player to use commands for it

- [x] ~~Derive certain delays (game end delay) from current AU server ping~~


Investigating:
- [ ] Role option propagation to role descriptions (and how that'd work with localization)





BUGS:
- [x] ~~Serial Killer in FFA/Color wars cannot kill twice~~
- [x] ~~Player turning into ghost instead of dying in Color wars~~
- [ ] ~~Custom win screen not working when host wins (otherwise works great)~~
- [ ] Issue with settings menu? Sometimes?
- [x] ~~Discussions randomly dying in a vent? (skill issue?) (probably ping)~~
- [x] ~~Black screen when connecting to lobby (good chance just an AU issue)~~
