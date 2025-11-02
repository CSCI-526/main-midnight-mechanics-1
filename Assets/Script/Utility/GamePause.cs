using System;
using UnityEngine;

public static class GamePause
{
    public static bool IsPaused { get; private set; }
    public static event Action<bool> OnPauseChanged;

    public static void SetPaused(bool on)
    {
        if (IsPaused == on) return;
        IsPaused = on;

        // 冻结时间 & 全局静音
        Time.timeScale = on ? 0f : 1f;
        AudioListener.pause = on;

        OnPauseChanged?.Invoke(on);
    }

    public static void Toggle() => SetPaused(!IsPaused);
}