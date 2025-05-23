using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditsMenu : MonoBehaviour
{
    public Button returnButton;

    private void Update() => Cursor.visible = true;
    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenu");
        Cursor.visible = false;
    }
}
