using UnityEngine;

/// <summary>
/// Manages setting, checking, and resetting dialog conditions using PlayerPrefs.
/// </summary>
public static class ConditionHandler
{
    private const string Prefix = "BASE_COND_";
    private const string KeysTracker = "BASE_COND_KEYS";

    /// <summary>
    /// Sets a condition to true or false.
    /// </summary>
    public static void SetCondition(string conditionName, bool value = true)
    {
        if (string.IsNullOrEmpty(conditionName)) return;

        PlayerPrefs.SetInt(Prefix + conditionName, value ? 1 : 0);
        TrackKey(conditionName);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Checks if a condition is true.
    /// </summary>
    public static bool HasCondition(string conditionName)
    {
        if (string.IsNullOrEmpty(conditionName)) return true; // No requirement by default

        return PlayerPrefs.GetInt(Prefix + conditionName, 0) == 1;
    }

    /// <summary>
    /// Resets all conditions tracked by the ConditionHandler.
    /// </summary>
    public static void ResetAllConditions()
    {
        string allKeys = PlayerPrefs.GetString(KeysTracker, "");
        string[] keys = allKeys.Split(',');

        foreach (var key in keys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.DeleteKey(Prefix + key);
            }
        }

        PlayerPrefs.DeleteKey(KeysTracker);
        PlayerPrefs.Save();
        Debug.Log("ConditionHandler: All conditions reset.");
    }

    private static void TrackKey(string key)
    {
        string allKeys = PlayerPrefs.GetString(KeysTracker, "");
        string searchKey = key + ",";
        
        if (!allKeys.Contains(searchKey))
        {
            PlayerPrefs.SetString(KeysTracker, allKeys + searchKey);
        }
    }
}
