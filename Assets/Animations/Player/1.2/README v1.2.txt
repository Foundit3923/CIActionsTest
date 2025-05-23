Player Animations v1.2 (made 3/7/25)
- Made for Player v2 model

——————————————————————————————————————

KEY:
* = New Additions
[x] = Range/Values Possible
SSM = Sub-State Machine

——————————————————————————————————————

Animations in this file:
- P_Crouch Idle
- P_Crouch Walk
- P_Jump
- P_Jump Backwards
- P_Jump Forward
- P_-NG_Attack
- P_NG_AttackToFly
- P_NG_Fly
- P_Sjena Death
- P_Stand Idle
- P_Stand Run
- P_Stand Run Backwards
- P_Stand Walk
- P_Stand Walk Backwards
- PZ_Attack
- PZ_Chase
- PZ-_Convert
- PZ_Death
- PZ_Idle
- PZ_Patrol
- *P_Cy_Attacked
- *P_Cy_Chestburst

——————————————————————————————————————

Parameters:
- RESET: Used to force reset to default idle state
- Crouched: Used to specify when the player is crouched
- Speed State [0-2]: Used to indicate what speed the player is moving (idle, walk, run)
- Jump: Used to trigger "Jump" animations
- Convert: Used to trigger "Lanternhead Zombification" conversion animations
- Zombie Attack: Available only to "Zombified" player, used to trigger "Attack" animations
- Kill: Used to trigger "Death" animations.
- *Cy_Attacked: Used to trigger "Cymothoa AttackSmall/Enter Mouth" animations. Does not kill the player.
- Death: Used to signify player death in controller. Only used to prevent looping/resetting of death animations.
- L_Zombified: Used to specify when the player is zombified. Allows for "Zombie Locomotion/Attack/Death" animations.
- S_Chosen: Used to specify when the player has been chosen by a "Sjena". Allows for "Sjena Death" animations.
- NG_Chosen: Used to specify when the player has been chosen by a "Night Gaunt". Allows for "Night Gaunt Death" animations.
- *Cy_Chosen: Used to specify when the player has been chosen by the "Cymothoa". Allows for both "Cymothoa Mouth/Cy_Attacked" animations, as well as "Cymothoa Death/Cy_Chestburst" animations.

——————————————————————————————————————

Changelog 3/7/25:
- Added "Cymothoa Mouth" SSM
- Added "Deaths/Killed by Cymothoa" SSM
- Added "Cy_Attacked" trigger parameter
- Added "Cy_Chosen" boolean parameter
- Adjusted "Deaths" SSM's so they return to the "Idle" state when finishing, or when the "Death" parameter is "False". Bug occurred where it would default to "PZ_Death" after a death, even when there was no reason to.

Changelog 2/21/25:
- Moved "Zombie" animations into a "Player Zombie" SSM
- Added "Deaths" SSM, Included "Night Gaunt Attack" animations
- Added "Deaths/Killed by Night Gaunt" SSM
- Moved "Sjena Death" animations into "Deaths/Killed by Sjena" SSM
- Moved "PZ Death" animations into "Deaths/Zombified, Killed" SSM
- Replaced "Player Zombie" parameter with "L_Zombified"
- Replaced "Sjena Attached" parameter with "S_Chosen"
- Added "RESET" parameter. Included as a "force-reset" for the animation controller in case something goes wrong.
- Added "NG_Chosen" parameter
- Added "Kill" parameter. Replaces the "Death" parameter only for when triggering a "death" animation.