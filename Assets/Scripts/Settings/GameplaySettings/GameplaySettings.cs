using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityServiceLocator;

public class GameplaySettings : MonoBehaviour
{
    public int Seed;

    public float DifficultyControlA = 0.05f;  // Night scaling factor
    public float DifficultyControlB = 0.1f;   // Player count scaling factor
    public float DifficultyControlC = 1.2f;   // Exponential scaling factor for player count
    public float MaxDifficulty = 2000f;

    public int NightsSurvived = 0;  //Both of these will need to be puulled from Context/Blackboard
    public int PlayerCount = 4;

    public float MonsterPointValue = 40f;  //All of these are subject to change, these are just blanket values for now.
    public float ItemPointValue = 10f;     //Same here
    public float Budget = 0f;

    public float RoomDensityLowerClamp = 8f;
    public float RoomDensityMultiplyer = 100f;
    public float RoomDensityUpperClamp = 1667f;

    // Allocating percentage of total points for monsters and items
    public float MonsterAllocationPercentage = 0.75f;  // 75% of difficulty for monsters
    public float ItemAllocationPercentage = 0.25f;    // 25% of difficulty for items

    private Dictionary<int, Dictionary<string, object>> gameplayHistory = new();

    public void SetValues(GameplaySettings newSettings, bool isRoot = false)
    {
        bool isUpdating = false;
        if (isRoot)
        {
            //Store history
            //TODO: this doesn't actually save anything
            if (!gameplayHistory.ContainsKey(NightsSurvived))
            {
                gameplayHistory.Add(NightsSurvived, this.GetValues());
            }
            else
            {
                isUpdating = true;
            }
        }

        Seed = newSettings.Seed;
        DifficultyControlA = newSettings.DifficultyControlA;
        DifficultyControlB = newSettings.DifficultyControlB;
        DifficultyControlC = newSettings.DifficultyControlC;
        MaxDifficulty = newSettings.MaxDifficulty;
        NightsSurvived = newSettings.NightsSurvived;
        PlayerCount = newSettings.PlayerCount;
        MonsterPointValue = newSettings.MonsterPointValue;
        ItemPointValue = newSettings.ItemPointValue;
        Budget = newSettings.Budget;
        RoomDensityLowerClamp = newSettings.RoomDensityLowerClamp;
        RoomDensityMultiplyer = newSettings.RoomDensityMultiplyer;
        RoomDensityUpperClamp = 100000f / RoomDensityMultiplyer;
        MonsterAllocationPercentage = newSettings.MonsterAllocationPercentage;
        ItemAllocationPercentage = newSettings.ItemAllocationPercentage;
        gameplayHistory = newSettings.gameplayHistory;

        if (isUpdating)
        {
            gameplayHistory[NightsSurvived] = this.GetValues();
        }
    }

    public void SetValues(Dictionary<string, object> newSettings, bool isRoot = false)
    {
        bool isUpdating = false;
        if (isRoot)
        {
            //Store history
            //TODO: this doesn't actually save anything
            if (!gameplayHistory.ContainsKey(NightsSurvived))
            {
                gameplayHistory.Add(NightsSurvived, this.GetValues());
            }
            else
            {
                isUpdating = true;
            }
        }

        Seed = (int)newSettings["Seed"];
        DifficultyControlA = (float)newSettings["DifficultyControlA"];
        DifficultyControlB = (float)newSettings["DifficultyControlB"];
        DifficultyControlC = (float)newSettings["DifficultyControlC"];
        MaxDifficulty = (float)newSettings["MaxDifficulty"];
        NightsSurvived = (int)newSettings["NightsSurvived"];
        PlayerCount = (int)newSettings["PlayerCount"];
        MonsterPointValue = (float)newSettings["MonsterPointValue"];
        ItemPointValue = (float)newSettings["ItemPointValue"];
        Budget = (float)newSettings["Budget"];
        RoomDensityLowerClamp = (float)newSettings["RoomDensityLowerClamp"];
        RoomDensityMultiplyer = (float)newSettings["RoomDensityMultiplyer"];
        RoomDensityUpperClamp = (float)(100000f / RoomDensityMultiplyer);
        MonsterAllocationPercentage = (float)newSettings["MonsterAllocationPercentage"];
        ItemAllocationPercentage = (float)newSettings["ItemAllocationPercentage"];
        gameplayHistory = (Dictionary<int, Dictionary<string, object>>)newSettings["gameplayHistory"];

        if (isUpdating)
        {
            gameplayHistory[NightsSurvived] = this.GetValues();
        }
    }

    public Dictionary<string, object> GetValues()
    {
        Dictionary<string, object> values = new()
        {
            {
                "Seed",
                Seed
            },
            {
                "DifficultyControlA",
                DifficultyControlA
            },
            {
                "DifficultyControlB",
                DifficultyControlB
            },
            {
                "DifficultyControlC",
                DifficultyControlC
            },
            {
                "MaxDifficulty",
                MaxDifficulty
            },
            {
                "NightsSurvived",
                NightsSurvived
            },
            {
                "PlayerCount",
                PlayerCount
            },
            {
                "MonsterPointValue",
                MonsterPointValue
            },
            {
                "ItemPointValue",
                ItemPointValue
            },
            {
                "Budget",
                Budget
            },
            {
                "RoomDensityLowerClamp",
                RoomDensityLowerClamp
            },
            {
                "RoomDensityMultiplyer",
                RoomDensityMultiplyer
            },
            {
                "MonsterAllocationPercentage",
                MonsterAllocationPercentage
            },
            {
                "ItemAllocationPercentage",
                ItemAllocationPercentage
            },
            {
                "gameplayHistory",
                gameplayHistory
            }
        };
        return values;
    }

    public Dictionary<int, Dictionary<string, object>> GetHistory() => gameplayHistory;

}
