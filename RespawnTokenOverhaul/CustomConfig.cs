using System.Collections.Generic;
using System.ComponentModel;
using PlayerRoles;

namespace RespawnTokenOverhaul;

public class CustomConfig
{
    #region StartingRespawnTokens
    [Description("The starting amount of spawn waves for the Nine Tailed Fox unit. Backup waves not included. Values >= 0")]
    public int NtfStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Chaos Insurgency unit. Backup waves not included. Values >= 0")]
    public int ChaosStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Tutorial team. Backup waves not included.\nThis is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    public int TutorialStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the SCP team. Backup waves not included.\nThis is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    // ReSharper disable once InconsistentNaming
    public int SCPStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Flamingo team. Backup waves not included.\nThis is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    public int FlamingoStartingRespawnTokens { get; set; } = 2;
    #endregion
    
    #region General
    [Description("Makes C.A.S.S.I.E. announce when all possibly achievable respawn tokens have been expended. This happens before the Dead Man's Sequence occurs.")]
    public bool NoMoreRespawnsNotification { get; set; } = true;
    [Description("The tickets required per respawn token for each *vanilla* team (does not alter custom plugin ones). Default: [30, 80, 150, 200]")]
    public List<int> NtfMilestones { get; set; } = [30, 80, 150, 200];
    public List<int> ChaosMilestones { get; set; } = [30, 80, 150, 200];
    #endregion
    
    /// <summary>
    /// Gets the configured starting Respawn Tokens for supported teams.
    /// </summary>
    /// <param name="faction">The faction of the team you are looking for.</param>
    /// <returns>Configured starting Respawn Tokens, -1 if team is not supported.</returns>
    public static int GetFactionDefaultRespawnTokens(Faction faction)
    {
        return faction switch
        {
            Faction.FoundationStaff => RSTPlugin.Instance.Config.NtfStartingRespawnTokens,
            Faction.FoundationEnemy => RSTPlugin.Instance.Config.ChaosStartingRespawnTokens,
            Faction.Unclassified => RSTPlugin.Instance.Config.TutorialStartingRespawnTokens,
            Faction.SCP => RSTPlugin.Instance.Config.SCPStartingRespawnTokens,
            Faction.Flamingos => RSTPlugin.Instance.Config.FlamingoStartingRespawnTokens,
            _ => -1
        };
    }

    public bool ValidConfiguration()
    {
        return ChaosStartingRespawnTokens >= 0 && NtfStartingRespawnTokens >= 0 && TutorialStartingRespawnTokens >= 0 && SCPStartingRespawnTokens >= 0 && FlamingoStartingRespawnTokens >= 0;
    }
}