using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardRecentHighlightManager
{
    private readonly Func<key[]> _getKeys;
    private readonly Func<List<Material>> _getMaterials;
    private readonly Func<int> _getHistorySize;
    private readonly Action<int> _setHistorySize;
    private readonly dial _historyDial;
    private readonly UnityEngine.Object _context;

    private readonly List<int> _recentKeyHistory = new List<int>();
    private bool _loggedMissingRecentHighlightMaterials;
    private bool _suppressHistoryDialEvent;

    public KeyboardRecentHighlightManager(
        Func<key[]> getKeys,
        Func<List<Material>> getMaterials,
        Func<int> getHistorySize,
        Action<int> setHistorySize,
        dial historyDial,
        UnityEngine.Object context)
    {
        _getKeys = getKeys;
        _getMaterials = getMaterials;
        _getHistorySize = getHistorySize;
        _setHistorySize = setHistorySize;
        _historyDial = historyDial;
        _context = context;
    }

    public void Initialize()
    {
        if (_historyDial != null)
        {
            _historyDial.isNotched = true;
            _historyDial.onPercentChangedEventLocal.AddListener(OnHistoryDialPercentChanged);
        }
        Reset();
    }

    public void LoadSettings(int desiredHistorySize, float savedDialPercent)
    {
        SetRecentKeyHistorySize(Mathf.Max(0, desiredHistorySize), false);

        if (_historyDial != null)
        {
            _suppressHistoryDialEvent = true;
            if (!float.IsNaN(savedDialPercent) && savedDialPercent >= 0f)
            {
                _historyDial.setPercent(savedDialPercent);
            }
            else
            {
                UpdateHistoryVisuals(true);
            }
            _suppressHistoryDialEvent = false;
        }
        else
        {
            ApplyRecentKeyHighlights();
        }
    }

    public void RegisterRecentKeyPress(int keyIndex)
    {
        key[] keys = _getKeys();
        if (keys == null || keyIndex < 0 || keyIndex >= keys.Length)
        {
            return;
        }

        if (!HasHighlightMaterials() || _getHistorySize() <= 0)
        {
            if (_recentKeyHistory.Count > 0)
            {
                _recentKeyHistory.Clear();
            }
            ApplyRecentKeyHighlights();
            return;
        }

        _recentKeyHistory.Remove(keyIndex);
        _recentKeyHistory.Insert(0, keyIndex);
        TrimRecentKeyHistory();
        ApplyRecentKeyHighlights();
    }

    public void Refresh() => ApplyRecentKeyHighlights();

    public void OnValidate()
    {
        if (_getHistorySize() < 0)
        {
            _setHistorySize(0);
        }
        SetRecentKeyHistorySize(_getHistorySize(), true);
    }

    public void OnDestroy()
    {
        if (_historyDial != null)
        {
            _historyDial.onPercentChangedEventLocal.RemoveListener(OnHistoryDialPercentChanged);
        }
    }

    private void Reset()
    {
        _recentKeyHistory.Clear();
        _loggedMissingRecentHighlightMaterials = false;
        UpdateHistoryVisuals(true);
    }

    private bool HasKeys()
    {
        key[] keys = _getKeys();
        return keys != null && keys.Length > 0;
    }

    private bool HasHighlightMaterials()
    {
        var mats = _getMaterials();
        return mats != null && mats.Count > 0;
    }

    private int ClampedHistorySize(int size)
    {
        return HasHighlightMaterials() ? Mathf.Clamp(size, 0, _getMaterials().Count) : 0;
    }

    private void ApplyRecentKeyHighlights()
    {
        if (!HasKeys())
        {
            return;
        }

        key[] keys = _getKeys();
        int limit = ClampedHistorySize(_getHistorySize());
        if (limit <= 0)
        {
            if (Application.isPlaying && !_loggedMissingRecentHighlightMaterials && !HasHighlightMaterials())
            {
                Debug.LogWarning($"{_context.name}: recent key highlights are disabled because no highlight materials are assigned.", _context);
            }

            _loggedMissingRecentHighlightMaterials = true;

            foreach (key k in keys)
            {
                k?.ClearRecentHighlight();
            }
            return;
        }

        _loggedMissingRecentHighlightMaterials = false;

        foreach (key k in keys)
        {
            k?.ClearRecentHighlight();
        }

        var mats = _getMaterials();
        int highlightCount = Mathf.Min(limit, _recentKeyHistory.Count);
        for (int i = 0; i < highlightCount; i++)
        {
            int keyIndex = _recentKeyHistory[i];
            if (keyIndex < 0 || keyIndex >= keys.Length)
            {
                continue;
            }

            Material highlightMat = mats[Mathf.Min(i, mats.Count - 1)];
            if (highlightMat == null)
            {
                continue;
            }

            keys[keyIndex].SetRecentHighlight(highlightMat);
        }
    }

    private void OnHistoryDialPercentChanged()
    {
        if (_historyDial == null || _suppressHistoryDialEvent)
        {
            return;
        }

        int maxSteps = Mathf.Max(0, _historyDial.notchSteps - 1);
        int targetSize = maxSteps > 0 ? Mathf.RoundToInt(Mathf.Clamp01(_historyDial.percent) * maxSteps) : 0;
        SetRecentKeyHistorySize(targetSize, false);
    }

    private void SetRecentKeyHistorySize(int newSize, bool syncDial)
    {
        _setHistorySize(Mathf.Max(0, newSize));
        TrimRecentKeyHistory();
        UpdateHistoryVisuals(syncDial);
    }

    private void TrimRecentKeyHistory()
    {
        int limit = ClampedHistorySize(_getHistorySize());
        if (limit <= 0)
        {
            if (_recentKeyHistory.Count > 0)
            {
                _recentKeyHistory.Clear();
            }
            return;
        }

        if (_recentKeyHistory.Count > limit)
        {
            _recentKeyHistory.RemoveRange(limit, _recentKeyHistory.Count - limit);
        }
    }

    private void UpdateHistoryVisuals(bool syncDial)
    {
        if (_historyDial != null)
        {
            int available = HasHighlightMaterials() ? _getMaterials().Count : _getHistorySize();
            int steps = Mathf.Max(2, available + 1);
            if (_historyDial.notchSteps != steps)
            {
                _historyDial.notchSteps = steps;
            }

            if (syncDial)
            {
                int maxSteps = steps - 1;
                int clampedSize = Mathf.Min(ClampedHistorySize(_getHistorySize()), maxSteps);
                _suppressHistoryDialEvent = true;
                _historyDial.setPercent(maxSteps > 0 ? (float)clampedSize / maxSteps : 0f);
                _suppressHistoryDialEvent = false;
            }
        }

        ApplyRecentKeyHighlights();
    }
}

