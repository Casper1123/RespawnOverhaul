using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using Respawning;


namespace RespawnOverhaul;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once InconsistentNaming
public class ROPlugin : Plugin<CustomConfig>
{
    public override string Name { get; } = "RespawnOverhaul";
    public override string Description { get; } = "Configurably changes team respawn behaviour.";
    public override string Author { get; } = "Casper1123";
    public override Version Version { get; } = new(1, 2, 0);
    public override string ConfigFileName { get; set; } = "config.yml";
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    
    public new CustomConfig Config => base.Config!;
    private CustomEvents Events { get; } = new();
    public static ROPlugin Instance { get; private set; }

    // Entry point override
    public override void Enable()
    {
        if (!Config.ValidConfiguration())
        {
            Logger.Error("Configuration is invalid. Please see required input value ranges in config.yaml");
            return;
        }

        Instance = this;

        Logger.Debug("Attaching custom events handler.", Instance.Config.EnableDebugLogging);
        CustomHandlersManager.RegisterEventsHandler(Events);

        Logger.Debug("Modifying vanilla spawn milestones.", Instance.Config.EnableDebugLogging);
        SetWaveMilestones(Faction.FoundationStaff, Config.NtfMilestones);
        SetWaveMilestones(Faction.FoundationEnemy, Config.ChaosMilestones);
    }

    // Exit point override
    public override void Disable()
    {
        Logger.Debug("Detaching custom events handler.", Instance.Config.EnableDebugLogging);
        CustomHandlersManager.UnregisterEventsHandler(Events);
        Logger.Debug("Resetting vanilla spawn milestones.", Instance.Config.EnableDebugLogging);
        SetWaveMilestones(Faction.FoundationStaff, DefaultWaveMilestoneList);
        SetWaveMilestones(Faction.FoundationEnemy, DefaultWaveMilestoneList);
    }

    private static List<int> DefaultWaveMilestoneList { get; } = [30, 80, 150, 200];

    /// <summary>
    /// Modifies the Milestones for the passed Faction, replacing them with passed milestone targets instead.
    /// </summary>
    /// <param name="faction">The faction to modify</param>
    /// <param name="milestones">The milestones to set to.</param>
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void SetWaveMilestones(Faction faction, List<int> milestones)
    {
        // Get list of milestones, clear it and add our own.
        List<RespawnTokensManager.Milestone> currentMilestones;
        try
        {
            currentMilestones = RespawnTokensManager.Milestones[faction];
        }
        catch (Exception)
        {
            return;
        }
        // Empty catch clause is fine because let's be real here, if we can't do a faction fetch from this there's no entry to alter and...
        //              I can't be asked to make one.
        // If it's not a vanilla entry then it might not be in there, at which point the original author is allowed to modify this themselves.
        // Vanilla should always be present.

        currentMilestones.Clear();
        currentMilestones.AddRange(from milestone in milestones
            where milestone >= 0
            select new RespawnTokensManager.Milestone(milestone));
    }
    // What todo with the displaying things:
    // When a player dies, display a hint on their screen, which does not expire. Should contain tokens required for next milestone, as well as the next Milestone token goal.
    // When a player's role changes from Spectator to something else, remove it.
    // When a player's role changes to Spectator, set it.
    // When the ticket goal is achieved, update the Hint for all Spectators.
    // Position: Below each team's ticket bar, additional Milestones
}