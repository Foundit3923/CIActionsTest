using UnityEngine;
using BaseState;
using UnityEngine.UIElements;
using System;

public abstract class EnemyControllerState : BaseState<EnemyControllerStateMachine.EEnemyState>
{
    protected EnemyControllerContext Context;
    protected TimeTickSystem TimeTickSystem;

    public EnemyControllerState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState stateKey) : base(stateKey) => Context = context;//TimeTickSystem = Context.TimeTickSystem;

    protected void GetClosestPointOnCollider(Collider intersectingCollider)
    {
        Vector3 positionToCheck = intersectingCollider.transform.position;
        Context.lastKnownPos = intersectingCollider.ClosestPoint(positionToCheck);
    }

    protected static void FuncOnTick(Action callback)
    {
        TimeTickSystem.OnTick += delegate (object sender, TimeTickSystem.OnTickEventArgs e)
        {
            callback?.Invoke();
        };
    }

    protected static void RemoveFuncOnTick(Action callback)
    {
        TimeTickSystem.OnTick -= delegate (object sender, TimeTickSystem.OnTickEventArgs e)
        {
            callback?.Invoke();
        };
    }
}
