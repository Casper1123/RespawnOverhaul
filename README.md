# RespawnTokenOverhaul
This is a plugin that aims to give more control about Respawn tokens and the Ticket system to server owners.
This runs on LabAPI and as such requires versions starting from 14.0.

# Configuration
When on the spectator screen, you'll see a bar for each team on the top, with a number besides each vanilla team icon.
![respawn-screen](./readme-files/respawn-screen.png)
The number displayed on either outside is the remaining number of Respawn Tokens; the amount of respawns that team still has. This plugin allows you to change the following: <BR>
- The tokens each team starts with.
- The amount of 'tickets' required to obtain a new ticket.


### Starting Respawn Tokens
The starting respawn tokens can be configured for each Faction. This **does** modify the token values for non-mini wave plugin spawnwaves, so keep that in mind (and is thus compatible with Serpents Hand plugins).
If one of these plugin waves do not exist, their configuration doesn't matter.
***All default token values must be `>= 0`.***
Each listed faction in the Config aligns to the team they win with, and most are there for compatibility reasons. Defaults to `2`.
The change in tokens will be reflected visually, like below.
![changed-tokens](./readme-files/changed-tokens.png)
You can also see them in the Remote Admin tool under Round & Events > Tokens.

### NoMoreRespawnNotification
Contrary to what the title suggests, instead of removing a notification it *adds* one.
When enabled, allows C.A.S.S.I.E. to broadcast an announcement once all Tokens and Milestones have been depleted. Defaults to `true`.

### NtfMilestones & ChaosMilestones
Both of these are a list of numbers, the amount of numbers input being the amount of milestones (so 4 numbers means 4 milestones to be achievable). The actual number input is the milestone threshold that needs to be cleared for the token to be given to the corresponding team. The ticket system is a little complicated, so I won't go over it right now; it just happens in the background and the speed of obtaining tickets can be edited in the `gameplay_config` file of the server.
Defaults to `[30, 80, 150, 200]` giving 4 milestones with their corresponding ticket count each.
Each milestone value should be `>=0` to be added, and to disable the milestone system just input `-1`.

**Important:** these milestones are only reflected server side, and the ticket requirement on the UI won't update. Whilst not implemented, a workaround for this is being thought about, so be patient (or contribute!).