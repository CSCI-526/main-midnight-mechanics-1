using UnityEngine;
using UnityEngine.SceneManagement;

public class SongButton : MonoBehaviour
{
    [SerializeField] private string songName = "Unknown Song";
    [SerializeField] private int songLevel = 1;

    public void OnSongSelected()
    {
        SongSelection.SetSong(songName, songLevel);
        Debug.Log($"[SongButton] Selected: {songName} (Lv.{songLevel})");

        
        SceneManager.LoadScene("PlayScene");
    }
}
