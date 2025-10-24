using UnityEngine;
using System;

public class PlayerInputTracker : MonoBehaviour
{
    private Analytics analytics;
    private string sessionId;
    private bool inGreenZone = false;

    [Header("Linked Systems")]
    [SerializeField] private LevelRunner levelRunner;

    [Header("Level Info")]
    public int levelNumber = 1;   
    void Awake()
    {
        sessionId = "Session_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    void Start()
    {
        analytics = UnityEngine.Object.FindFirstObjectByType<Analytics>();

        if (levelRunner == null)
            levelRunner = FindFirstObjectByType<LevelRunner>();

        if (levelRunner != null)
        {
            levelRunner.OnLevelApplied += OnLevelStarted;
            levelRunner.OnLevelEnded += OnLevelEnded;
        }

        Debug.Log($"[Tracker] Started {sessionId}, Level={levelNumber}");
    }

    void OnDestroy()
    {
        if (levelRunner != null)
        {
            levelRunner.OnLevelApplied -= OnLevelStarted;
            levelRunner.OnLevelEnded -= OnLevelEnded;
        }
    }

    private void OnLevelStarted()
    {
        
        if (levelNumber < 1) levelNumber = 1;
        // Debug.Log($"[Tracker] Level start #{levelNumber}");
        // analytics.LogAction(sessionId, levelNumber, "LEVEL_START", false);
    }

    private void OnLevelEnded()
    {
        // Debug.Log($"[Tracker] Level end #{levelNumber}");
        // analytics.LogAction(sessionId, levelNumber, "LEVEL_END", false);
        levelNumber++;
    }

    void Update()
    {
        string key = "";
        // if (Input.GetKeyDown(KeyCode.LeftArrow))  key = "L";
        // if (Input.GetKeyDown(KeyCode.RightArrow)) key = "R";
        // if (Input.GetKeyDown(KeyCode.UpArrow))    key = "U";
        // if (Input.GetKeyDown(KeyCode.DownArrow))  key = "D";
        if (Input.GetKeyDown(KeyCode.Space))      key = "Space";

        if (key != "")
        {
            bool success = (key == "Space" && inGreenZone);
            analytics.LogAction(sessionId, levelNumber, key, success);
            Debug.Log($"[Analytics] {key} pressed — success={success}");
        }
    }

    public void EnterGreenZone() => inGreenZone = true;
    public void ExitGreenZone()  => inGreenZone = false;

    public void SetLevelNumber(int num) => levelNumber = num;
}
