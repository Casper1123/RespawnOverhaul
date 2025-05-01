using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
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
            ILimitedWave limitedWave = spawnableWaveBase as ILimitedWave;
            if (limitedWave is null || wave is null) continue;
            if (wave is MiniRespawnWave) continue;

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
    
    public override void OnServerWaveRespawned(WaveRespawnedEventArgs ev)
    {
        base.OnServerWaveRespawned(ev);

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
}
