using System;
using UnityEngine;

namespace GluttonousDemonStates
{
    public class AttackState : EnemyControllerState
    {
        public AttackState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        private bool shouldReset;
        private int damage = 10;  //10 base value, balance dmg and range later.
        private int attackRange = 10;

        public override void EnterState()
        {
            Debug.Log("------------Attack State");
            int roll = UnityEngine.Random.Range(1, 3); // 50% chance

            if (roll == 1)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

                foreach (GameObject player in players)
                {
                    float distance = Vector3.Distance(Context.Self.transform.position, player.transform.position);

                    if (distance < attackRange)
                    {
                        PlayerInformationManager playerManager = player.GetComponent<PlayerInformationManager>();
                        if (playerManager != null)
                        {
                            Debug.Log($"Monster hits player: {player.name}");
                            playerManager.DecreaseHealth(damage);
                        }
                    }
                }
            }
            else if (roll == 2)
            {
                //something else, or nothing at all :) maybe some sort of sound to know you triggered the roll but got lucky... this time.
            }
        }
        public override void ExitState() => shouldReset = false;

        public override void UpdateState()
        {
            if (Context.CheckProximityToDeathItem(Context.itemCheckRadius) == true)
            {
                Context.isDead = true;
            }

            if (!Context.isDead)
            {
                shouldReset = true;
            }
        }

        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (shouldReset) //shouldReset
            {
                return EnemyControllerStateMachine.EEnemyState.Reset;
            }
            else if (Context.isDead)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
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
