using System.Collections.Generic;
using System.ComponentModel;
using PlayerRoles;

namespace RespawnOverhaul;

public class CustomConfig
{
    #region StartingRespawnTokens
    [Description("The starting amount of spawn waves for the Nine Tailed Fox unit. Backup waves not included. Values >= 0")]
    public int NtfStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Chaos Insurgency unit. Backup waves not included. Values >= 0")]
    public int ChaosStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Tutorial team. Backup waves not included.\n# This is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    public int TutorialStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the SCP team. Backup waves not included.\n# This is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    // ReSharper disable once InconsistentNaming
    public int SCPStartingRespawnTokens { get; set; } = 2;
    [Description("The starting amount of spawn waves for the Flamingo team. Backup waves not included.\n# This is here to be compatible with plugins, as they cannot spawn naturally. Values >= 0")]
    public int FlamingoStartingRespawnTokens { get; set; } = 2;
    #endregion
    
    #region General
    [Description("Makes C.A.S.S.I.E. announce when all possibly achievable respawn tokens have been expended. This happens before the Dead Man's Sequence occurs.")]
    public bool NoMoreRespawnsNotification { get; set; } = true;
    [Description("The tickets required per respawn token for each *vanilla* team (does not alter custom plugin ones). Default: [30, 80, 150, 200].\n# put in -1 to disable for this team.")]
    public List<int> NtfMilestones { get; set; } = [30, 80, 150, 200];
    public List<int> ChaosMilestones { get; set; } = [30, 80, 150, 200];
    // Todo: Display the next milestone as a Hint on the screen.
    [Description("The percentage of the server population that must be included in the spawn.\n# Automatically sets max wave size to all spectators.\n# range 0.0 - 1.0, but 1.0 will prevent spawns.\n# Can be -1 to disable this functionality.")]
    public int MinimumWaveSizePercentage { get; set; } = 60;
    [Description("Prints debug information to the server console.")]
    public bool EnableDebugLogging { get; set; } = false;
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
            Faction.FoundationStaff => ROPlugin.Instance.Config.NtfStartingRespawnTokens,
            Faction.FoundationEnemy => ROPlugin.Instance.Config.ChaosStartingRespawnTokens,
            Faction.Unclassified => ROPlugin.Instance.Config.TutorialStartingRespawnTokens,
            Faction.SCP => ROPlugin.Instance.Config.SCPStartingRespawnTokens,
            Faction.Flamingos => ROPlugin.Instance.Config.FlamingoStartingRespawnTokens,
            _ => -1
        };
    }

    /// <summary>
    /// Checks if the loaded configuration is valid (no spawn tokens below 0).
    /// </summary>
    /// <returns>Configuration validity.</returns>
    public bool ValidConfiguration()
    {
        // todo: rewrite verification.
        return ChaosStartingRespawnTokens >= -1 && NtfStartingRespawnTokens >= -1 && TutorialStartingRespawnTokens >= -1 && SCPStartingRespawnTokens >= -1 && FlamingoStartingRespawnTokens >= -1;
    }
}