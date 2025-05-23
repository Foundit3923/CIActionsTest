using System.Collections.Generic;
using System.Linq;
using Mirror.BouncyCastle.Asn1.Mozilla;
using UnityEngine;
using static InventoryManager;

[RequireComponent(typeof(InventoryManager))]
public class PlayerProperties : MonoBehaviour
{
    public enum PlayerState
    {
        Attacked,
        Base,
        CyAttacked,
        Dead,
        Shrunk,
        SjenaHost,
        Zombie,
        Revived,
        Damaged,
        Healed,
        FailedRevival,
        FullHeal
    }

    public enum Persuer
    {
        None,
        LanternHead,
        Sjena,
        NightGaunt,
        Cymothoa
    }
    [SerializeField] public bool isDead = false;
    public bool isZombie = false;
    public bool inLevel = false;
    public bool isSpectating = false;
    public int health = 100;
    private PlayerState _playerState;
    private Persuer _persuerMonster;
    private List<PlayerState> _experiences = new();
    private InventoryManager __inventoryManager;
    private InventoryManager _inventoryManager
    {
        get
        {
            if (__inventoryManager != null )
            {
                return __inventoryManager;
            }

            if (this.gameObject.TryGetComponent<InventoryManager>(out InventoryManager value))
            {
                return __inventoryManager = value;
            }

            return __inventoryManager = this.gameObject.AddComponent<InventoryManager>();
        }
        set => __inventoryManager = value;
    }

    //--------- Setters
    public void SetState(PlayerState state)
    {
        _experiences.Add(_playerState);
        _playerState = state;
        if (state == PlayerState.Dead) { isDead = true; }

        if (state == PlayerState.Zombie) { isZombie = true; }
    }

    public void SetValues(PlayerProperties newProps)
    {
        isDead = newProps.isDead;
        isZombie = newProps.isZombie;
        inLevel = newProps.inLevel;
        health = newProps.health;
        _playerState = newProps._playerState;
        _experiences = newProps._experiences.ToList();
    }

    public void AddToolToInventory(InventoryItems tool) => _inventoryManager.AddToolToInventory(tool);
    public void RemoveToolFromInventory() => _inventoryManager.RemoveToolFromInventory();

    public void AddExperience(PlayerState state) => _experiences.Add(state);

    public void SetPersuer(Persuer monster) => _persuerMonster = monster;
    public void StoreCurrentToolReference(GameObject tool) => _inventoryManager.StoreCurrentToolReference(tool);

    //---------- Getters

    public PlayerState GetPlayerState() => _playerState;
    public List<PlayerState> GetExperiences() => _experiences;
    public InventoryItems[] GetInventory() => _inventoryManager.Inventory;
    public GameObject GetCurrentTool() => _inventoryManager.GetCurrentToolReference();

}
