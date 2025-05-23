using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityServiceLocator;

public class Egg : MonoBehaviour
{
    private AudioSource AudioSource;
    private Light Light;
    private GameObject FroogDaddy;
    private float distance;

    BlackboardController blackboardController;
    Blackboard blackboard;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        //Change this to track the grotto
        //if distance is > 100, normalize to less than 100, then less than 1
        if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Monsters, out List<GameObject> monsters))
        {
            FroogDaddy = monsters.Where(x => x.GetComponent<EnemyProperties>().Name == "Vodyanoi").ToArray()[0];
            distance = Vector3.Distance(transform.position, FroogDaddy.transform.position) / 100;
        }

        AudioSource.loop = true;
    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector3.Distance(transform.position, FroogDaddy.transform.position) / 100;
        Light.intensity = distance * 10;
        AudioSource.volume = distance;
    }
}
