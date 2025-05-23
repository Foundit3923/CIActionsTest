using Mirror.BouncyCastle.Security;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class SteamItemDrop : MonoBehaviour
{
    //This is Ron practicing writing my own code but also doing this to control rarity for steam item drops.
    private int roll;
    private int nightsSurvived;     //this will be pulled from difficulty manager, just putting in a placeholder value for now.
    private bool gameComplete;      //Simple bool to start rolling.

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        if (gameComplete)
        {
            LetsRoll(nightsSurvived);
        }
    }

    void Update()
    {

    }

    private void LetsRoll(int nightsSurvived) //ALL RARITY VALUES SUBJECT TO CHANGE UPON REVIEW FROM TEAM
    {
        switch (nightsSurvived) //this determines everything. What is the highest rarity available to drop, and how likely each roll will be to hit.
        {
            case 5:     //SuperRare
            {
                roll = Random.Range(1, 10001); //0.01% base chance to hit (superRare)
                if (roll <= nightsSurvived - 4)
                {
                    Debug.Log("Super Rare Item Awarded!");
                    break;
                }

                goto case 4;
            }
            case 4:     //VeryRare
            {
                roll = Random.Range(1, 1001); //0.1% base chance to hit (veryRare)
                if (roll <= nightsSurvived - 3)
                {
                    Debug.Log("Very Rare Item Awarded!");
                    break;
                }

                goto case 3;
            }
            case 3:     //Rare
            {
                roll = Random.Range(1, 401); //0.25% base chance to hit (rare)
                if (roll <= nightsSurvived - 2)
                {
                    Debug.Log("Rare Item Awarded!");
                    break;
                }

                goto case 2;
            }
            case 2:     //Uncommon
            {
                roll = Random.Range(1, 251); //0.4% base chance to hit (uncommon)
                if (roll <= nightsSurvived - 1)
                {
                    Debug.Log("Uncommon Item Awarded!");
                    break;
                }

                goto case 1;
            }
            case 1:     //Common
            {
                roll = Random.Range(1, 125); //0.8% base chance to hit (common)
                if (roll <= nightsSurvived)
                {
                    Debug.Log("Common Item Awarded!");
                    break;
                }

                break;
            }
            case 0:     //None
            {
                //No item will be awared as you did not complete the first night.
                break;
            }
            default:    //incase nightsSurvived >= 6; Will default to highest value possible.
            {
                goto case 5;
            }
        }
    }
}
