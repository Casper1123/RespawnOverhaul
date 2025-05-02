using System;
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

namespace RespawnTokenOverhaul;

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
            wave.MaxWaveSize = Player.List.Count > Server.MaxPlayers ? Player.List.Count : Server.MaxPlayers;

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
        foreach (RespawnWave wave in from spawnableWaveBase in WaveManager.Waves let wave = RespawnWaves.Get(spawnableWaveBase) let limitedWave = spawnableWaveBase as ILimitedWave where limitedWave is not null && wave is not null where wave is not MiniRespawnWave select wave)
        {
            // Overriding spawn wave max size to 100% of the spectators. Leave no-one behind.
            // Done regardless of the set tokens in the config.
            wave.MaxWaveSize = Player.List.Count > Server.MaxPlayers ? Player.List.Count : Server.MaxPlayers;
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
        Logger.Debug("Checking respawn parameters.", RTOPlugin.Instance.Config.EnableDebugLogging);
        if (!RTOPlugin.Instance.Config.NoMoreRespawnsNotification) return;
        
        Logger.Debug("Checking Milestones", RTOPlugin.Instance.Config.EnableDebugLogging);
        // We check if there are any factions with unachieved milestones, return.
        if (RespawnTokensManager.Milestones.Values.Any(milestones => milestones.Any(milestone => !milestone.Achieved)))
        {
            if (!RTOPlugin.Instance.Config.EnableDebugLogging) return;
            
            foreach (List<RespawnTokensManager.Milestone> milestones in RespawnTokensManager.Milestones.Values)
            {
                foreach (RespawnTokensManager.Milestone milestone in milestones)
                {
                    Logger.Debug($"\t{milestone.Threshold} | {milestone.Achieved}", RTOPlugin.Instance.Config.EnableDebugLogging);
                }
            }

            return;
        }
            
        Logger.Debug("Checking Tokens", RTOPlugin.Instance.Config.EnableDebugLogging);
        // If there are any respawn tokens left.
        if (WaveManager.Waves.Select(spawnableWaveBase => spawnableWaveBase as ILimitedWave)
            .Where(limitedWave => limitedWave is not null).Any(limitedWave => limitedWave.RespawnTokens > 0))
        {
            if (!RTOPlugin.Instance.Config.EnableDebugLogging) return;
            
            foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
            {
                if (spawnableWaveBase is not ILimitedWave limitedWave) continue;
                if (limitedWave.RespawnTokens <= 0) continue;
                RespawnWave wave = RespawnWaves.Get(spawnableWaveBase);
                if (wave is null) continue;
                
                Logger.Debug($"\t{limitedWave.RespawnTokens} | {spawnableWaveBase.GetType()} | {wave.RespawnTokens} | {wave.GetType()}", RTOPlugin.Instance.Config.EnableDebugLogging);
            }
            return;
        }

        Logger.Debug("Attempting to play announcement", RTOPlugin.Instance.Config.EnableDebugLogging);
        // No tokens, no milestones. Wait a certain amount of time, and make CASSIE report.
        RespawnEffectsController.PlayCassieAnnouncement("SITE 0 2 ENTRANCE SEAL ACTIVATED ALL TEAM BACKUP RESTRICTED", true, false, true);
        // SURVIVE . FOR THERE IS ONLY YOU LEFT
        // SURVIVE . FOR NO MORE BACKUP IS LEFT
        // SITE 0 2 ENTRANCE SEAL ACTIVATED ALL PERSONNEL BACKUP RESTRICTED
        // SITE 0 2 ENTRANCE SEAL ACTIVATED ALL TEAM BACKUP RESTRICTED
    }

    public void old_OnServerWaveRespawning(WaveRespawningEventArgs ev)  // Here to keep the code around for now, used to be in the spot of OnServerWaveTeamSelecting.
    {
        if (ev.Wave is MiniRespawnWave) return;
        
        if (RTOPlugin.Instance.Config.MinimumWaveSizePercentage == -1) return; // Return if disabled.

        int requiredUsers = RTOPlugin.Instance.Config.MinimumWaveSizePercentage * Player.List.Count / 100;
        
        if (ev.SpawningPlayers.Count() < requiredUsers)
        {
            Logger.Debug($"Permitting Respawn attempt because {ev.SpawningPlayers.Count()} >= {requiredUsers}.", RTOPlugin.Instance.Config.EnableDebugLogging);
            return;
        }
        
        ev.IsAllowed = false;
        ev.Wave.RespawnTokens++;  // Give the token back.
        Logger.Debug(
            $"Tossing Respawn attempt because {ev.SpawningPlayers.Count()} < {requiredUsers}.", RTOPlugin.Instance.Config.EnableDebugLogging);
    }

    private bool _spawnsLocked;
    public override void OnServerWaveTeamSelecting(WaveTeamSelectingEventArgs ev)
    {
        if (RTOPlugin.Instance.Config.MinimumWaveSizePercentage == -1) return; // Return if disabled.

        int lobbyCount = Player.List.Count(p => p.Role == RoleTypeId.Spectator);
        int requiredUsers = RTOPlugin.Instance.Config.MinimumWaveSizePercentage * Player.List.Count / 100;
        
        if (lobbyCount < requiredUsers)
        {
            Logger.Debug($"Permitting Respawn attempt because {lobbyCount} >= {requiredUsers}.", RTOPlugin.Instance.Config.EnableDebugLogging);
            if (!_spawnsLocked) return;
            
            Logger.Debug("Unpausing timers. WARNING: MAY OVERRIDE PAUSES FROM OTHER PLUGINS. If this causes issues, contact the Developer.", RTOPlugin.Instance.Config.EnableDebugLogging);
            foreach (RespawnWave wave in WaveManager.Waves.Select(spawnableWaveBase => RespawnWaves.Get(spawnableWaveBase)).Where(wave => wave is not null))
            {
                wave.PausedTime = 0;
            }

            _spawnsLocked = false;
            
            return;
        }
        
        // Steps for if it's not permitted:
        // Pause all others
        // Set self to low timer
        // Wait out low timer until spawns.
        // Reset pauses to 0 if permitted.
        
        // Todo: pause timers whilst selecting, force only this team to spawn because it was selected first.
        // Accept that tokens for further waves will be able to be influenced in the mean time.
        // See if any mini waves can still spawn when this is prevented. -> unless you pause their timers.
        // Once it's successfully spawned a wave, unpause the timers.
        ev.IsAllowed = false;  // Deny selection.
        Logger.Debug(
            $"Tossing Respawn attempt because {lobbyCount} < {requiredUsers}.", RTOPlugin.Instance.Config.EnableDebugLogging);
        
        RespawnWave evWave = RespawnWaves.Get(ev.Wave);
        if (evWave is null) return;
        
        Logger.Debug("Pausing all other team timers.", RTOPlugin.Instance.Config.EnableDebugLogging);
        foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
        {
            RespawnWave wave = RespawnWaves.Get(spawnableWaveBase);
            if (wave is null)
            {
                Logger.Debug($"\tRespawnWave {spawnableWaveBase.TargetFaction} | {spawnableWaveBase.GetType()} is not a RespawnWave.");
                continue;
            }
            wave.PausedTime = 35; // Should be enough time, no?
        }
        evWave.PausedTime = 30;  // Will try again in 30 seconds.
        _spawnsLocked = true;
    }
}
