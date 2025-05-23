using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Highlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] public Button button;
    [SerializeField] public GameObject highlight;
    bool activate = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        activate = true;
        Debug.Log("Pointer Enter");
    }

    public void OnPointerExit(PointerEventData eventData) => activate = false;

    private void Update()
    {
        if (activate)
        {
            if (!highlight.activeSelf)
            {
                highlight.SetActive(true);
            }
        }
        else
        {
            if (highlight.activeSelf)
            {
                highlight.SetActive(false);
            }
        }
    }
}
