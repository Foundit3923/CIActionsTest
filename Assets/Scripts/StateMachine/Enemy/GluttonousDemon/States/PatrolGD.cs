using Mirror.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GluttonousDemonStates
{
    public class PatrolState : EnemyControllerState
    {
        public PatrolState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        public float attackRange = 10f;
        private Vector3 destPoint;
        private bool walkpointSet;
        private bool shouldAttack = false;
        public float timerGD = 10f;
        private bool shouldEnvAtk;
        private GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        private List<Transform> targets = new();
        private bool canAttack = false;
        private float timeSinceLastAttack;
        private float attackCooldown = 5f;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState() => Debug.Log("------------Patrol State");
        public override void ExitState() => ResetFlags();

        public override void UpdateState()
        {
            if (!canAttack)
            {
                timeSinceLastAttack += UnityEngine.Time.deltaTime;

                if (timeSinceLastAttack >= attackCooldown)
                {
                    canAttack = true;
                }
            }

            if (canAttack)
            {
                foreach (GameObject player in players)
                {
                    Transform target = player.transform;

                    if (target != null)
                    {
                        Vector3 targetPosition = target.position;

                        float distanceToTarget = Vector3.Distance(Context.Self.transform.position, targetPosition);
                        if (distanceToTarget < attackRange)
                        {
                            shouldAttack = true;
                        }
                    }
                }
            }

            timerGD -= UnityEngine.Time.deltaTime;
            //Debug.Log($"Remaining Time: {Math.Max(0, timerGD):0.00} seconds");
            if (timerGD <= 0)
            {
                Debug.Log("Puke Tyme");
                shouldEnvAtk = true;
            }

            if (!walkpointSet)
            {
                SearchForDest();
            }

            if (walkpointSet)
            {
                Context.Agent.SetDestination(destPoint);
            }

            if (Vector3.Distance(Context.Self.transform.position, destPoint) < 2f) walkpointSet = false;

            if (Context.CheckProximityToDeathItem(Context.itemCheckRadius) == true)
            {
                Context.isDead = true;
            }
        }
        private void SearchForDest()
        {
            float z = UnityEngine.Random.Range(-Context.range, Context.range);
            float x = UnityEngine.Random.Range(-Context.range, Context.range);

            destPoint = new Vector3(Context.Self.transform.position.x + x, Context.Self.transform.position.y, Context.Self.transform.position.z + z);

            if (Physics.Raycast(destPoint, Vector3.down, Context.groundLayer))
            {
                walkpointSet = true;
            }
        }

        private void ResetFlags()
        {
            shouldAttack = false;
            walkpointSet = false;
            shouldEnvAtk = false;
            timerGD = 10f;
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (shouldEnvAtk)
            {
                return EnemyControllerStateMachine.EEnemyState.EnvAtk;
            }
            else if (Context.isDead)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else if (shouldAttack)
            {
                return EnemyControllerStateMachine.EEnemyState.Attack;
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
