using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace NightGauntStates
{
    public class AttackState : EnemyControllerState
    {
        public AttackState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        private float visibilityDuration = 3f, visibilityTimer;
        bool shouldKill;
        public override void EnterState()
        {
            Debug.Log($"------------{Context.Self.name} | Attack State Initiated.");
            //targetPlayer.getPlayerProperties.isDead = true;
            Context.KillTargetPlayer();
            //Context.TargetPlayer.gameObject.GetComponent<PlayerProperties>().isDead = true;

            foreach (Camera cam in Camera.allCameras)
            {
                cam.cullingMask |= 1 << LayerMask.NameToLayer("NightGauntLayer");
            }

            visibilityTimer = visibilityDuration;

            Context.shouldChase = false;

        }
        public override void ExitState()
        {
            Context.setLastState(StateKey);

            Context.SetLastKillPosition(Context.Self.transform.position);

            Context.UpdateVisibility();
        }
        public override void UpdateState()
        {
            //Get the current target player's PlayerProperties
            GameObject targetPlayer = Context.FindNearestNonCursedPlayer();
            // ^This method will return dead players, look at NightGauntVisibility for ideas on how to fix that.

            if (visibilityTimer > 0)
            {
                visibilityTimer -= Time.unscaledDeltaTime;
            }
            else
            {
                if (targetPlayer == null)
                {
                    shouldKill = true;
                }

                Debug.Log("Attack! Close enough to: " + targetPlayer.name);
                //attack logic to expand later (animation/sound)
                //maybe add particle effects/unique feature with vision update for all

                Context.shouldFlee = true;
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.shouldKill == true || shouldKill == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else if (Context.shouldFlee == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Flee;
            }
            else
            {
                return StateKey;
            }
        }
        public override void OnTriggerEnter(Collider other)
        {
        }
        public override void OnTriggerStay(Collider other)
        {
        }
        public override void OnTriggerExit(Collider other)
        {
        }
        public override void OnCollisionEnter(Collision collision)
        {
        }

        public override void OnCollisionStay(Collision collision)
        {
        }
        public override void OnCollisionExit(Collision collision)
        {
        }
    }
}
