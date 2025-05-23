using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(GameplaySettings))]
public class DifficultyManager : MonoBehaviour
{
    public LoadManager.State state;
    private GameplaySettings gameplaySettings;

    public GameplaySettings GameplaySettings
    {
        get
        {
            if (gameplaySettings != null)
            {
                return gameplaySettings;
            }

            return gameplaySettings = this.gameObject.GetOrAddComponent<GameplaySettings>();
        }
        set => gameplaySettings = value;
    }

    private BlackboardController blackboardController;
    private Blackboard blackboard;
    private BlackboardKey playersKey, monstersKey;

    public delegate void DifficultyUpdatedEvent();
    public static event DifficultyUpdatedEvent OnDifficultyUpdated;

    public void SetValues(GameplaySettings newGameplaySettings)
    {
        GameplaySettings.SetValues(newGameplaySettings.GetValues(), true);
        OnDifficultyUpdated?.Invoke();
    }

    public void SetValues(Dictionary<string, object> newGameplaySettings)
    {
        GameplaySettings.SetValues(newGameplaySettings, true);
        OnDifficultyUpdated?.Invoke();
    }

    public Dictionary<string, object> GetValues() => GameplaySettings.GetValues();

    public float CalculateBudget()
    {
        // Calculate the difficulty multiplier
        float budget = (1 + (GameplaySettings.DifficultyControlA * GameplaySettings.NightsSurvived)) *
                           (1 + (GameplaySettings.DifficultyControlB * Mathf.Pow(GameplaySettings.PlayerCount, GameplaySettings.DifficultyControlC)));

        // Apply max difficulty cap
        budget = Mathf.Min(budget, GameplaySettings.MaxDifficulty);

        this.gameplaySettings.Budget = budget;

        return budget;
    }

    public float ProcGenBudget(float budget = 0)
    {
        if (budget == 0)
        {
            budget = this.gameplaySettings.Budget;
        }

        float ProcGenBudget = Mathf.Round(Mathf.Max(Mathf.Min(budget, GameplaySettings.RoomDensityUpperClamp) / 1000f * GameplaySettings.RoomDensityMultiplyer, GameplaySettings.RoomDensityLowerClamp));

        return ProcGenBudget;
    }

    public void AdjustRewardsBasedOnDifficulty(float budget)
    {
        float rewardMultiplier = 1 + (1 * (budget / 1000f));  // Reward increases as difficulty increases

        float xpRate = 1 * rewardMultiplier;
    }

    public void CalculateDifficulty()
    {
        // Calculate the total difficulty Value
        float budget = CalculateBudget();

        // Allocate points for monsters and items based on the set percentages
        float totalMonsterPoints = budget * GameplaySettings.MonsterAllocationPercentage;
        float totalItemPoints = budget * GameplaySettings.ItemAllocationPercentage;

        // Calculate number of monsters and items based on their point values
        int monstersToSpawn = Mathf.FloorToInt(totalMonsterPoints / GameplaySettings.MonsterPointValue);
        int itemsToSpawn = Mathf.FloorToInt(totalItemPoints / GameplaySettings.ItemPointValue);

        // Example output
        Debug.Log("Spawning " + monstersToSpawn + " monsters and " + itemsToSpawn + " items.");
    }

    public void DisplayDifficultyToPlayers(float budget = 0)  //This is likely to be scrapped but wanted to include for now :)
    {
        if (budget == 0)
        {
            budget = this.gameplaySettings.Budget;
        }

        string difficultyLevel = "Normal";

        if (budget > 100f)
            difficultyLevel = "Hard";
        if (budget > 200f)
            difficultyLevel = "Insane";
        if (budget > 300f)
            difficultyLevel = "OogaBoogaMode";

        Debug.Log("Current Difficulty: " + difficultyLevel);
    }

    private void Awake() => ServiceLocator.Global.Register<DifficultyManager>(this);

    void Start()
    {
        // Call function to spawn monsters and items based on the calculated difficulty
        LevelEvaluator.OnLevelCompleted += CalculateNextLevel;
        OnDifficultyUpdated += CalculateDifficulty;
        CalculateDifficulty();
        state = LoadManager.State.Started;
    }

    public void InitDependencies()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        playersKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Players);
        monstersKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Monsters);
        state = LoadManager.State.Initialized;
        SetSeed();
    }

    public void CalculateNextLevel()
    {
        GameplaySettings newValues = this.gameplaySettings;

        newValues.NightsSurvived++;

        this.SetValues(newValues);
    }

    public void SetSeed()
    {
        BlackboardController blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        Blackboard blackboard = blackboardController.GetBlackboard();
        Int32 seed = -1;
        Int32 Seed = 0;
        blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.RandomSeed), out seed);
        if (seed is (-1) or 0)
        {
            Seed = (Int32)Environment.TickCount;
            seed = Seed;
        }

        Dictionary<string, object> newSettings = GameplaySettings.GetValues();
        newSettings["Seed"] = seed;
        GameplaySettings.SetValues(newSettings, true);
    }
}
