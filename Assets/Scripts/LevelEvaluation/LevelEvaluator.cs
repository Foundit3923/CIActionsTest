using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using Steamworks;
using UnityEngine;
using UnityEngine.VFX;
using UnityServiceLocator;

public class LevelEvaluator : MonoBehaviour
{
    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey playerKey, monsterKey;

    private int monsterCount;
    private int livingMonsterCount;
    private int deadMonsterCount;

    private List<GameObject> monsterList = new();
    private List<EnemyProperties> monsterPropertiesList = new();

    private int playerCount;
    private int livingPlayerCount;
    private int deadPlayerCount;

    private List<GameObject> playerList = new();
    private List<PlayerInformationManager> playerSettingsList = new();

    [SerializeField] public float maxChaosAngle;
    [SerializeField] public float maxOrderAngle;
    private float angleRange;
    private float angleChangeDelta;

    [SerializeField] public GameObject rotationAnchor;

    public delegate void LevelAttemptEndEvent();
    public static event LevelAttemptEndEvent OnLevelAttemptEnd;

    public delegate void LevelCompletedEvent();
    public static event LevelCompletedEvent OnLevelCompleted;

    public delegate void LevelFailedEvent();
    public static event LevelFailedEvent OnLevelFailed;

    [SerializeField] public FlameBinder chaosFlameBinder;
    private VisualEffect chaosFlame;
    [SerializeField] public FlameBinder orderFlameBinder;
    private VisualEffect orderFlame;

    [SerializeField] public List<Brasier> brasiers = new();

    private DifficultyManager difficultyManager;

    //Order and Chaos flame deltas
    private float yValue100 = 3.5f;
    private Vector3 flame100 = Vector3.one * 2f;
    private float smoke100 = 24f;
    private float sparks100 = 1f;
    private float yValueDelta;
    private float flameDelta;
    private float smokeDelta;
    private float sparksDelta;

    private void Awake()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        playerKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Players);
        monsterKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Monsters);
        chaosFlame = chaosFlameBinder.GetComponent<VisualEffect>();
        orderFlame = orderFlameBinder.GetComponent<VisualEffect>();

        yValueDelta = yValue100 / 100;
        flameDelta = flame100.x / 100;
        smokeDelta = smoke100 / 100;
        sparksDelta = sparks100 / 100;
        difficultyManager = ServiceLocator.For(this).Get<DifficultyManager>();
        angleRange = maxChaosAngle - maxOrderAngle;
        angleChangeDelta = angleRange / 100;
        //subscribe level evaluation mehtod to level end event
        OnLevelAttemptEnd += LevelEvaluation;
        OnLevelFailed += UpdateDifficultyValuesForFailure;
        OnLevelCompleted += UpdateDifficultyValuesForCompleted;
        EnemyControllerStateMachine.OnMonsterCreated += UpdateMonsterValues;
        EnemyControllerStateMachine.OnMonsterDeath += UpdateMonsterValues;
        PlayerInformationManager.OnPlayerCreated += UpdatePlayerValues;
        PlayerInformationManager.OnPlayerDeath += UpdatePlayerValues;
    }

    void Start() => ResetScale();

    private void ResetScale()
    {
        rotationAnchor.transform.Rotate(new Vector3(-rotationAnchor.transform.rotation.x, 0, 0));
        rotationAnchor.transform.Rotate(new Vector3(maxChaosAngle, 0, 0));
        //positive rotation flame values (Chaos)
        SetFlameWithBinder(chaosFlameBinder, chaosFlame, 100);
        //negative rotation flame values (Order)
        SetFlameWithBinder(orderFlameBinder, orderFlame, 0);
    }

    private void UpdateScale(float delta)
    {
        //Rotate scale
        rotationAnchor.transform.Rotate(new Vector3(delta, 0, 0));

        //Establish variables 
        float maxAngle = Mathf.Abs(maxChaosAngle);
        float neutralRot = 0f;
        float a = Mathf.Abs(rotationAnchor.transform.rotation.x);
        float b = 0f;

        //Get angles
        if (a != neutralRot)
        {
            b = maxAngle - a;
        }

        //ConvertPlayerToZombie angles into percentages
        a *= angleChangeDelta;
        b = 100 - a;

        //Evaluate which direction delta is moving towards
        if (delta >= 0)
        {
            (b, a) = (a, b);
        }

        //a and b should be whole, positive values as they represent percentages
        //positive rotation flame values (Chaos)
        SetFlameWithBinder(chaosFlameBinder, chaosFlame, a);
        //negative rotation flame values (Order)
        SetFlameWithBinder(orderFlameBinder, orderFlame, b);

    }

    public void SetFlameWithBinder(FlameBinder binder, VisualEffect visualEffect, float successValue)
    {
        if (successValue < 20)
        {
            successValue = 20;
        }

        Vector3 pos = binder.gameObject.transform.position;
        binder.gameObject.transform.position = new Vector3(pos.x, yValueDelta * successValue, pos.z);
        binder.FlameScaleValue = Vector3.one * (flameDelta * successValue);
        binder.SmokeSizeValue = smokeDelta * successValue;
        binder.SparksSizeValue = sparksDelta * successValue;
        if (binder.IsValid(visualEffect))
        {
            binder.UpdateBinding(visualEffect);
        }
    }

    // Update is called once per frame
    void Update()
    {
        deadMonsterCount = 0;
        foreach (EnemyProperties monsterProperties in monsterPropertiesList)
        {
            if (monsterProperties.isDead)
            {
                deadMonsterCount++;
            }
        }

        if ((monsterCount - deadMonsterCount) < livingMonsterCount)
        {
            //Get current percentage of living monsters to total monsters
            //Evaluate the change in percentage and multiply that by the angleChangeDelta
            float currentPercentage = monsterCount / livingMonsterCount;
            livingMonsterCount = monsterCount - deadMonsterCount;
            float newPercentage = monsterCount / livingMonsterCount;
            float percentDiff = newPercentage - currentPercentage;
            float angleDelta = Mathf.Abs(percentDiff) * angleChangeDelta;
            UpdateScale(-angleDelta);
        }
        else if ((monsterCount - deadMonsterCount) > livingMonsterCount)
        {
            //Get current percentage of living monsters to total monsters
            //Evaluate the change in percentage and multiply that by the angleChangeDelta
            float currentPercentage = monsterCount / livingMonsterCount;
            livingMonsterCount = monsterCount - deadMonsterCount;
            UpdateScale(angleChangeDelta);
            float newPercentage = monsterCount / livingMonsterCount;
            float percentDiff = newPercentage - currentPercentage;
            float angleDelta = Mathf.Abs(percentDiff) * angleChangeDelta;
            UpdateScale(angleDelta);
        }

        if (playerSettingsList.Count > 0)
        {
            int endlevel = 1;
            foreach (PlayerInformationManager player in playerSettingsList)
            {
                //don't trigger if anyone is alive
                //Will only stay 1 if everyone is dead
                endlevel &= player.playerProperties.isDead ? 1 : 0;
            }

            if (endlevel > 0)
            {
                OnLevelAttemptEnd?.Invoke();
            }
        }
    }

    //Interaction mthod
    public void PlayerInteraction() => OnLevelAttemptEnd?.Invoke();

    //Level Evaluation method
    public void LevelEvaluation()
    {
        //if balance or tipped towards order don't light a brasier
        Vector3 rotation = rotationAnchor.transform.rotation.eulerAngles;
        if (rotation.x <= 0)
        {
            //level passed
            OnLevelCompleted?.Invoke();
        }
        else
        {
            //Run failed
            //light a brasier
            int failures = 0;
            foreach (Brasier brasier in brasiers)
            {
                if (!brasier.isLit)
                {
                    brasier.SetActive(true);
                    failures++;
                    break;
                }
                else
                {
                    failures++;
                }
            }

            if (failures == brasiers.Count)
            {
                OnLevelFailed?.Invoke();
            }
        }
    }

    private void UpdateDifficultyValuesForCompleted()
    {
        Dictionary<string, object> updatedSettings = difficultyManager.GetValues();
        updatedSettings["NightsSurvived"] = (int)updatedSettings["NightsSurvived"] + 1;
        difficultyManager.SetValues(updatedSettings);

    }

    private void UpdateDifficultyValuesForFailure()
    {
        Dictionary<int, Dictionary<string, object>> settingsHistory = difficultyManager.GameplaySettings.GetHistory();
        if (settingsHistory.Keys.Count > 0)
        {
            difficultyManager.SetValues(settingsHistory[0]);
        }

        foreach (Brasier brasier in brasiers)
        {
            brasier.SetActive(false);
        }

        ResetScale();
    }

    private void UpdateMonsterValues(EnemyControllerStateMachine monster)
    {
        //This is being called for one of two reasons.
        //A monster was created, or a monster died
        Preconditions.CheckNotNull(monster, nameof(monster));
        EnemyControllerContext context = monster.getContext();
        if (context != null)
        {
            if (context.isDead)
            {
                deadMonsterCount++;
                if (monsterCount > 0)
                {
                    if ((monsterCount - deadMonsterCount) < livingMonsterCount)
                    {
                        livingMonsterCount = monsterCount - deadMonsterCount;
                        UpdateScale(-angleChangeDelta);

                    }
                    else
                    {
                        livingMonsterCount = monsterCount - deadMonsterCount;
                        UpdateScale(angleChangeDelta);
                    }
                }

                if (monsterList.Contains(monster.gameObject))
                {
                    monsterList.Remove(monster.gameObject);
                }
            }
            else
            {
                monsterCount++;
                if (!monsterList.Contains(monster.gameObject))
                {
                    monsterList.Add(monster.gameObject);
                }

                livingMonsterCount = monsterList.Count;
            }
        }
    }
    private void UpdatePlayerValues(PlayerInformationManager player)
    {
        Preconditions.CheckNotNull(player, nameof(player));
        if (player.playerProperties.isDead)
        {

            if (playerList.Contains(player.gameObject))
            {
                deadPlayerCount++;
                playerList.Remove(player.gameObject);
                livingPlayerCount = playerCount - deadPlayerCount;
            }
        }
        else
        {
            if (!playerList.Contains(player.gameObject))
            {
                playerCount++;
                playerList.Add(player.gameObject);
            }

            livingPlayerCount = playerList.Count;
        }
    }

    private void UpdatePlayerValues(PlayerInformationManager player, GameObject saviorOrKiller)
    {
        Preconditions.CheckNotNull(player, nameof(player));
        if (player.playerProperties.isDead)
        {

            if (playerList.Contains(player.gameObject))
            {
                deadPlayerCount++;
                playerList.Remove(player.gameObject);
                livingPlayerCount = playerCount - deadPlayerCount;
            }
        }
        else
        {
            if (!playerList.Contains(player.gameObject))
            {
                playerCount++;
                playerList.Add(player.gameObject);
            }

            livingPlayerCount = playerList.Count;
        }
    }

    public void ResetData()
    {
        monsterCount = 0;
        livingMonsterCount = 0;
        deadMonsterCount = 0;
        monsterList.Clear();
        monsterPropertiesList.Clear();
    }
}
