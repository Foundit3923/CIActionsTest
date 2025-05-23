using System;
using UnityEngine;
using UnityEngine.UI;

public class Hotbar : MonoBehaviour
{
    [SerializeField] private Button _invSlot1;
    [SerializeField] private Button _invSlot2;

    private static RawImage invSlot1Outline;
    private static RawImage invSlot2Outline;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Transform childTransform in _invSlot1.transform)
        {
            invSlot1Outline = childTransform.GetComponent<RawImage>();
        }

        foreach (Transform childTransform in _invSlot2.transform)
        {
            invSlot2Outline = childTransform.GetComponent<RawImage>();
            invSlot2Outline.enabled = false;
        }

        _invSlot1.onClick.AddListener(() => toggleInventorySlot());
        _invSlot2.onClick.AddListener(() => toggleInventorySlot());

    }
    private void Update()
    {
        if (Math.Abs(Input.mouseScrollDelta.y) % 2 != 0)
        {
            //When not in Update() all scroll events trigger twice
            Debug.Log("Scroll Value: " + Input.mouseScrollDelta.y.ToString());
            toggleInventorySlot();
        }
    }

    private void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.A.ToString())))
        {
            toggleInventorySlot();
        }

        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.Alpha1.ToString())))
        {
            setInventorySlotByValue(1);
        }

        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.Alpha2.ToString())))
        {
            setInventorySlotByValue(2);
        }
    }

    private void toggleInventorySlot()
    {
        if (invSlot1Outline.enabled)
        {
            invSlot1Outline.enabled = false;
            invSlot2Outline.enabled = true;
        }
        else
        {
            invSlot2Outline.enabled = false;
            invSlot1Outline.enabled = true;

        }
    }

    private void setInventorySlotByValue(int value)
    {
        switch (value)
        {
            case 1:
                invSlot2Outline.enabled = false;
                invSlot1Outline.enabled = true;
                break;
            case 2:
                invSlot1Outline.enabled = false;
                invSlot2Outline.enabled = true;
                break;
        }
    }
}
