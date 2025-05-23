using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityServiceLocator;
public class InitializeItems : MonoBehaviour
{
    [SerializeField] public GameObject[] itemsToSpawnAsNeeded;
    [SerializeField] public GameObject[] itemsToBeReturned;
    [SerializeField] public GameObject[] itemsToPlaceRandomly;
    [SerializeField] public GameObject[] itemsToPlaceAtMonsterLocation;
    [SerializeField] public GameObject[] itemsToPlaceInReception;
    [SerializeField] public GameObject[] itemsToPlaceAtLocation;
    [SerializeField] public string[] locations;
    [SerializeField] public List<GameObject> itemsToStoreInBlackBoard;
    [SerializeField] public string[] blackboardKeys;

    public enum PlacementType
    {
        Random,
        Set
    }

    public void SetUpItems(Vector3 monsterLocation, List<GrimmGen.Tile> tiles, System.Random _rand)
    {
        BlackboardController blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        Blackboard blackboard = blackboardController.GetBlackboard();
        if (itemsToPlaceRandomly != null)
        {
            if (itemsToPlaceRandomly.Length > 0)
            {
                foreach (GameObject item in itemsToPlaceRandomly)
                {
                    if (tiles.Count > 0)
                    {
                        int index = _rand.Next(0, tiles.Count);
                        if (tiles[index] != null)
                        {
                            GrimmGen.Tile randomTile = tiles[index];
                            Instantiate(item, randomTile._worldLocation, item.transform.rotation);
                            if (itemsToStoreInBlackBoard.Contains(item))
                            {
                                blackboard.SetValue(blackboardController.GetKey(blackboardKeys[itemsToStoreInBlackBoard.IndexOf(item)]), item);
                            }

                            tiles.RemoveAt(index);
                        }
                    }
                    else
                    {
                        throw new Exception($"Unable to place {item.name}. No available tiles");
                    }
                }
            }
        }

        if (itemsToPlaceAtMonsterLocation != null)
        {
            if (itemsToPlaceAtMonsterLocation.Length > 0)
            {
                foreach (GameObject item in itemsToPlaceAtMonsterLocation)
                {
                    Instantiate(item, monsterLocation, item.transform.rotation);
                    if (itemsToStoreInBlackBoard.Contains(item))
                    {
                        blackboard.SetValue(blackboardController.GetKey(blackboardKeys[itemsToStoreInBlackBoard.IndexOf(item)]), item);
                    }
                }
            }
        }

        if (itemsToPlaceAtLocation != null)
        {
            if (itemsToPlaceAtLocation.Length > 0)
            {
                for (int i = 0; i < itemsToPlaceAtLocation.Length; i++)
                {
                    if (blackboard.TryGetValue(blackboardController.GetKey(locations[i]), out GameObject item))
                    {
                        Instantiate(itemsToPlaceAtLocation[i], item.transform.position, itemsToPlaceAtLocation[i].transform.rotation);
                        if (itemsToStoreInBlackBoard.Contains(item))
                        {
                            blackboard.SetValue(blackboardController.GetKey(blackboardKeys[itemsToStoreInBlackBoard.IndexOf(item)]), item);
                        }
                    }
                    else
                    {
                        throw new Exception($"Unable to place {itemsToPlaceAtLocation[i].name}. BlackboardKey {locations[i]} had no associated Value");
                    }
                }
            }
        }

        if (itemsToPlaceInReception != null)
        {
            if (itemsToPlaceInReception.Length > 0)
            {

                BlackboardKey receptionRoomKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Reception);
                if (blackboard.TryGetValue(receptionRoomKey, out GrimmGen.Room receptionRoom))
                {
                    if (receptionRoom.props.Count > 0)
                    {
                        List<GrimmGen.Prop> availableProps = receptionRoom.props.ToList();
                        foreach (GameObject item in itemsToPlaceInReception)
                        {
                            int index = _rand.Next(0, availableProps.Count);
                            GrimmGen.Prop selectedProp = availableProps[index];
                            Instantiate(item, selectedProp.PropGameObject.transform.position, item.transform.rotation);
                            if (itemsToStoreInBlackBoard.Contains(item))
                            {
                                blackboard.SetValue(blackboardController.GetKey(blackboardKeys[itemsToStoreInBlackBoard.IndexOf(item)]), item);
                            }

                            availableProps.RemoveAt(index);
                        }
                    }
                }
            }
        }
    }
}
