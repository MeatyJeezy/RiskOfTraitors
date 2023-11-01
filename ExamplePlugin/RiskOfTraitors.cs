using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Networking;
using R2API.Utils;
using System;
using R2API.Networking;
using R2API.Networking.Interfaces;

// ALL ENDINGS:
/* 
    Ending Name: EscapeSequenceFailed, Index: 0
    Ending Name: LimboEnding, Index: 1
    Ending Name: MainEnding, Index: 2
    Ending Name: ObliterationEnding, Index: 3
    Ending Name: PrismaticTrialEnding, Index: 4
    Ending Name: StandardLoss, Index: 5
    Ending Name: VoidEnding, Index: 6
*/
// KNOWN BUGS:
// - Exiting the run in any other way than a gameover or the run_end command results in bugs for the next round.
// - Dying to world damage causes the game to effectively softlock a team from winning for the rest of the stage.
// - Scoreboard doesn't properly update at the end of the game.
// - A player leaving will likely require a lobby restart as they can still be assigned traitor role since their controller still exists.

// Features I'd like:
// - Player nameplates hide behind map geometry or aren't shown at all unless you hover over with your crosshair
// - User accessibility / QoL features like a GUI for the options and config files.
// - Traitor Shop with special items.
// - Funny Easter Egg where it has a chance to warp you to Skeld. (High effort shitpost)

namespace RiskOfTraitors
{
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    //[BepInDependency(CommandHelper.PluginGUID)]
    //[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]

    public class RiskOfTraitors : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "MeatyJesus";
        public const string PluginName = "RiskOfTraitors";
        public const string PluginVersion = "2.0.0";

        public TeamComponent team;
        public TeleporterInteraction teleport;
        public CustomGUI score = null;
        public RoleDisplayUI roleDisplay = null;
        public SyncScoreString sync;
        public SyncTraitorTeam syncTraitor;
        public AverageItems averageItems;

        public static bool canDamageSelf = false;
        public static int itemsBelowAverage = 1;
        public static int itemsAboveAverage = 1;
        public static bool enableItemAveraging = true;
        public static bool removeLunarItems = false;
        public int playerCount;
        public int deadInnoCount;
        public int deadTraitorCount;
        public bool innocentWin = false;
        public bool traitorWin = false;
        public bool newGameStart = true;
        public bool wonByEliminatingAllTraitors = false;
        public bool sentInnoWinMessageAlready = false;
        public bool skippingRound = false;

        public int[] winsAsTraitor;
        public int[] winsAsInnocent;
        public int[] killsOnTraitors;
        public int[] killsOnInnocents;
        public int[] teamKills;
        public int[] randomNum;

        // Rounds to be played. Can be changed in the Update function
        public static int maxRounds = 5;
        public int roundsPlayed = 0;

        public static int numTraitors = 1; // Can be changed, default to 1.
        public static int oldPlayerDamageMul = 0;
        public static int playerOutgoingDamageMul = 0;
        public static int oldEnemyDamageMul = 0;
        public static int enemyOutgoingDamageMul = 0;
        public static bool updatePlayerDamageMul= false;
        public static bool updateMonsterDamageMul= false;

        // init text for imposter
        public const string imposterText = "<color=red>TRAITOR:</color> Eliminate all Innocent players to win.";
        public const string innoText = "<color=green>INNOCENT:</color> Escape via the teleporter to win, or eliminate all Traitors.";

        public static string scoreDataFromServer = "";
        public static string syncedScore = "Default Synced Score";
        public static string traitorTeamNames = "";


        // I'm such a Scorpio for doing this haha!
#pragma warning disable Publicizer001 // Accessing a member that was not originally public

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);

            CommandHelper.AddToConsoleWhenReady();

            NetworkingAPI.RegisterMessageType<SyncScoreString>();
            NetworkingAPI.RegisterMessageType<SyncTraitorTeam>();
            // hooks
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            Stage.onServerStageBegin += Stage_onServerStageBegin;
            Run.onServerGameOver += Run_onServerGameOver;
            RoR2.CharacterBody.onBodyStartGlobal += BodyStartGlobal;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            RoR2.Run.onRunStartGlobal += Run_onRunStartGlobal;
            // COMMENT OUT, ONLY FOR TESTING
            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };

            TeamComponent.onJoinTeamGlobal += TeamComponent_onJoinTeamGlobal;
            On.RoR2.TeleporterInteraction.Start += TeleporterInteraction_Start;
            On.RoR2.Run.CCRunEnd += Run_CCRunEnd;

            Log.Info(nameof(Awake) + " DONE LOADING RISK OF TRAITORS.");
        }

        // Commands start here
        [ConCommand(commandName = "a_toggle_lunar_removal", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggles whether lunar items are removed from inventories after each stage.")]
        private static void ToggleLunarRemoval(ConCommandArgs args)
        {
            removeLunarItems = !removeLunarItems;
            ChatMessage.Send("<color=blue>Lunar item removal after each stage set to: <color=green>" + removeLunarItems + "</color></color>");
        }

        [ConCommand(commandName = "a_toggle_self_damage", flags = ConVarFlags.ExecuteOnServer, helpText = "Toggles whether any characters can harm themselves. Only changes damage if Chaos Artifact is on.")]
        private static void ToggleSelfDamage(ConCommandArgs args)
        {
            canDamageSelf = !canDamageSelf;
            ChatMessage.Send("<color=yellow>Self damage set to: <color=green>" + canDamageSelf + "</color></color>");
        }

        [ConCommand(commandName = "a_max_rounds", flags = ConVarFlags.ExecuteOnServer, helpText = "Set the number of rounds to be played before the game ends.")]
        private static void MaxRounds(ConCommandArgs args)
        {
            maxRounds = args.GetArgInt(0);
            if (maxRounds < 1)
                maxRounds = 1;
            Log.Info($"Max rounds set to: {maxRounds}");
            ChatMessage.Send("<color=yellow>Max rounds set to: <color=green>" + maxRounds + "</color></color>");
        }
        [ConCommand(commandName = "a_player_damage_out", flags = ConVarFlags.ExecuteOnServer, helpText = "Set player damage for next stage in increments of x%. 0 is default, -99 is 1%, 50 is 150%.")]
        private static void PlayerDamage(ConCommandArgs args)
        {
            playerOutgoingDamageMul = args.GetArgInt(0);
            if (playerOutgoingDamageMul < -99)
                playerOutgoingDamageMul = -99;
            Log.Info($"Player damage set to: {100 + 100 * playerOutgoingDamageMul * 0.01f}%");
            ChatMessage.Send("<color=yellow>Player damage set to: <color=green>" + (100 + 100 * playerOutgoingDamageMul * 0.01f) + "%</color></color>");
        }
        [ConCommand(commandName = "a_monster_damage_out", flags = ConVarFlags.ExecuteOnServer, helpText = "Set monster damage in increments of x%. 0 is default, -99 is 19%, 50 is 150%.")]
        private static void MonsterDamage(ConCommandArgs args)
        {
            enemyOutgoingDamageMul = args.GetArgInt(0);
            if (enemyOutgoingDamageMul < -99)
                enemyOutgoingDamageMul = -99;
            Log.Info($"Enemy damage set to: {100 + 100 * enemyOutgoingDamageMul * 0.01f}%");
            ChatMessage.Send("<color=yellow>Enemy damage set to: <color=green>" + (100 + 100 * enemyOutgoingDamageMul * 0.01f) + "%</color></color>");
        }
        [ConCommand(commandName = "a_traitor_count", flags = ConVarFlags.ExecuteOnServer, helpText = "Set number of traitors for next stage.")]
        private static void TraitorCount(ConCommandArgs args)
        {
            numTraitors = args.GetArgInt(0);
            if (numTraitors <= 0)
                numTraitors = 0;
            ChatMessage.Send("<color=yellow>Next stage traitor count set to: <color=red>" + numTraitors + "</color></color>");
            Log.Info($"Traitor count set to: {numTraitors}");
        }
        [ConCommand(commandName = "a_average_item_range", flags = ConVarFlags.ExecuteOnServer, helpText = "After each stage, average item counts across all players. arg1: true/false, arg2: give until (x) below average, arg3: remove until (x) above average.")]
        private static void AverageItemRange(ConCommandArgs args)
        {
            enableItemAveraging = args.GetArgBool(0);
            itemsBelowAverage = args.GetArgInt(1);
            itemsAboveAverage = args.GetArgInt(2);
            /*if (itemsAboveAverage < 0)
                itemsAboveAverage = 0;
            if (itemsBelowAverage < 0)
                itemsBelowAverage = 0;*/
            if (enableItemAveraging)
            {
                ChatMessage.Send("<color=yellow>Min items below average set to: <color=green>" + itemsBelowAverage + "</color>, Max items above average set to: <color=green>" + itemsAboveAverage + "</color></color>");
                Log.Info($"Min items below average: {itemsBelowAverage}, Max items above average: {itemsAboveAverage}");
            }
            else
            {
                ChatMessage.Send("Item averaging each stage has been <color=red>disabled.</color>");
                Log.Info($"Item averaging each stage has been disabled.");
            }
        }
        // Commands end here

        // Reset some vars for new game
        private void Run_onRunStartGlobal(Run obj)
        {
            roundsPlayed = 0;
            innocentWin = false;
            traitorWin = false;
            newGameStart = true;
        }
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            try
            {
                if (damageInfo.damageType != (DamageType.OutOfBounds | DamageType.FallDamage | DamageType.ClayGoo | DamageType.VoidDeath))
                {
                    var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    //Log.Info($"Attacker Body netidentity: {attackerBody.netId}, self netidentity: {self.body.netId}");
                    if (attackerBody.netId == self.body.netId && !canDamageSelf)
                    {
                        damageInfo.damage = 0;
                    }
                }
                else
                {
                    Log.Info($"Damage type was {damageInfo.damageType}, skipping.");
                }
                orig(self, damageInfo);
            } catch
            {
                orig(self, damageInfo);
                Log.Info($"Skipping damage check.");
            }
        }

        // Adjusts player and monster damage based on console command variables. Updates occur only when the body spawns
        private void BodyStartGlobal(CharacterBody body)
        {
            if (body.isPlayerControlled) {
                var oldBaseDamage = body.baseDamage;
                body.baseDamage *= (1 + playerOutgoingDamageMul * 0.01f);
                Log.Info($"Player spawned with basedamage of {body.baseDamage} from old {oldBaseDamage}");
            }
            else
            {
                body.baseDamage *= (1 + enemyOutgoingDamageMul * 0.01f);
                //Log.Info($"Monster spawned with basedamage of {body.baseDamage} from old {oldBaseDamage}");
            }
        }

        // Reset some vars for new game. Handle if the run ends via command. Redundant.
        private void Run_CCRunEnd(On.RoR2.Run.orig_CCRunEnd orig, ConCommandArgs args)
        {
            orig(args);
            roundsPlayed = 0;
            innocentWin = false;
            traitorWin = false;
            newGameStart = true;
        }

        // There's probably a better way to do this, but this code runs client side at the start of the round. Initializes scoreboards and player roles. Breaks on stages without teleporters.
        private void TeleporterInteraction_Start(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction self)
        {
            orig(self);
            teleport = self;
            Log.Info("Teleporter has been initialized.");

            if (NetworkServer.active)
            {
                // Send score string to clients
                new SyncScoreString(scoreDataFromServer).Send(NetworkDestination.Clients);
                syncedScore = scoreDataFromServer;
                ChatMessage.Send("<color=#A040C0>Round <color=yellow>" + (roundsPlayed + 1) + "/" + maxRounds + "</color></color>");
            }

            //Log.Info($"syncedScore is: {syncedScore}");
            // init new scoreboard each stage
            score = new CustomGUI();
            score.Init();
            roleDisplay = new RoleDisplayUI();
            roleDisplay.Init();

            // RoleDisplay();
        }

        private void TeamComponent_onJoinTeamGlobal(TeamComponent teamcomp, TeamIndex index) {
            // This should hide all ally cards, but still shows the dead ones for some reason
            team = teamcomp;
            teamcomp.hideAllyCardDisplay = true;
        }

        // Also redundant 
        private void Run_onServerGameOver(Run run, GameEndingDef gameEnd)
        {
            // Reset vars to default to begin new game:
            roundsPlayed = 0;
            innocentWin = false;
            traitorWin = false;
            newGameStart = true;
        }

        public NetworkConnection GetNetworkConnectionFromPlayerControllerIndex(int index) // This should HOPEFULLY resolve the issue of desynced playermastercontroller and networkconnection IDs by checking a common field. If not idk wtf to do.
        {
            PlayerCharacterMasterController player = PlayerCharacterMasterController.instances[index];
            for (int i = 0; i < NetworkServer.connections.Count; i++)
            {
                NetworkConnection conn = NetworkServer.connections[i];
                // True if one of the clientOwnedObjects is a player?
                // It seems the code will throw an error here if the player slot is reserved by someone who isn't connected. Honestly preferable, as the user will at least know something has gone wrong when they don't spawn in.
                if (conn.clientOwnedObjects.Contains(player.networkUserInstanceId))
                {
                    Log.Info($"Match found in conn: {conn.clientOwnedObjects} for {player.networkUserInstanceId}");
                    return conn;
                }
                else
                    Log.Warning($"No match found in conn: {conn.clientOwnedObjects} for {player.networkUserInstanceId}");
            }
            ChatMessage.Send("<color=orange>An error may have occurred. If you notice any gamebreaking bugs, have the host relaunch their game.</color>");
            return NetworkServer.connections[0];
        }

        // String is the message to send, index is the index of the player to send it to.
        private void SendMessageFromStringAndIndex(string message, int index) {
            Chat.SimpleChatMessage simpleChatMessage = new Chat.SimpleChatMessage();
            simpleChatMessage.baseToken = "{0}";
            simpleChatMessage.paramTokens = new string[1] { message };
            NetworkWriter networkWriter = new NetworkWriter();
            networkWriter.StartMessage(59);
            networkWriter.Write(simpleChatMessage.GetTypeIndex());
            networkWriter.Write(simpleChatMessage);
            networkWriter.FinishMessage();
            GetNetworkConnectionFromPlayerControllerIndex(index).SendWriter(networkWriter, QosChannelIndex.chat.intVal);
        }

        // Maybe I'm dumb and there was a more efficient way to do this.
        private int FindPlayerIndexFromBodyInstanceID(NetworkInstanceId playerBodyId)
        {
            int i;
            for (i = 0; i < playerCount; i++) {
                // A match, return index i
                var user = NetworkUser.readOnlyInstancesList[i];
                if (user.master.bodyInstanceId == playerBodyId) {
                    return i;
                }
            }
            // Couldn't find? Default to 0
            return 0;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            var playerCount = PlayerCharacterMasterController.instances.Count;
            bool victimWasInno = true;
            bool killerIsInno = true;

            // indexes set to -1 unless there is a match for a player somewhere.
            var victimIndex = -1;
            var attackerIndex = -1;
            // NEED TO CHECK IF ATTACKER AND/OR VICTIM WAS A PLAYER FIRST
            for (int i = 0; i < playerCount; i++)
            {
                if (report.victimMaster.bodyInstanceId != null && report.victimMaster.bodyInstanceId == PlayerCharacterMasterController.instances[i].master.bodyInstanceId)
                {
                    victimIndex = FindPlayerIndexFromBodyInstanceID(report.victimMaster.bodyInstanceId);
                    //ChatMessage.Send($"Victim index is {victimIndex}, Name of player: {PlayerCharacterMasterController.instances[victimIndex].GetDisplayName()}");
                }
                // Check for null in case world damage somehow
                if (report.attackerMaster.bodyInstanceId != null && report.attackerMaster.bodyInstanceId == PlayerCharacterMasterController.instances[i].master.bodyInstanceId)
                {
                    attackerIndex = FindPlayerIndexFromBodyInstanceID(report.attackerMaster.bodyInstanceId);
                    //ChatMessage.Send($"Attacker index is {attackerIndex}, Name of player: {PlayerCharacterMasterController.instances[attackerIndex].GetDisplayName()}");
                }
            }
            string reportTraitorKill = "Player <color=red>";
            string reportInnoKill = "Player <color=green>";

            if (victimIndex != -1)
            {
                reportTraitorKill += PlayerCharacterMasterController.instances[victimIndex].GetDisplayName() + "</color> was a <color=red>Traitor!</color>";
                reportInnoKill += PlayerCharacterMasterController.instances[victimIndex].GetDisplayName() + "</color> was a <color=green>Innocent!</color>";
            }

            // Check if can revive && victim is a player. Ignore if a winner is already decided.
            if (report.victimMaster.IsDeadAndOutOfLivesServer() && victimIndex != -1 && !(traitorWin || innocentWin))
            {
                for (int j = 0; j < randomNum.Length; j++) {
                    // SHOULD evaluate to true if victim is the traitor
                    //Log.Info($"Victim body id:  {report.victimMaster.bodyInstanceId} Traitor body id checked: {PlayerCharacterMasterController.instances[randomNum[j]].master.bodyInstanceId}");
                    var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[j]];
                    //if (report.victimMaster.bodyInstanceId == PlayerCharacterMasterController.instances[randomNum[j]].master.bodyInstanceId)
                    if (report.victimMaster.bodyInstanceId == traitoruser.master.bodyInstanceId)
                    {
                        //Log.Info("Entered TRAITOR Victim If statement.");
                        if (attackerIndex != -1)
                        {
                            // Loop to see if traitor was killer
                            for (int i = 0; i < randomNum.Length; i++)
                            {
                                //Log.Info($"Loop i: {i}, bodyId 1: {PlayerCharacterMasterController.instances[randomNum[i]].master.bodyInstanceId} bodyId 2: {report.attackerMaster.bodyInstanceId}");
                                var user = NetworkUser.readOnlyInstancesList[randomNum[i]];
                                if (user.master.bodyInstanceId == report.attackerMaster.bodyInstanceId)
                                {
                                    //Log.Info("Killer was a TRAITOR!");
                                    killerIsInno = false;
                                    teamKills[attackerIndex]++;
                                    reportTraitorKill = "<color=orange>(TEAMKILL)</color> " + reportTraitorKill;
                                }
                                //Log.Info("PAST IF STATEMENT IN LOOP");
                            }
                            //Log.Info("PAST LOOP");
                            if (killerIsInno)
                            {
                                //Log.Info("Killer was INNOCENT - !");
                                killsOnTraitors[attackerIndex]++;
                            }
                            //Log.Info($"T: {reportTraitorKill}");
                            SendMessageFromStringAndIndex(reportTraitorKill, attackerIndex);
                        }

                        // increment dead traitor count
                        deadTraitorCount++;
                        //Log.Info($"Traitor <color=red>{PlayerCharacterMasterController.instances[randomNum[j]].GetDisplayName()}</color> has died. {randomNum.Length - deadTraitorCount}/{randomNum.Length} remain.");
                        victimWasInno = false;
                        if (deadTraitorCount >= randomNum.Length && !traitorWin)
                        {
                            DoInnocentWin();
                        }
                    }
                }

                // For all other deaths:
                // Check which player was killed. Victim should be inno unless changed by previous loop.
                if (victimWasInno)
                {
                    // If killer and victim are inno, add a teamkill to their slot.
                    if (attackerIndex != -1)
                    {
                        // Loop to see if traitor was killer BECAUSE I'M NOT ALLOWED TO HAVE NICE THINGS IG
                        for (int i = 0; i < randomNum.Length; i++)
                        {
                            //Log.Info($"Loop i: {i}, bodyId 1: {PlayerCharacterMasterController.instances[randomNum[i]].master.bodyInstanceId} bodyId 2: {PlayerCharacterMasterController.instances[attackerIndex].master.bodyInstanceId}");
                            var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[i]];
                            
                            // If there is a match, the killer was a traitor. Return false.
                            if (traitoruser.master.bodyInstanceId == report.attackerMaster.bodyInstanceId)
                            {
                                //Log.Info("Killer was a TRAITOR!");
                                killsOnInnocents[attackerIndex]++;
                                killerIsInno = false;
                            }
                        }
                        if (killerIsInno)
                        {
                            teamKills[attackerIndex]++;
                            reportInnoKill = "<color=orange>(TEAMKILL)</color> " + reportInnoKill;
                        }
                        //Log.Info($"I: {reportInnoKill}");
                        SendMessageFromStringAndIndex(reportInnoKill, attackerIndex);
                    }

                    // victim is a player and not traitor, increment the dead inno player count
                    deadInnoCount++;
                    // Last inno player dead, show round-end message
                    // Subtract number of traitor players from player count to get the inno players.
                    if (deadInnoCount >= (playerCount - randomNum.Length) && !innocentWin)
                    {
                        DoTraitorWin();
                    }
                }
            }
        }

        public void DoTraitorWin ()
        {
            // traitor wins, send message and advance stage.
            string traitorWinMessage = "<color=red>Traitor team wins!</color> Traitors: <color=red>";

            // loop for each player, increment their traitor wins if they are traitor and add their name here.
            for (int j = 0; j < randomNum.Length; j++)
            {
                var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[j]];
                // Add their name to the win message
                traitorWinMessage += traitoruser.masterController.GetDisplayName() + " ";
                // Get their CharacterController Index and add a Traitor win.
                winsAsTraitor[FindPlayerIndexFromBodyInstanceID(traitoruser.masterController.master.bodyInstanceId)]++;
            }
            traitorWinMessage += "</color>";
            ChatMessage.Send(traitorWinMessage);
            innocentWin = false;
            traitorWin = true;

            EndRoundWarpToNextStage();
        }
        public void DoInnocentWin ()
        {
            innocentWin = true;
            traitorWin = false;
            wonByEliminatingAllTraitors = true;

            // Build win string for innocent team
            string innoWinMessage = "<color=green>Innocents win!</color> Traitors: <color=red>";
            for (int i = 0; i < randomNum.Length; i++)
            {
                var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[i]];
                innoWinMessage += traitoruser.masterController.GetDisplayName() + " ";
            }
            // Add each inno player + 1 to their score
            bool curPlayerNotTraitor = true;
            for (int i = 0; i < playerCount; i++)
            {
                var user = NetworkUser.readOnlyInstancesList;
                for (int k = 0; k < randomNum.Length; k++)
                {
                    // get each player that wasn't an imposter.
                    // If the player matches the traitor, set curPlayerNotTraitor to false in order to not add a point after this inner loop.
                    if ((user[i].masterController.networkUserInstanceId == user[randomNum[k]].masterController.networkUserInstanceId))
                    {
                        //Log.Info($"Player {PlayerCharacterMasterController.instances[i].GetDisplayName()} is a Traitor, don't add win.");
                        curPlayerNotTraitor = false;
                    }
                }
                if (curPlayerNotTraitor)
                {
                    //Log.Info($"Adding a win to player: " + PlayerCharacterMasterController.instances[i].GetDisplayName());
                    winsAsInnocent[i]++;
                }
                curPlayerNotTraitor = true;
            }
            innoWinMessage += "</color>";
            ChatMessage.Send(innoWinMessage);

            EndRoundWarpToNextStage();
        }

        public void EndRoundWarpToNextStage()
        {
            // Avoid warping to moon2, return to first stages.
            SceneDef def = SceneCatalog.GetSceneDef((SceneIndex)7);
            var next = Run.instance.nextStageScene.baseSceneName;
            Log.Info($"Next Stage Basename: {next}");
            if (roundsPlayed >= (maxRounds - 1))
            {
                ChatMessage.Send("Round limit of " + maxRounds + " reached. Game Over!");
                // Update score at end of last round.
                new SyncScoreString(scoreDataFromServer).Send(NetworkDestination.Clients);
                syncedScore = scoreDataFromServer;
                score = new CustomGUI();
                score.Init();
                // Obliterate at obelisk ending
                Run.instance.BeginGameOver(GameEndingCatalog.gameEndingDefs[3]);
            }
            else
            {
                if (next.Equals("moon2") || next.Equals("golemplains") || next.Equals("golemplains2") || next.Equals("blackbeach") || next.Equals("blackbeach2") || next.Equals("snowyforest"))
                {
                    Log.Info("I just pissed on the moon");
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0: def = SceneCatalog.GetSceneDef((SceneIndex)7); break;
                        case 1: def = SceneCatalog.GetSceneDef((SceneIndex)8); break;
                        case 2: def = SceneCatalog.GetSceneDef((SceneIndex)15); break;
                        case 3: def = SceneCatalog.GetSceneDef((SceneIndex)16); break;
                    }
                    Run.instance.nextStageScene = def;
                    teleport.sceneExitController.destinationScene = def;
                }
                averageItems = new AverageItems();
                averageItems.RedistributeItems();
                teleport.sceneExitController.SetState(SceneExitController.ExitState.ExtractExp);
            }
        }

        private void RoleDisplay()
        {
            if (traitorTeamNames.Equals(""))
            {
                roleDisplay.UpdateTextInnocent();
                roleDisplay.ToggleWindow();
            }
            else
            {
                roleDisplay.UpdateTextTraitor(traitorTeamNames);
                roleDisplay.ToggleWindow();
            }
        }
        //The Update() method is run on every frame of the game.
        private void Update()
        {
            // Scoreboard
            if (PlayerCharacterMasterController.instances.Count != 0 && Input.GetKeyDown(KeyCode.F2))
            {
                // Score already been initialized, hide it and update the text
                score.UpdateTextContent(syncedScore);
                score.ToggleWindow();
            }
            // DISPLAY YOUR ROLE WHEN F3 IS PRESSED
            if (PlayerCharacterMasterController.instances.Count != 0 && Input.GetKeyDown(KeyCode.F3))
            {
                RoleDisplay();
            }
            // DEBUG
            // teleporter exists, the exitstate is extracting EXP, and innos didn't kill all traitors, any innos alive, and send the message only once. DEBUG F5 to skip
            if (NetworkServer.active)
            {
                // lol
                if ((teleport != null && deadTraitorCount < randomNum.Length && teleport.sceneExitController.exitState == SceneExitController.ExitState.ExtractExp && !wonByEliminatingAllTraitors && !sentInnoWinMessageAlready && deadInnoCount < (playerCount - randomNum.Length) && !skippingRound && !traitorWin))
                {
                    InnocentWinByTeleport();
                    sentInnoWinMessageAlready = true;
                    EndRoundWarpToNextStage();
                }
                // DEBUG SKIP ROUND
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    ChatMessage.Send("<color=yellow>Host has forced a skip to the next stage.</color>");
                    EndRoundWarpToNextStage();
                    skippingRound = true;
                }
            }
        }

        private string BuildScoreText() {
            string txt = "";
            for(int i = 0; i < playerCount; i++)
            {
                
                Log.Info($"Building string for index: {i}");
                // FOR SERVER, BUILD ENTIRE STRING FOR EVERY PLAYER
                {
                    // Build the scoretext string
                    var username = NetworkUser.readOnlyInstancesList[i].masterController.GetDisplayName();
                    txt +=
                        "Player " + (i + 1) + ": " + username +  "\n" +
                        "\tInnocent wins: " + winsAsInnocent[i] + "\n" +
                        "\tTraitor wins: " + winsAsTraitor[i] + "\n" +
                        "\tTotal wins: " + (winsAsTraitor[i] + winsAsInnocent[i]) + "\n" +
                        "\tTraitors killed as Innocent: " + killsOnTraitors[i] + "\n" +
                        "\tInnocents killed as Traitor: " + killsOnInnocents[i] + "\n" +
                        "\tTeamkills: " + teamKills[i] + "\n \n";
                    Log.Info($"Finished String For Index: {i}");
                }
            }
            Log.Info("Finished building all strings.");
            return txt;
        }
        private void Stage_onServerStageBegin(Stage stage) {

            playerCount = PlayerCharacterMasterController.instances.Count;
            // If a new game is starting, init certain variables/arrays, then set newGameStart to false
            if (newGameStart) {
                wonByEliminatingAllTraitors = false;
                winsAsInnocent = new int[playerCount];
                winsAsTraitor = new int[playerCount];
                teamKills = new int[playerCount];
                killsOnTraitors = new int[playerCount];
                killsOnInnocents = new int[playerCount];
                roundsPlayed = 0;
                for (int i = 0; i < playerCount; i++)
                {
                    winsAsInnocent[i] = 0;
                    winsAsTraitor[i] = 0;
                    killsOnTraitors[i] = 0;
                    killsOnInnocents[i] = 0;
                    teamKills[i] = 0;
                }
            }
            else
            {
                Run.instance.stageClearCount--;
                roundsPlayed++;
            }

            // Builds Score String
            scoreDataFromServer = BuildScoreText();
            wonByEliminatingAllTraitors = false;
            newGameStart = false;
            innocentWin = false;
            traitorWin = false;
            sentInnoWinMessageAlready = false;
            // Reset all relevant vars each stage.
            deadInnoCount = 0;
            deadTraitorCount = 0;
            skippingRound = false;

            // ignore all of this for singleplayer or if there are too many traitors
            if (playerCount <= numTraitors || numTraitors == 0)
            {
                ChatMessage.Send("<color=red>WARNING!!!</color> <color=orange>There must be at least one Traitor player. Adjust Traitor count with the console command 'a_traitor_count' and restart.</color>");
                //return;
            }
            // Declare random number array to be used x times depending on how many traitors are in the lobby
            randomNum = new int[numTraitors];
            for (int i = 0; i < numTraitors; i++)
            {
                var randomVar = UnityEngine.Random.Range(1, playerCount + 1);    // var to compare to array. Do +1 here and -1 later because the array elements default to 0
                //Log.Info($"Random Var Before Loop: {randomVar}");
                // If there's a match, keep reassigning until a unique var is picked. Skip if less than 2 traitors
                while (Array.Exists(randomNum, num => num == randomVar) && playerCount > 1 && numTraitors > 1)
                {
                    randomVar = UnityEngine.Random.Range(1, playerCount + 1);
                    //Log.Info($"Still inside the loop!: {randomVar}");
                }
                // Log.Info($"Random Var: {randomVar}");
                // Insert randomVar into the array.
                randomNum[i] = randomVar;
            }
            // I wish I wasn't so fucking stupid sometimes
            for (int i = 0; i < randomNum.Length; i++)
            {
                randomNum[i]--;
            }
            Log.Info("Finished assigning Traitors");

            //Log.Info("Beginning to send role messages");
            SendRolesToAllPlayers();
            string howManyTraitors = "There are <color=red>" + randomNum.Length + "</color> Traitor(s).";
            ChatMessage.Send(howManyTraitors);
        }

        private void SendRolesToAllPlayers()
        {
            // Loop for each player to send direct message indicating their role.
            string traitorTextAllies = "<color=yellow>Your team:</color><color=red> ";
            // reset traitor team names for server, use temp variable
            traitorTeamNames = "";
            string tempTraitorTeamNameStorage = "";
            new SyncTraitorTeam(traitorTeamNames);
            for (int i = 0; i < randomNum.Length; i++)
            {
                var user = NetworkUser.readOnlyInstancesList[randomNum[i]];
                traitorTextAllies += user.masterController.GetDisplayName() + " ";
                tempTraitorTeamNameStorage += user.masterController.GetDisplayName() + " ";
                //traitorTextAllies += PlayerCharacterMasterController.instances[randomNum[i]].GetDisplayName() + " "; 
                //tempTraitorTeamNameStorage += PlayerCharacterMasterController.instances[randomNum[i]].GetDisplayName() + " ";
            }
            traitorTextAllies += "</color>";
            Log.Info($"Successfully built traitor team message");
            
            bool isInnoIndex = true;
            for (int i = 0; i < playerCount; i++)
            {
                var user = NetworkUser.readOnlyInstancesList[i];
                NetworkConnection conn = GetNetworkConnectionFromPlayerControllerIndex(i);
                
                // This sucks ass but I need a way to prevent desync between controller index and networkconn index
                Log.Info($"Player character: {i}, Connection ID: {conn.connectionId}");
                for (int j = 0; j < randomNum.Length; j++)
                {
                    if (i == randomNum[j])
                    {
                        Log.Info($"Traitor character: {i}");
                        SendMessageFromStringAndIndex(imposterText, i);
                        // SEND TEAMMATE NAMES TO TRAITORS, index 0 is the host so just do this instead
                        if (i == 0)
                        {
                            traitorTeamNames = tempTraitorTeamNameStorage;
                        }
                        else
                        {
                            new SyncTraitorTeam(tempTraitorTeamNameStorage).Send(conn);
                        }

                        // TEMPORARY FOR DEBUGGING
                        if (randomNum.Length >= 1)
                        {
                            SendMessageFromStringAndIndex(traitorTextAllies, i);
                        }
                        isInnoIndex = false;
                    }
                }
                if (isInnoIndex)
                {
                    SendMessageFromStringAndIndex(innoText, i);
                    // blank text to clear the innos UI
                    if (i != 0)
                        new SyncTraitorTeam("").Send(conn);
                }
                //Log.Info($"i is {i}, after isInnoIndex");
                isInnoIndex = true;
            }
        }
        private void InnocentWinByTeleport()
        {
            innocentWin = true;
            traitorWin = false;
            // Build win string for innocent team
            string innoWinMessage = "<color=green>Innocents win!</color> Traitors: <color=red>";
            for (int k = 0; k < randomNum.Length; k++)
            {
                var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[k]];
                innoWinMessage += traitoruser.masterController.GetDisplayName() + " ";
            }
            // Give each inno player + 1 to their score
            bool curPlayerNotTraitor = true;
            for (int i = 0; i < playerCount; i++)
            {
                var user = NetworkUser.readOnlyInstancesList[i];
                for (int k = 0; k < randomNum.Length; k++)
                {
                    var traitoruser = NetworkUser.readOnlyInstancesList[randomNum[k]];
                    // get each player that wasn't a traitor.
                    // If the player matches the traitor, set curPlayerNotTraitor to false in order to not add a point after this inner loop.
                    if ((user.masterController.networkUserInstanceId == traitoruser.masterController.networkUserInstanceId))
                    {
                        //Log.Info($"Player {PlayerCharacterMasterController.instances[i].GetDisplayName()} is a Traitor, don't add win.");
                        curPlayerNotTraitor = false;
                    }
                }
                if (curPlayerNotTraitor)
                {
                    Log.Info($"Adding a win to player: " + user.masterController.GetDisplayName());
                    winsAsInnocent[i]++;
                }
                curPlayerNotTraitor = true;
            }
            innoWinMessage += "</color>";
            ChatMessage.Send(innoWinMessage);
        }
    }

    // All netcode stuff for sending strings to clients.
    public class SyncScoreString : INetMessage
    {
        string networkSyncScoreString = "Default unsynced";


        public SyncScoreString()
        {

        }

        public SyncScoreString(string newString)
        {
            networkSyncScoreString = newString;
        }

        public void Deserialize(NetworkReader reader)
        {
            RiskOfTraitors.syncedScore = reader.ReadString();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                return;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(networkSyncScoreString);
        }
    }
    public class SyncTraitorTeam : INetMessage
    {
        string traitorTeam = "none";

        public SyncTraitorTeam()
        {

        }

        public SyncTraitorTeam (string traitorTeam)
        {
            this.traitorTeam = traitorTeam;
            //RiskOfTraitors.traitorTeamNames = traitorTeam;
        }

        public void Deserialize(NetworkReader reader)
        {
            RiskOfTraitors.traitorTeamNames = reader.ReadString();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                Log.Info("SyncTraitorTeam: Host ran this. Skip.");
                return;
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(traitorTeam);
        }
    }

    public class SyncPlayerMonsterDamage : INetMessage
    {
        int oldPlayerDamageMul = 0;
        int playerOutgoingDamageMul = 0;
        int oldEnemyDamageMul = 0;
        int enemyOutgoingDamageMul = 0;

        bool updatePlayerMul = false;
        bool updateEnemyMul = false;

        public SyncPlayerMonsterDamage()
        {

        }

        public SyncPlayerMonsterDamage(int oldPlayerMul, int curPlayerMul, int oldEnemyMul, int curEnemyMul, bool updatePlayerMul, bool updateEnemyMul)
        {
            this.oldPlayerDamageMul = oldPlayerMul;
            this.playerOutgoingDamageMul = curPlayerMul;
            this.oldEnemyDamageMul = oldEnemyMul;
            this.enemyOutgoingDamageMul = curEnemyMul;

            this.updatePlayerMul = updatePlayerMul;
            this.updateEnemyMul = updateEnemyMul;
        }

        public void Deserialize(NetworkReader reader)
        {
            RiskOfTraitors.oldPlayerDamageMul = reader.ReadInt32();
            RiskOfTraitors.playerOutgoingDamageMul = reader.ReadInt32();
            RiskOfTraitors.oldEnemyDamageMul = reader.ReadInt32();
            RiskOfTraitors.enemyOutgoingDamageMul = reader.ReadInt32();
            RiskOfTraitors.updatePlayerDamageMul = reader.ReadBoolean();
            RiskOfTraitors.updateMonsterDamageMul = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                Log.Info("SyncTraitorTeam: Host ran this. Skip.");
                return;
            }
            else {
                Log.Info($"Received muls from server: {oldPlayerDamageMul}, {playerOutgoingDamageMul}, {oldEnemyDamageMul}, {enemyOutgoingDamageMul}");
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(oldPlayerDamageMul);
            writer.Write(playerOutgoingDamageMul);
            writer.Write(oldEnemyDamageMul);
            writer.Write(enemyOutgoingDamageMul);
            writer.Write(updatePlayerMul);
            writer.Write(updateEnemyMul);
        }
    }

    public class SyncWinningTeam : INetMessage
    {
        bool didInnocentWin = true;
        string traitorTeam = "";

        public SyncWinningTeam()
        {

        }

        public SyncWinningTeam(bool didInnocentWin, string traitorTeam)
        {
            this.didInnocentWin = didInnocentWin;
            this.traitorTeam = traitorTeam;
        }

        public void Deserialize(NetworkReader reader)
        {
            didInnocentWin = reader.ReadBoolean();
            traitorTeam = reader.ReadString();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                Log.Info("SyncTraitorTeam: Host ran this. Skip.");
                return;
            }

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(didInnocentWin);
            writer.Write(traitorTeam);
        }
    }
}

#pragma warning restore Publicizer001 // Accessing a member that was not originally public