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

Minor:
- [ ] Dynamic Name renders for a brief period before the Intro cutscene starts
- [ ] After-game lobby spams "TownOfHost.Extensions.PlayerControlExtensionsRewrite.GetRoleName Invalid Custom Role"
- [ ] Roles that extend other roles (Blood Knight extending Impostor) show both role's tasks in the "Tasks" panel
- [ ] Allow removing of "Maximum" setting from roles
- [ ] Deprecate AsNormalOptions()
- [ ] Boot non-vent-allowed players out of vent

Even Minor(er):
- [ ] Vampire should kill all bitten when meeting is called
- [ ] Miner LastEnteredVentLocation (should also actually be a local variable to miner)