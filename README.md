WARNING: This mod was made for my own personal use, so accessibility/polish weren't high priorities. This mod may be updated in the future for ease of use, but for now read this if you encounter any issues.

Overview:
This mod adds social deduction elements to Risk of Rain 2, similar to gamemodes such as TTT. Each stage, a new round starts and everyone is assigned the role of either
"Innocent" or "Traitor." This mod also allows for some adjustments to item distribution to prevent the game from snowballing in one person's favor, as well as
various tweaks to make PvE less of a threat to encourage more PvP-related action.

Roles:
 - Innocent: Work together with other Innocent players to complete the teleporter event each stage as normal OR seek out the traitors and eliminate them to win.
 - Traitor: Eliminate all innocent players and/or prevent them from completing the teleporter event.

Features:
Press F2 in game to show scoreboard (updates once at the start of each stage)
Press F3 to see your role, and your teammates if you're a traitor.
(As host) Press F5 to skip the current round and move on to the next one. Mostly as a debug tool in case something goes wrong.

- Ability to tweak various pve balance/item catchup mechanics via console commands. 
- Automatically advances to the next stage when a team is dead, loops to the first stages instead of moon.
- Difficulty doesn't increase with stage count
- Can disable self-damage.
- Hides ally health bars, although it will still show if a player is dead.

(POSSIBLY GAMEBREAKING) Known issues:
- If the lobby size changes in between games, an error may occur where the host or other players are unable to spawn in properly. Having the server host relaunch RoR2 should fix this.
- A player dying to any damage type without a killer (Artifact of frailty fall damage, explosive barrels, etc.) will effectively softlock a team from winning via kills for the rest of the stage. As a bandaid, host can skip to the next stage at any time by pressing F5.
- Mod may not be compatible with certain other mods. If you have an issue that isn't listed here, try disabling all mods not in the recommended modpack.
- Avoid warping to anywhere without a teleporter, such as using the celestial portal or bulwark's ambry. These areas break certain aspects of the mod, but the game will resume as normal after leaving those areas.

All commands can have their values changed mid-game and will usually be reflected upon loading the next stage.
Command guide:
- a_traitor_count [arg]: Sets the number of traitors to be assigned next stage.
- a_max_rounds [arg]: Set how many stages will need to be played before the game is automatically ended. Minimum 1.
- a_player_damage_out [arg]: % of base damage to be added to all player characters' base damage stat next stage. E.g. -99 would be 1%, 0 would be default base damage, 150 would be 250%
- a_monster_damage_out [arg]: Works the same as player damage modifier, but applies to all non-player bodies.
- a_toggle_self_damage: Toggles whether characters can damage themselves.
- a_toggle_lunar_removal: Toggles whether lunar items are removed from inventories at the end of each stage.
- a_average_item_range [arg1] [arg2] [arg3]: Settings for how items are averaged between players after each round. Default setting: 1 1 1. Supports negative values, but untested.
	- arg 1: set to 0 to disable feature completely, 1 to enable.
	- arg 2: give random items until the player is (x) number of items below average. 
	- arg 3: remove items until player is (x) number of items above average. 

Recommended House Rules:
- No talking after dying (Use Push to Talk)
- Don't randomly kill players for no reason if you're innocent (And avoid teamkilling in general)
- Follow the objective of your role
- May want to use a mod like Content Disabler to disable items that cause too much unintentional team damage or are too OP: (Will o wisp, Gasoline, Genesis Loop, Frost Relic)
- Character bans: Loader and Railgunner (too strong single-hit), and Engineer (too passive).