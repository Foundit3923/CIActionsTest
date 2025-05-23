using System.IO;
using TMPro;
using UnityEngine;

public class currentdir : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    private void Awake() => text.text = Directory.GetCurrentDirectory();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
