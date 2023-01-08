- [ ] Move all role interactions/actions/code to respective code
- [ ] Fix desync impostor code (we are NOT using TOH's desync code)
- [ ] Re-implement custom death reasons (probably move to PlayerPlus/PlayerState)
- [ ] End of game screen is probably broken and needs fixing
- [ ] Setup custom winners utilizing factions ++ maybe some exccptions
- [ ] Create new RPCs for modded clients and/or setup interactions for modded clients
- [ ] Correct and setup name rendering within meetings and correctly for teammates (DynamicName.cs)
- [ ] Explore possibly using AU HnS timer for specific roles (Serial Killer suicide, etc)
- [ ] Fully implement PlayerState / PlayerPlus over original TOH PlayerState
- [ ] First round of testing after implementing all roles
- [ ] Cleanup old/unused code

- [ ] Implement Custom Gamemode Support
- [ ] Allow for gamemodes across addons
- [x] ~~Gamemodes should also be able to change option tabs~~

- [x] ~~Add Reactor "RpcLocalHandling" to ModRPC~~
- [x] ~~Support optional parameters for ModRPC~~


Minor:
- [ ] Add option propagation (display) to /m command
- [ ] Dynamic Name renders for a brief period before the Intro cutscene starts
- [ ] After-game lobby spams "TownOfHost.Extensions.PlayerControlExtensionsRewrite.GetRoleName Invalid Custom Role"
- [ ] FIX Roles that extend other roles (Blood Knight extending Impostor) show both role's tasks in the "Tasks" panel
- [ ] Allow removing of "Maximum" setting from roles
- [ ] Deprecate AsNormalOptions()
- [ ] Boot non-vent-allowed players out of vent



Even Minor(er):
- [x] ~~Vampire should kill all bitten when meeting is called~~
- [x] ~~Miner LastEnteredVentLocation (should also actually be a local variable to miner)~~

- [ ] Possible attempt to make transporter into ToU transporter (select players to transport). I REFUSE to force the player to use commands for it

- [ ] Derive certain delays (game end delay) from current AU server ping


Investigating:
- [ ] Role option propagation to role descriptions (and how that'd work with localization)



BUGS:
- [ ] Serial Killer in FFA/Color wars cannot kill twice
- [ ] Player turning into ghost instead of dying in Color wars
- [ ] Custom win screen not working when host wins (otherwise works great)
- [ ] Issue with settings menu? Sometimes?
- [ ] Discussions randomly dying in a vent? (skill issue?) (probably ping)
- [ ] Black screen when connecting to lobby (good chance just an AU issue)