using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

public class InputMonitor : MonoBehaviour
{
    private Menu currentMenu;
    private Scene currentContext;
    Utility utils;
    [SerializeField] public InputActionAsset asset;
    private AssetsInputs _input;
    string settingsSceneName = "SettingsMenu";

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            if (FizzyNetworkManager.singleton != null)
            {
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
            }

            return ServiceLocator.For(this).Get<FizzyNetworkManager>();
        }
    }
    private void Awake()
    {
        utils = ServiceLocator.Global.Get<Utility>();
        PlayerInput playerInput = this.gameObject.GetComponent<PlayerInput>();
        if (SceneManager.GetActiveScene().name == settingsSceneName)
        {
            playerInput.enabled = true;
        }
        else
        {
            playerInput.enabled = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SceneManager.GetActiveScene().name == settingsSceneName)
        {
            _input = this.gameObject.AddComponent<AssetsInputs>();
            _input.asset = asset;
            _input.enabled = true;
            currentContext = SceneManager.GetActiveScene();
            Utility.OnNewMenu += UpdateMenuAndContext;
        }
    }

    // Update is called once per frame
    void Update() => EvalInput();

    private void UpdateMenuAndContext()
    {
        currentMenu = utils.GetCurrentMenu();
        currentContext = SceneManager.GetActiveScene();
    }

    private void EvalInput()
    {
        if (currentContext.name == settingsSceneName)
        {
            PauseInput();
        }
    }

    private void PauseInput()
    {
        if (_input.pause)
        {
            Scene settingsScene = SceneManager.GetSceneByName(settingsSceneName);
            if (settingsScene.IsValid())
            {
                SceneManager.UnloadSceneAsync(settingsScene);
                SceneManager.LoadScene(Manager.offlineScene);
            }
        }
    }
}
