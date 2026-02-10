using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple in-scene sets, allowing only one to be visible at a time.
/// The first set in the list is the default and will be visible on scene load.
/// </summary>
public class InSceneSetManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Configured sets to manage. The first one will be the default visible set.")]
    private List<InSceneSetConfig> configuredSets = new List<InSceneSetConfig>();

    [SerializeField]
    [Tooltip("All soundCleaners")]
    private List<SoundCleaner> soundCleaners = new List<SoundCleaner>();

    private Dictionary<string, SoundCleaner> soundCleanersByName = new Dictionary<string, SoundCleaner>();
    private Dictionary<string, GameObject> setsByName = new Dictionary<string, GameObject>();
    private Dictionary<string, InSceneSetDisableHook> disableHooksBySetName = new Dictionary<string, InSceneSetDisableHook>();
    private GameObject currentVisibleSet;

    [System.Serializable]
    private class InSceneSetConfig
    {
        public GameObject set;

        [Tooltip("Optional script to run when this set is disabled (e.g., to reset state).")]
        public InSceneSetDisableHook onDisabled;
    }

    private void Awake()
    {
        InitializeSets();
        InitializeSoundManagers();
    }

    /// <summary>
    /// Initializes the sets, indexes them by name, and shows only the default (first) set.
    /// </summary>
    private void InitializeSets()
    {
        setsByName.Clear();
        disableHooksBySetName.Clear();

        // Index all sets by name
        if (configuredSets != null)
        {
            foreach (InSceneSetConfig config in configuredSets)
            {
                if (config != null && config.set != null)
                {
                    setsByName[config.set.name] = config.set;
                    disableHooksBySetName[config.set.name] = config.onDisabled;
                    config.set.SetActive(false); // Hide all sets initially
                }
            }
        }

        // Show the default set (first in the list)
        if (configuredSets != null && configuredSets.Count > 0 && configuredSets[0] != null && configuredSets[0].set != null)
        {
            configuredSets[0].set.SetActive(true);
            currentVisibleSet = configuredSets[0].set;
        }
    }

    private void RunDisableHookForSet(string setName)
    {
        if (string.IsNullOrWhiteSpace(setName))
        {
            return;
        }

        if (disableHooksBySetName.TryGetValue(setName, out InSceneSetDisableHook hook) && hook != null)
        {
            hook.Run();
        }
    }

    private void InitializeSoundManagers()
    {
        soundCleanersByName.Clear();

        // Index all sets by name
        foreach (SoundCleaner soundCleaner in soundCleaners)
        {
            if (soundCleaner != null)
            {
                soundCleanersByName[soundCleaner.name] = soundCleaner;
            }
        }
    }

    private void CleanSound(string setName)
    {
        if (soundCleanersByName.TryGetValue(setName, out SoundCleaner cleaner))
        {
            if (cleaner != null)
            {
                cleaner.Clean();    
            }
        }
    }


    /// <summary>
    /// Switches to a different set by name, hiding the currently visible set.
    /// </summary>
    /// <param name="setName">The name of the set to switch to.</param>
    /// <returns>True if the switch was successful, false otherwise.</returns>
    public bool SwitchToSet(string setName)
    {
        if (!setsByName.ContainsKey(setName))
        {
            Debug.LogWarning($"InSceneSetManager: Set with name '{setName}' not found.");
            return false;
        }

        GameObject targetSet = setsByName[setName];

        // Don't do anything if switching to the already visible set
        if (targetSet == currentVisibleSet)
        {
            return true;
        }

        if (currentVisibleSet != null)
        {
            CleanSound(currentVisibleSet.name);
            RunDisableHookForSet(currentVisibleSet.name);
        }

        // Hide the currently visible set
        if (currentVisibleSet != null)
        {
            currentVisibleSet.SetActive(false);
        }

        // Show the target set
        targetSet.SetActive(true);
        currentVisibleSet = targetSet;

        return true;
    }

    /// <summary>
    /// Switches to a set by index in the original list.
    /// </summary>
    /// <param name="index">The index of the set to switch to.</param>
    /// <returns>True if the switch was successful, false otherwise.</returns>
    public bool SwitchToSetByIndex(int index)
    {
        if (configuredSets == null || index < 0 || index >= configuredSets.Count)
        {
            Debug.LogWarning($"InSceneSetManager: Index {index} is out of range.");
            return false;
        }

        GameObject targetSet = configuredSets[index].set;
        if (targetSet == null)
        {
            Debug.LogWarning($"InSceneSetManager: Set at index {index} is null.");
            return false;
        }

        return SwitchToSet(targetSet.name);
    }

    /// <summary>
    /// Gets the currently visible set.
    /// </summary>
    /// <returns>The currently visible GameObject set.</returns>
    public GameObject GetCurrentSet()
    {
        return currentVisibleSet;
    }

    /// <summary>
    /// Gets the name of the currently visible set.
    /// </summary>
    /// <returns>The name of the currently visible set, or null if none is visible.</returns>
    public string GetCurrentSetName()
    {
        return currentVisibleSet != null ? currentVisibleSet.name : null;
    }

    /// <summary>
    /// Gets a set by name without switching to it.
    /// </summary>
    /// <param name="setName">The name of the set to retrieve.</param>
    /// <returns>The GameObject if found, null otherwise.</returns>
    public GameObject GetSet(string setName)
    {
        return setsByName.ContainsKey(setName) ? setsByName[setName] : null;
    }
}
