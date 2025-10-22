using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelProgressHUD : MonoBehaviour
{
    [SerializeField] private LevelRunner runner;
    [SerializeField] private Slider slider;          // Min=0, Max=1, Interactable=false
    [SerializeField] private TMP_Text percentText;  

    [SerializeField] private bool showPercentText = true;

    void Awake()
    {
        if (!runner) runner = FindObjectOfType<LevelRunner>(true);
        if (!slider) slider = GetComponentInChildren<Slider>(true);
    }

    void Update()
    {
        if (!runner || !slider) return;

        float p = runner.Progress01;
        slider.value = p;

        if (percentText)
        {
            if (showPercentText)
            {
                int pct = Mathf.RoundToInt(p * 100f);
                percentText.SetText($"{pct}%");
            }
            else
            {
                percentText.SetText(string.Empty);
            }
        }
    }
}