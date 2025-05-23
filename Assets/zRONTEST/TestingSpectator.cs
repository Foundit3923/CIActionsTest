using UnityEngine;

public class TestingSpectator : MonoBehaviour
{
    //Practicing writing my own code by hand. This is for testing the spectating.

    PlayerProperties pp;
    PlayerInformationManager pim;
    void Start()
    {
        pp = GetComponent<PlayerProperties>();

        pim = GetComponent<PlayerInformationManager>();
    }

    private void Update()
    {
        
        {
            if (pp == null || pim == null)
            {
                Debug.Log("Dis not workin'");
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                pim.DecreaseHealth(25, null);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                pim.IncreaseHealth(25, null);
            }
        }
        
    }

}