using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace RespawnTokenOverhaul;

public class CustomEventModifications : CustomEventsHandler
{
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

        if (!RSTPlugin.Instance.Config.NoMoreRespawnsNotification) return;
        
        // We check if there are any factions with unachieved milestones, return.
        if (RespawnTokensManager.Milestones.Values.Any(milestones => milestones.Any(milestone => !milestone.Achieved)))
            return;
        
        // If there are any respawn tokens left.
        if (WaveManager.Waves.Select(spawnableWaveBase => spawnableWaveBase as ILimitedWave)
            .Where(limitedWave => limitedWave is not null).Any(limitedWave => limitedWave.RespawnTokens > 0)) return;

        // No tokens, no milestones. Wait a certain amount of time, and make CASSIE report.
        RespawnEffectsController.PlayCassieAnnouncement("SURVIVE . FOR THERE IS ONLY YOU LEFT", true, false, true);
    }
}
