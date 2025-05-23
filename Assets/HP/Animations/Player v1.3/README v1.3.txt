Player Animations v1.3 (made 3/27/25)
- Made for Player v2 model

——————————————————————————————————————

KEY:
** = New or Updated
[x] = Range/Values/ID's
SSM = Sub-State Machine

——————————————————————————————————————

= = = = = = = = = BEFORE YOU IMPORT THE UNITYPACKAGE = = = = = = = = =

Please make sure to add the latest version of "Animation Rigging" to your unity project via. Unity's Package manager. It's required for the new IK animations to work.

——————————————————————————————————————

PREFAB?

- ** Included is a prefab called "Player v2". This is an example of how I've setup the player for animations.
  - There's an object called "RigContainer" on the prefab that should be attached to the actual player. See in the "NOTES ON HOW STUFF WORKS", "Arm IK Info" section on how to attach everything to it.
  - I'd recommend against actually using it, as well as the player fbx, as there's updated versions that should (hopefully) work, as long as the "RigContainer" is attached properly.
  - Keep in mind that the prefab is scaled at 0.2x size.

——————————————————————————————————————

UNITYPACKAGE INSTEAD OF .ZIP?

- ** Due to errors where the animation controller had their animations decouple from it after unpacking the .zip, I'm now uploading this as a .unitypackage. That will make sure the animation controller has the animations properly attached.
  - However, this also means that the location of the files will be imported into a specific spot. To make it easier, I'll be numbering the files to make moving them around in other projects easier.
  - The animation files are located in: "Assets/HP/Animations/Player v[#]/...", and the model file is located in: "Assets/HP/Meshes-FBX/...". You can move them wherever you need.
  - When I make updates to the animations, I'll be renaming the "Player v[#]" to coorespond with the version number, just so that there's no conflicts with other versions. If there are (like unity warning about overriding existing files with imports, please let me know)

——————————————————————————————————————

ANIMATIONS IN THIS PACK:

- ** ArmIKPosition_P_CrouchIdle
- ** ArmIKPosition_P_CrouchWalkBack
- ** ArmIKPosition_P_CrouchWalkForward
- ** ArmIKPosition_P_CrouchWalkLeft
- ** ArmIKPosition_P_CrouchWalkRight
- ** ArmIKPosition_P_JumpBack
- ** ArmIKPosition_P_JumpForward
- ** ArmIKPosition_P_JumpIn
- ** ArmIKPosition_P_JumpLoop
- ** ArmIKPosition_P_JumpOut
- ** ArmIKPosition_P_Push
- ** ArmIKPosition_P_StandIdle
- ** ArmIKPosition_P_StandRunBack
- ** ArmIKPosition_P_StandRunForward
- ** ArmIKPosition_P_StandRunLeft
- ** ArmIKPosition_P_StandRunRight
- ** ArmIKPosition_P_StandWalkBack
- ** ArmIKPosition_P_StandWalkForward
- ** ArmIKPosition_P_StandWalkLeft
- ** ArmIKPosition_P_StandWalkRight
- ** ArmIKRotation_P_HoldItem_A
- ** ArmIKRotation_P_HoldItem_B
- ** ArmIKRotation_P_HoldItem_C
- ** ArmIKRotation_P_HoldItem_D
- ** ArmIKRotation_P_HoldItem_E
- ** ArmIKRotation_P_HoldItem_SmallPlayer
- ** ArmIKRotation_P_PickupHover
- ** ArmIKRotation_P_PickupSwipe
- ** ArmIK_DefaultWeight
- ** P_CrouchIdle
- ** P_CrouchPush
- ** P_CrouchWalkBack
- ** P_CrouchWalkForward
- ** P_CrouchWalkLeft
- ** P_CrouchWalkRight
- ** P_Cy_Attacked
- ** P_Cy_Chestburst
- ** P_HandSize
- ** P_HoldItem_A
- ** P_HoldItem_B
- ** P_HoldItem_C
- ** P_HoldItem_D
- ** P_HoldItem_E
- ** P_HoldItem_SmallPlayer
- ** P_JumpBack
- ** P_JumpForward
- ** P_JumpIn
- ** P_JumpLoop
- ** P_JumpOut
- ** P_NG_Attack
- ** P_NG_AttackToFly
- ** P_NG_Fly
- ** P_PickupHover
- ** P_PickupSwipe
- ** P_Shove
- ** P_Sjena_Death
- ** P_SmallPlayer_PickedUp
- ** P_StandIdle
- ** P_StandPush
- ** P_StandRunBack
- ** P_StandRunForward
- ** P_StandRunLeft
- ** P_StandRunRight
- ** P_StandWalkBack
- ** P_StandWalkForward
- ** P_StandWalkLeft
- ** P_StandWalkRight
- ** P_Zom_Attack
- ** P_Zom_Chase
- ** P_Zom_Convert
- ** P_Zom_Death
- ** P_Zom_Idle
- ** P_Zom_Patrol
- ** test_rotateplayer_90

——————————————————————————————————————

PARAMETERS:

- ** DEV_RESET: Used to force reset to default idle state
- ** Always_Active: Used to allow specific state transition conditions that otherwise would not. Needs to stay active.
- ** Speed_Modifier: Used to control the speed of the locomotion animations by multiplying them (ex. [animation speed = 1] * Speed_Modifier).
- ** Speed State: REMOVED
- ** Move_X: Used to indicate the direction of the player's movement along the X axis (left and right for the player)
- ** Move_Y: Used to indicate the direction of the player's movement along the Y axis (forward & back for the player)
- ** Arms_State: Used to trigger arm animations on the Left & Right arms.
- ** Chosen_State: Replaces "L_Zombified", "S_Chosen", "NG_Chosen", "Cy_Chosen". Used to specify what monster has chosen the player. Allows for various monster attack & kill animations
- Crouched: Used to specify when the player is crouched
- ** Grounded: Used to specify when the player is grounded. On its own, used to trigger falling animations. In conjunction with the "Jump" trigger, used to trigger "Jump" animations.
- Jump: Used to trigger "Jump" animations
- ** Push: Used to trigger "Push" animations" while crouched or standing
- ** P_Zom_Attack: (Previously "Zombie Attack") Available only to "Zombified" player, used to trigger "Attack" animation
- Cy_Attacked: Used to trigger "Cymothoa AttackSmall/Enter Mouth" animations. Does not kill the player.
- ** Cy_InBody: Used to signify small Cymothoa in player body. Used to allow death by Cymothoa chestburst.
- ** L_Convert: (Previously "Convert") Used to trigger "Lanternhead Zombification" conversion animations
- ** Kill_Player: (Previously "Kill") Used to trigger "Death" animations.
- ** Is_Dead (Previously "Death") Used to signify player death in controller. Only used to prevent looping/resetting of death animations.
- ** L_Zombified: REMOVED
- ** S_Chosen: REMOVED
- ** NG_Chosen: REMOVED
- ** Cy_Chosen: REMOVED

——————————————————————————————————————

NOTES ON HOW STUFF WORKS:

- ** Player Locomotion:
  - Locomotion is found in the "Base Layer/Locomotion Normal" and "Base Layer/Player Zombified" SSM's.
  - Locomotion Normal:
    - This is the generic locomotion SSM, and controls the movement animations of the player.
    - This SSM is controlled by a few parameters:
      - Move_X
      - Move_Y
      - Crouched
      - Grounded
      - Jump
    - The machine is built to transition to any state in any situation, depending on the combination of parameters.
  - Player Zombified:
    - This is the SSM that control the locomotion of the "Zombified" player, and is very simplistic.
    - This is transitioned into, and is only active when the player is zombified.
    - It uses only "Move_Y" to control the animations. More info below.
  - The direction of the locomotion is based on the "Move_X" and "Move_Y" float values:
    - Move_X [-2.0] = Left (Run)
    - Move_X [-1.0] = Left (Walk)
    - Move_X [0.0] = Idle
    - Move_X [1.0] = Right (Walk)
    - Move_X [2.0] = Right (Run)
    - Move_Y [-2.0] = Back (Run)
    - Move_Y [-1.0] = Back (Walk)
    - Move_Y [0.0] = Idle
    - Move_Y [1.0] = Forward (Walk)
    - Move_Y [2.0] = Forward (Run)
  - "Standing" uses "Crouched = false", and has a range of [-2, 2] in both axes
  - "Crouched" uses "Crouched = true", and has a range of [-1, 1] in both axes
  - "Player Zombie" uses "Chosen_State = 1", and has a range of [0, 1] only on "Move_Y"

- ** Player Jumping:
  - Split into three jump animations, "JumpIn" (player starts the jump), "JumpLoop" (when player is in midair), "JumpOut" (player falls and returns to idle)
  - Uses the new "Grounded" state to specify when the "Jump" or "JumpLoop/Fall" animations will play. It follows this progression:
    - If player falls off ledge or is midair for reasons other than "player-initiated jump", "Grounded" state is false, but jump doesn't happen. Instead, it skips to "JumpLoop" until the "Grounded" state is true, transitioning to "JumpOut".
    - Else, if player initiates a jump, "Jump" trigger is activated, "Grounded" state is false, and "JumpIn", followed by "JumpLoop" is played. "JumpOut" only plays when "Grounded" state returns true.
    - If the player is moving beyond [0.5] or [-0.5] on "Move_Y" & jumps, a special "JumpForward" or "JumpBack" animation will play, transitioning into the "JumpLoop" & "JumpOut" states. Otherwise, it'll just use the "JumpIn" state.

- ** Arm Pickups & States:
  - Arms animations are overridden when these happen:
    - Player picks up an object/player
    - Player hovers over an object/player
    - Player is holding an object/player
  - When doing so, it uses an IK system to force the hands in a specific position in front of the player. This is done with a few combined methods:
    - The IK is controlled using an Unity plugin called "Animation Rigging". The scripts are located on the player on an object called "RigContainer" and the children of the object as well.
    - Weights of the IK (how much the script controls the arm/hand positions) are controlled dynamically through the animation controller
    - Layer "Default Arms IK Weight" is a "write-default safety" layer, that specifies the default weights of the IK rigs
    - Layer "Right Arm Animations Override":
      - It overrides the right arm animations, replacing them with the pickup animations. 
      - It transitions between them using the "Arms State" integer.
      - When the integer is 0, it uses an "Idle" state with no animation, instead using the "Default Arms IK Weight" layer's IK weights.
    - Layer "Left Arm Animations Override":
      - It overrides the left arm animations.
      - It's the same system as the "Right Arm Animations Override" layer, but having a few animations removed, as the left arm is only used for a few animations.
      - Otherwise it uses a state that has no animation, instead using the default IK weight.
    - Layer "Arm IK Rotations":
      - This layer sets the rotations of the IK targets on the player.
      - This layer is synced to the "Right Arm Animations Override", but affects both arms.
      - This makes it so that it plays at the exact same time as the other layers.
      - It uses separate animations that affect only the rotation of the IK targets.
    - Layer "Arm IK Positions":
      - This layer sets the positions of the IK targets on the player.
      - This layer is not synced to the "Base Layer", but is a copied version of the layer.
      - This layer uses separate animations that affect the position of the IK targets, split up by what locomotion animation is currently playing.
      - In situations that aren't using locomotion, like Death, Monster attacks, or Zombified player animations, the pickups wouldn't be happening, so it doesn't do anything.
  - These are the current ID's for the parameter, and what they do:
    - [0]: Idle, uses default Base Layer animations
    - [1]: Right arm only, Hold item, small flat
    - [2]: Right arm only, Hold item, small round
    - [3]: Right arm only, Hold item, cylinder/flashlight
    - [4]: Both arms, Hold item, small
    - [5]: Both arms, Hold item, large
    - [6]: Right arm only, Hold Small Player
    - [7]: Right arm only, Hover hand for item pickup
    - [8]: Right arm only, Swipe hand for item pickup
  - This scenario should be taking place when an item is picked up by the player:
    - Player's camera hovers over a pickup-able item. Trigger ID [7]: Hand now hovers in front of player.
    - Player picks up object. Trigger ID [8]: Hand swipes in front, picking up object.
    - Trigger ID [1-6]: Depending on object, transition into holding said item.
    - If player drops item, Trigger ID [0]: Return to neutral state.

- ** Arm IK Information:
  - The player now uses IK on the arms & elbows for various pickup, push, & attack animations.
  - The plugin/package NEEDED for this to work is "Animation Rigging", a built-in unity package that can be downloaded form Unity's Package Manager.
  - There is a parent object on the player gameObject called "RigContainer". Inside contains the scripts, targets, & rigs to make the arms have proper IK. These objects NEED TO STAY AS A CHILD OF THE ROOT OF THE AVATAR (Player v2/Rig Container), otherwise the animations that use the IK system will break. Here is a breakdown on how those objects are setup:
    - RigContainer: Contains a required script called "Rig".
    - ArmRigRight: Uses a "Two Bone IK Constraint" script that determines what bones & targets are in the IK system. This is the setup that should be followed to make sure the rig works:
      - Root = mixamorig:RightArm
      - Mid = mixamorig:RightForeArm
      - Tip = mixamorig:RightHand
      - Target = HandTargetR
      - Hint = ElbowTargetR
    - ArmRigLeft: Like above, however built for the left arm:
      - Root = mixamorig:LeftArm
      - Mid = mixamorig:LeftForeArm
      - Tip = mixamorig:LeftHand
      - Target = HandTargetL
      - Hint = ElbowTargetL
    - HandTargetR, HandTargetL, ElbowTargetR, ElbowTargetL: These are empty gameObjects that are used in the IK system for the hand & elbow positions. They are animated dynamically using the player animation controller.
  - When the IK is not being used, the weight of "ArmRigRight" and "ArmRigLeft" are set to 0 or 0.001 (basically nothing)

- ** Zombie Attack:
  - The "Zombie Attack" layer controls the attack animation for the zombified player, and only happens when:
    - "Chosen_State" is 1 (aka. the player is a zombie)
    - "PZ_Attack" is triggered (zombie attacks)
    - "Is_Dead" is false

- ** Pushing:
  - The "Push Override" layer controls the pushing animation, and has a few things happen:
    - When "Crouching" is false & "Push" is triggered, a "Push" animation is played while standing.
    - When "Crouching" is true & "Push" is triggered, a "Push" animation is played while crouched.
    - These animations override existing IK & pickup animations (so the item will stay in the hand, or must be disappeared before the push plays)

- ** Monster Choosing:
  - Ingame, the player can be flagged/targeted/chosen by monsters. That's controlled by the "Chosen_State" integer.
  - The "Chosen_State" integer has currently 4 ID's and scenarios associated with them:
    - [0]: No monster has chosen the player
    - [1]: Lanternhead has converted the player. Will only trigger a conversion when "L_Convert" is triggered. "Chosen_State = [1]" should be activated as soon as the "L_Convert is triggered", and will persist while the player is zombified. This ID signifies that the player is zombified.
    - [2]: Sjena has chosen the player. This will let the Sjena linger on the player's back until they decide to kill. Death from this monster will be explained below.
    - [3]: Night Gaunt has chosen the player. This will let the Night Gaunt appear for everyone else that doesn't have "Chosen_State = [3]", and be who Night Gaunt targets.Death from this monster will be explained below.
    - [4]: Cymothoa has chosen the player. This is only applicable to the small Cymothoa. When "Cy_Attacked" is triggered while this Id is on, the Cymothoa Mouth animation will play, muting the player for a period of time. Death from this monster will be explained below.

- ** Death by Monsters:
  - Monsters will kill the player under specific conditions. These are those conditions/scenarios:
    - [1]: Lanternhead Convert:
      - Triggers "Chosen_State = [1]"
      - Triggers "L_Convert"
      - Player is converted into a zombie.
    - [1]: Zombie Player Death. When player is already a zombie, kill zombie by:
      - Triggering "Kill_Player" while "Chosen_State = [1]"
      - Toggle "Is_Dead"
    - [2]: Sjena Player Death. When player has Sjena attached, kill player with Sjena by:
      - Triggering "Kill_Player" while "Chosen_State = [2]"
      - Toggle "Is_Dead"
    - [3]: Night Gaunt Player Death: When player is grabbed by Night Gaunt, kill player with Night Gaunt by:
      - Triggering "Kill Player" while "Chosen_State = [3]"
      - Toggle "Is_Dead"
    - [4]: Cymothoa Player Attack. When the player is attacked by a small Cymothoa:
      - Trigger "Cy_Attacked" while "Chosen_State = [4]"
      - Toggle "Cy_InBody"
      - WILL NOT KILL PLAYER, ONLY MUTE THEM.
    - [4]: Cymothoa Player Death. When the player already has a small Cymothoa in them, chestburst/kill them by:
      - Triggering "Kill Player" while "Chosen_State = [4]" & "Cy_InBody = True"
      - Toggle "Is_Dead"
      - If need be, toggle "Cy_InBody" off.

——————————————————————————————————————

Changelog 3/27/25:
- Updated version number to v1.3 for all necessary files
- Renamed "Player Zombie" SSM to "Player Zombified"
- Removed a redundant transition on "AnyState -> P_Zom_Convert"
- Scrapped the original Locomotion for "Locomotion Normal" and "Player Zombie", instead replacing it with a multi-drectional blend-tree system.
- Removed "Speed State" integer parameter. Replaced with "Move_X" & "Move_Y" float parameters.
- Renamed "PZ_" prefix for Zombified animations to "P_Zom" to better match pre-existing prefixes on other animations. (ex. "P_Cy", "P_NG", "P_Sjena")
- Updated the "Jump" animations, putting them into a SSM in "Locomotion Normal/P_Jump"
- Renamed the "RESET" trigger parameter to "DEV_RESET" to signify purpose
- Added the "Always_Active" Boolean parameter. Put into transition conditions like ones used for "Jump" to force a transition situation. NEEDS TO ALWAYS BE ON, hence the name.
- Added the "Speed_Modifier" float parameter.
- Added the "Move_X" & "Move_Y" float parameters, as stated previously.
- Added the "Arms_State" integer parameter
- Added the "Chosen_State" integer parameter.
- Added the "Grounded" Boolean parameter
- Added the "Push" trigger parameter.
- Renamed the "Zombie Attack" trigger parameter to "P_Zom_Attack" to better match prefixes for "Zombie Player"
- Added the "Cy_InBody" Boolean parameter.
- Renamed the "Convert" trigger parameter to "L_Convert" to better signify what monster is converting the player.
- Renamed the "Kill" trigger parameter to "Kill_Player" to better signify who is being killed.
- Renamed the "Death" Boolean parameter to "Is_Dead" to better signify if the player is dead.
- Removed "L_Zombified", "S_Chosen", "NG_Chosen", and "Cy_Chosen" Boolean parameters. Replaced with the "Chosen_State" integer parameter, as stated before.
- Added logic & layers for arm pickup & animation systems. Layers include: "Default Arms IK Weight", "Right Arm Animations Override", "Left Arm Animations Override", "Arm IK Rotations", "Arm IK Positions"
- Added a "RigContainer" & various children objects to add IK systems to the player arms. Uses Unity's "Animation Rigging" package/plugin.
- Added a layer mask to the "Zombie Attack" layer to make sure it only affects the upper torso.
- Added a "Player Hand Size Override" layer to force the hands to be at 1.3x scale.
- (test only, may not be needed) Added a "test_rotateplayer_90" layer that rotates the player 90° on the armature's X-axis.
- Renamed most animations to follow this ruleset:
  - Spaces are either removed or replaced with "_"
  - Locomotion animations have a suffix of "Forward", "Back", "Left", "Right", or "Idle"
  - Prefixes will start with "P_", followed by the monster it's associated with (ex. "Cy_", "NG_"). If there's no monster, skip.
  - IK-based animations will have a prefix of "ArmIK", followed by their purpose, followed by the regular naming convention.


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