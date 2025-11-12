using UnityEngine;

public class SongSelection : MonoBehaviour
{
    public static SongSelection Instance { get; private set; }

    public string CurrentSongName = "Unknown";
    public int CurrentLevel = 0;

    void Awake()
    {
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SetSong(string name, int level)
    {
        
        if (Instance == null)
        {
            GameObject go = new GameObject("SongSelection");
            Instance = go.AddComponent<SongSelection>();
            DontDestroyOnLoad(go);
        }

        Instance.CurrentSongName = name;
        Instance.CurrentLevel = level;
        Debug.Log($"[SongSelection] SetSong → {name} (Lv.{level})");
    }
}
