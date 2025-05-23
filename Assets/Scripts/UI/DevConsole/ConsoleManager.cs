using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class DevConsole : MonoBehaviour
{
    //public GameObject consoleUI;     // The console UI (InputField + Text)
    public TMP_InputField inputField;    // The InputField where you type commands
    public TextMeshProUGUI outputText;          // The Text element where output will be displayed
    public GameObject devConsole;
    public ScrollRect scrollView;
    public GameObject scrollbarVertical;
    public GameObject[] monsterPrefabs; // An array of monster prefabs to spawn
    public Dictionary<string, GameObject> spawnedMonstersByName = new(); // Track monsters by their names

    private bool isConsoleVisible = false;

    private void Start() => inputField.textComponent.color = Color.black;
    void Update()
    {
        // Toggle console visibility with the `~` key
        if (Input.GetKeyDown(KeyCode.BackQuote)) // `~` key
        {
            isConsoleVisible = !isConsoleVisible;
            SetActive(); // Show or hide the console
        }

        // Execute command when user presses Enter, and only if the console is visible
        if (isConsoleVisible && Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteCommand(inputField.text);  // Execute command
            inputField.text = "";  // Clear the input field after executing the command
            inputField.DeactivateInputField();  // Deactivate the input field to prevent it from highlighting
        }
    }

    public void SetActive()
    {
        if (devConsole.gameObject.activeSelf)
        {
            Debug.Log("Console is eepy");
            foreach (Transform child in devConsole.transform)
            {
                child.gameObject.SetActive(false);
            }

            scrollbarVertical.gameObject.SetActive(false);
            foreach (Transform child in scrollbarVertical.transform)
            {
                child.gameObject.SetActive(false);
            }

            devConsole.gameObject.SetActive(false);
            inputField.DeactivateInputField();
        }
        else
        {
            Debug.Log("Console is awake!");
            devConsole.SetActive(true);
            foreach (Transform child in devConsole.transform)
            {
                child.gameObject.SetActive(true);
            }

            scrollbarVertical.gameObject.SetActive(true);
            foreach (Transform child in scrollbarVertical.transform)
            {
                child.gameObject.SetActive(true);
            }

            inputField.ActivateInputField();  // Activate the InputField
            inputField.Select();  // Select the InputField
            //EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
    }
    // Execute the command typed by the user
    // Execute the command typed by the user
    private void ExecuteCommand(string command)
    {
        outputText.text += $"> {command}\n";  // Display the command in the console output

        Canvas.ForceUpdateCanvases();

        StartCoroutine(ScrollToBottom());

        string[] args = command.Split(' ');  // Split command into parts (e.g., "spawnMonster monsterName")

        if (args.Length == 0) return;

        string mainCommand = args[0];

        switch (mainCommand.ToLower())
        {
            case "spawnmonster":
                if (args.Length > 1)
                {
                    string monsterName = args[1];
                    SpawnMonster(monsterName);
                }
                else
                {
                    outputText.text += "Please specify a monster name.\n";
                }

                break;

            case "killmonster":
                if (args.Length > 1)
                {
                    string monsterName = args[1];
                    KillMonster(monsterName);
                }
                else
                {
                    outputText.text += "Please specify a monster name to kill.\n";
                }

                break;

            default:
                outputText.text += "Unknown command.\n";
                break;
        }

        // Re-activate the input field so you can immediately type the next command
        inputField.text = "";  // Clear the input field after executing the command
        inputField.ActivateInputField();  // Reactivate the input field so the user can type again
        inputField.Select();  // Ensure the input field is selected for typing
    }

    // Spawn a monster by name
    private void SpawnMonster(string monsterName)
    {
        foreach (var prefab in monsterPrefabs)
        {
            if (prefab.name.ToLower() == monsterName.ToLower()) // Compare the monster name
            {
                GameObject spawnedMonster = Instantiate(prefab, new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5)), Quaternion.identity);
                spawnedMonstersByName[monsterName] = spawnedMonster;  // Add the monster to the dictionary with its name as the key
                outputText.text += $"{monsterName} spawned.\n";
                return;
            }
        }

        outputText.text += $"No monster named {monsterName} found.\n";
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();

        scrollView.verticalNormalizedPosition = 0f;
    }

    // Kill a monster (destroy all monsters of a specific tag or type)
    private void KillMonster(string monsterName)
    {
        if (spawnedMonstersByName.ContainsKey(monsterName))
        {
            GameObject monsterToKill = spawnedMonstersByName[monsterName];
            Destroy(monsterToKill);  // Destroy the monster
            spawnedMonstersByName.Remove(monsterName);  // Remove the monster from the dictionary
            outputText.text += $"{monsterName} killed.\n";
        }
        else
        {
            outputText.text += $"No monster named {monsterName} found to kill.\n";
        }
    }
}
