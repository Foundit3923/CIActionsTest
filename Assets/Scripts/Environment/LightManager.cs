using UnityEngine;
using UnityServiceLocator;
using static IServerExpert;

public class LightManager : MonoBehaviour, IServerExpert
{
    //[SerializeField]
    //[Tooltip("External light to flicker; you can leave this null if you attach script to a light")]
    //public new Light light;
    [SerializeField]
    [Tooltip("Minimum random light intensity")]
    public float minIntensity = 0f;
    [SerializeField]
    [Tooltip("Maximum random light intensity")]
    public float maxIntensity = 1f;
    [SerializeField]
    [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
    [Range(1, 50)]
    public int smoothing = 5;
    [SerializeField]
    [Tooltip("How long should the current settings be used before asking for new settings?")]
    public float duration = 1f;
    [SerializeField]
    [Tooltip("Minimum duration of a flicker sequence")]
    public float minDuration = 0.1f;
    [SerializeField]
    [Tooltip("Maximum duration of a flicker sequence")]
    public float maxDuration = 5f;

    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey lightManager, readyFlagKey, flickerKey;

    bool getBlackboardAndRegister, isPriority;
    public LoadManager.State state;
    private int flicker = 0;
    private float maxIntensityRandom;
    private float flickerDuration = 0f;
    int readyFlag = 0;
    int attempts = 0;
    public int GetInsistence(Blackboard blackboard)
    {
        int result = (int)ServerInsistenceLevel.None;
        if (isPriority)
        {
            result = (int)ServerInsistenceLevel.Init; //This should run until it is deregistered by LoadManager
        }
        else
        {
            result = (int)ServerInsistenceLevel.UI + attempts;
        }

        attempts++;
        return result;
    }

    public void Execute(Blackboard blackboard)
    {
        blackboard.AddAction(() =>
        {
            attempts = 0;
            flicker = Random.Range(0, 50);
            maxIntensityRandom = Random.Range(0, 100) / 100f;
            flickerDuration = Random.Range(minDuration, maxDuration);
            blackboard.SetValue(flickerKey, (flicker, flickerDuration, minIntensity, maxIntensityRandom));
        });
    }
    public bool IsPriority(bool set)
    {
        isPriority = set;
        return set;
    }

    void Awake()
    {
        state = LoadManager.State.Awake;
        ServiceLocator.Global.Register<LightManager>(this);
        DontDestroyOnLoad(this);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => state = LoadManager.State.Started;

    public void InitDependencies()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        flickerKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Flicker);
        blackboard.SetValue(flickerKey, flicker);
        blackboardController.RegisterExpert(this);
    }

    // Update is called once per frame
    void Update()
    {
        //Replace this with a reference to the LoadManager
        //Get the state of the blackboard from there
        //remove service locator from coordination to decrease interdependency
        ////solving race conditions and not having to stagger anything manually. 
        //if (getBlackboardAndRegister)
        //{
        //    if (ServiceLocator.For(this).TryGet<BlackboardController>(out blackboardController))
        //    {
        //        getBlackboardAndRegister = false;
        //        blackboard = blackboardController.GetBlackboard();
        //        flickerKey = blackboard.GetOrRegisterKey("flicker");
        //        blackboard.SetValue(flickerKey, flicker);
        //        blackboardController.RegisterExpert(this);
        //        blackboardController.RegisterWithPriorityGroup(this);
        //        state = LoadManager.State.Started;
        //    }
        //}
    }
}
