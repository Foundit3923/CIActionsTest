using System;
using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

public class GameplayStats : MonoBehaviour
{
    //Tools
    private BlackboardController blackboardController;
    private Blackboard blackboard;

    //Players
    public int PlayerCount;
    public int FailedLevels;
    public int LevelsSurvived;


    //Difficulty
    private DifficultyManager difficultyManager;
    public GameplaySettings gameplaySettings;
    Int32 Seed;

    //ProcGen
    public float timeToGenLevel;
    public float timeToBuildNavMesh;
    public int navMeshAgentCount;
    public int roomCount;
    public int hallwayCount;
    public int tileCount;
    public int monsterCount;

    //Monsters
    public Dictionary<string, int> monstersDefeated;        //<Monstername, killcount>
    public Dictionary<string, int> playerDeathsByMonster;   //<MonsterName, killcount>

    //History
    private Dictionary<int, GameplayStats> SessionStats = new();

    void Start()
    {
        ServiceLocator.Global.Register<GameplayStats>(this);
        difficultyManager = ServiceLocator.For(this).Get<DifficultyManager>();
        DifficultyManager.OnDifficultyUpdated += StoreDifficultyValues;
    }

    private void StoreDifficultyValues() => gameplaySettings.SetValues(difficultyManager.GetValues());

    private void GetNewSeed()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        if (!blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.RandomSeed), out Int32 seed))
        {
            Seed = (Int32)Environment.TickCount;
            seed = Seed;
        }
    }
}
