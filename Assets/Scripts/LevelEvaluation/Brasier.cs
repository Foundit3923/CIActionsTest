using UnityEngine;

public class Brasier : MonoBehaviour
{
    [SerializeField] public GameObject flame;
    [SerializeField] public bool isLit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        flame.SetActive(false);
        isLit = flame.activeSelf;
    }

    public void SetActive(bool state)
    {
        flame.SetActive(state);
        isLit = state;
    }
}
