using System;
using UnityEngine;
using UnityServiceLocator;
using static TimeTickSystem;

public class EvalSjenaShouldAttach : MonoBehaviour
{
    TimeTickSystem TimeTickSystem;
    BlackboardController blackboardController;
    Blackboard blackboard;
    Utility utils;

    private float highVal = 100f;
    private double baseChance = 2f;
    private double chance;

    private int layerIndex;
    private int layerMask;

    TileProperties.Lighting lightingState;

    private bool updateTicker = false;
    public bool shouldEval = true;
    public bool restartOverride = false;

    private Action callback;

    Ray ray;

    Vector3 _rayStart;
    Vector3 _rayDir;

    PlayerInformationManager pim;

    public delegate void OnSjenaShouldAttachEvent(GameObject go);
    public static event OnSjenaShouldAttachEvent OnSjenaShouldAttach;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        utils = ServiceLocator.Global.Get<Utility>();
        pim = GetComponent<PlayerInformationManager>();
        InvokeRepeating("GetCurrentRoomState", 0, 1);
        lightingState = TileProperties.Lighting.On;
        chance = baseChance;
        shouldEval = true;
        layerIndex = LayerMask.NameToLayer("Obstacle");
        layerMask = 1 << layerIndex;
    }

    private void GetCurrentRoomState()
    {
        if (pim.playerProperties.inLevel && shouldEval && !pim.playerProperties.isDead)
        {
            Vector3 dirToTarget = Vector3.up;

            float dstToTarget = 30;
            //_rayDir = dirToTarget * dstToTarget;

            ray = new Ray(transform.position, dirToTarget);
            RaycastHit _hitData = new();
            _rayStart = ray.origin;
            _rayDir = ray.direction * dstToTarget;
            if (Physics.Raycast(ray, out _hitData, dstToTarget, layerMask))
            {
                GameObject propRoot = utils.GetNearestPropRoot(_hitData.collider.gameObject);
                if (propRoot != null)
                {
                    TileProperties tileProperties = propRoot.transform.parent.GetComponent<TileProperties>();
                    if ((tileProperties.lighting != lightingState) || restartOverride)
                    {
                        lightingState = tileProperties.lighting;
                        UpdateLightingTicker();
                    }
                }
            }
        }
    }

    private void UpdateLightingTicker()
    {
        //check if there is a callback to remove the previous ticker
        InvokeCallback();

        switch (lightingState)
        {
            case TileProperties.Lighting.On:
                break;
            case TileProperties.Lighting.Off:
                chance = baseChance;
                callback = () => TimeTickSystem.OnTick_12 -= RollToAttach;
                TimeTickSystem.OnTick_12 += RollToAttach;
                break;
            case TileProperties.Lighting.Flickering:
                chance = Math.Ceiling(baseChance / 2d);
                callback = () => TimeTickSystem.OnTick_12 -= RollToAttach;
                TimeTickSystem.OnTick_12 += RollToAttach;
                break;
            default:
                break;
        }
    }

    private void RollToAttach(object sender, OnTickEventArgs e)
    {
        var rand = utils.GetRandomNumber(1, 101);
        if (rand <= chance)
        {
            //should attach to this player
            OnSjenaShouldAttach?.Invoke(this.gameObject);
        }
    }

    public void InvokeCallback()
    {
        if (callback != null)
        {
            callback.Invoke();
            callback = null;
        }
    }

    private void OnDestroy()
        //check if there is a callback to remove the previous ticker
        => callback?.Invoke();

    private void OnDisable()
        //check if there is a callback to remove the previous ticker
        => callback?.Invoke();

}
