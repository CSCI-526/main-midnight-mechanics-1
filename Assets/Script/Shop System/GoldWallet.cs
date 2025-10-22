using System;
using UnityEngine;

public sealed class GoldWallet : MonoBehaviour
{
    [Header("Start Gold")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField, Min(0)] private int startGold = 100;

    public event Action<int> OnChanged;

    public int Gold { get; private set; }

    private void Awake()
    {
        if (initializeOnAwake) Set(startGold);
    }

    public void Set(int value)
    {
        Gold = Mathf.Max(0, value);
        OnChanged?.Invoke(Gold);
    }

    public void Add(int delta)
    {
        if (delta == 0) return;
        Gold = Mathf.Max(0, Gold + delta);
        OnChanged?.Invoke(Gold);
    }

    public bool TrySpend(int price)
    {
        if (price <= 0) return true;
        if (Gold < price) return false;
        Gold -= price;
        OnChanged?.Invoke(Gold);
        return true;
    }

    // 可选：外部想重置时调用
    public void ResetToStart() => Set(startGold);
}