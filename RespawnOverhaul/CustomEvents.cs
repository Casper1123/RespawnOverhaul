using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using Logger = LabApi.Features.Console.Logger;


namespace RespawnOverhaul;

public class CustomEvents : CustomEventsHandler
{
    /// <summary>
    /// Overrides basic behaviour to set spawning tokens to default value.
    /// </summary>
    public override void OnServerWaitingForPlayers()   // OnServerRoundStart()
    {
        int setTokens = 0;
        foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
        {
            RespawnWave wave = RespawnWaves.Get(spawnableWaveBase);
            if (spawnableWaveBase is not ILimitedWave limitedWave || wave is null) continue;
            if (wave is MiniRespawnWave) continue;
            
            // Overriding spawn wave max size to 100% of the spectators. Leave no-one behind.
            // Done regardless of the set tokens in the config.
            wave.MaxWaveSize = Player.ReadyList.Count() > Server.MaxPlayers ? Player.ReadyList.Count() : Server.MaxPlayers;

            int factionTokens = CustomConfig.GetFactionDefaultRespawnTokens(wave.Faction);
            if (factionTokens < 0) continue;
            
            setTokens += factionTokens;
            wave.RespawnTokens = factionTokens;  // Base tokens
            limitedWave.RespawnTokens = factionTokens; // Remaining tokens
            WaveUpdateMessage.ServerSendUpdate(spawnableWaveBase, UpdateMessageFlags.Tokens);
        }
        // Set the amount of respawns left to the total amount of tokens distributed. May not be required
        RespawnTokensManager.AvailableRespawnsLeft = setTokens;
    }

    public override void OnServerRoundStarting(RoundStartingEventArgs ev)
    {
        if (ROPlugin.Instance.Config.MinimumWaveSizePercentage == -1) return;  // Config description states to only do this if a % is set.
        
        foreach (RespawnWave wave in from spawnableWaveBase in WaveManager.Waves let wave = RespawnWaves.Get(spawnableWaveBase) let limitedWave = spawnableWaveBase as ILimitedWave where limitedWave is not null && wave is not null where wave is not MiniRespawnWave select wave)
        {
            // Overriding spawn wave max size to 100% of the spectators. Leave no-one behind.
            // Done regardless of the set tokens in the config.
            wave.MaxWaveSize = Player.ReadyList.Count() > Server.MaxPlayers ? Player.ReadyList.Count() : Server.MaxPlayers;
        }
    }

    public override void OnServerWaveRespawned(WaveRespawnedEventArgs ev)
    {
        // Call delayed to let background things update.
        Timing.CallDelayed(3f, CheckForRemainingRespawns);
    }

    /// <summary>
    /// Checks for remaining respawn tokens and achievable milestones if NoMoreRespawnsNotification = true.
    /// If there aren't any, then CASSIE states so in a broadcast.
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void CheckForRemainingRespawns()
    {
        Logger.Debug("Checking respawn parameters.", ROPlugin.Instance.Config.EnableDebugLogging);
        if (!ROPlugin.Instance.Config.NoMoreRespawnsNotification) return;
        
        Logger.Debug("Checking Milestones", ROPlugin.Instance.Config.EnableDebugLogging);
        // We check if there are any factions with unachieved milestones, return.
        if (RespawnTokensManager.Milestones.Values.Any(milestones => milestones.Any(milestone => !milestone.Achieved)))
        {
            if (!ROPlugin.Instance.Config.EnableDebugLogging) return;
            
            foreach (List<RespawnTokensManager.Milestone> milestones in RespawnTokensManager.Milestones.Values)
            {
                foreach (RespawnTokensManager.Milestone milestone in milestones)
                {
                    Logger.Debug($"\t{milestone.Threshold} | {milestone.Achieved}", ROPlugin.Instance.Config.EnableDebugLogging);
                }
            }

            return;
        }
            
        Logger.Debug("Checking Tokens", ROPlugin.Instance.Config.EnableDebugLogging);
        // If there are any respawn tokens left.
        if (WaveManager.Waves.Select(spawnableWaveBase => spawnableWaveBase as ILimitedWave)
            .Where(limitedWave => limitedWave is not null).Any(limitedWave => limitedWave.RespawnTokens > 0))
        {
            if (!ROPlugin.Instance.Config.EnableDebugLogging) return;
            
            foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
            {
                if (spawnableWaveBase is not ILimitedWave limitedWave) continue;
                if (limitedWave.RespawnTokens <= 0) continue;
                RespawnWave wave = RespawnWaves.Get(spawnableWaveBase);
                if (wave is null) continue;
                
                Logger.Debug($"\t{limitedWave.RespawnTokens} | {spawnableWaveBase.GetType()} | {wave.RespawnTokens} | {wave.GetType()}", ROPlugin.Instance.Config.EnableDebugLogging);
            }
            return;
        }

        Logger.Debug("Attempting to play announcement", ROPlugin.Instance.Config.EnableDebugLogging);
        // No tokens, no milestones. Wait a certain amount of time, and make CASSIE report.
        RespawnEffectsController.PlayCassieAnnouncement("SITE 0 2 ENTRANCE SEAL ACTIVATED ALL TEAM BACKUP RESTRICTED", true, false, true);
        // SURVIVE . FOR THERE IS ONLY YOU LEFT
        // SURVIVE . FOR NO MORE BACKUP IS LEFT
        // SITE 0 2 ENTRANCE SEAL ACTIVATED ALL PERSONNEL BACKUP RESTRICTED
        // SITE 0 2 ENTRANCE SEAL ACTIVATED ALL TEAM BACKUP RESTRICTED
    }

    public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
    {
        if (ev.Wave is MiniRespawnWave && ROPlugin.Instance.Config.DisableMiniWave)
        {
            ev.IsAllowed = false;
            ev.Wave.TimeLeft = 0;
            Logger.Debug($"Prevented Mini-wave {ev.Wave.GetType()} from spawning.", ROPlugin.Instance.Config.EnableDebugLogging);
        }
    }

    [CanBeNull] private RespawnWave _lockedWave;
    public override void OnServerWaveTeamSelecting(WaveTeamSelectingEventArgs ev)
    {
        if (ROPlugin.Instance.Config.MinimumWaveSizePercentage == -1) return; // Return if disabled.
        
        RespawnWave evWave = RespawnWaves.Get(ev.Wave);
        if (evWave is MiniRespawnWave && ROPlugin.Instance.Config.DisableMiniWave)
        {
            ev.IsAllowed = false;
            return;
        }
        
        if (evWave is null)
        {
            Logger.Warn($"RespawnWave Get returned null for SpawnableWaveBase with TargetFaction {ev.Wave.TargetFaction}. THIS WILL BREAK THINGS :)");
            return;
        }

        if (_lockedWave != null && evWave.GetType() != _lockedWave.GetType()) // Equality will probably fumble. Need to get the type of the spawn wave.
        {
            Logger.Debug($"Tossing Respawn attempt for {evWave.GetType()} because it is not the wave that attempted to Respawn first ({_lockedWave.GetType()}.", ROPlugin.Instance.Config.EnableDebugLogging);
            ev.IsAllowed = false;
            evWave.TimeLeft = 0; // Reset the timer.
            return;
        }
        
        // Now it's either the locked wave, or it's not locked. Check regardless if allowed.

        int lobbyCount = Player.ReadyList.Count(p => p.Role == RoleTypeId.Spectator);
        int requiredUsers = ROPlugin.Instance.Config.MinimumWaveSizePercentage * Player.ReadyList.Count(p => p.Team != Team.Flamingos || p.Team != Team.SCPs || p.Team != Team.OtherAlive) / 100; // Non SCP/Mingo players
        
        if (lobbyCount >= requiredUsers)
        {
            Logger.Debug($"Permitting Respawn attempt because {lobbyCount} >= {requiredUsers}. Spawning {evWave.GetType()}", ROPlugin.Instance.Config.EnableDebugLogging);
            _lockedWave = null;
            return;
        }
        
        // If it's not in bounds, discard and set as locked. Then, set its timer to 15s remaining.
        ev.IsAllowed = false;  // Deny selection.
        evWave.TimeLeft = evWave.TimePassed - 15;
        _lockedWave = evWave;
        Logger.Debug(
            $"Tossing Respawn attempt because {lobbyCount} < {requiredUsers}. Not spawning {evWave.GetType()}", ROPlugin.Instance.Config.EnableDebugLogging);
        // Todo: when mini wave spawns, does that reset the main wave timer? Might want to figure out how to stop this many mini-waves from spawning.
        // Cannot assume they are always disabled.
    }
}
