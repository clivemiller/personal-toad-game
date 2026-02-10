using UnityEngine;

/// <summary>
/// Optional hook that can be invoked by <see cref="InSceneSetManager"/> when a set is being disabled.
/// Attach a derived component somewhere in the scene (commonly on the set root) and assign it in the manager.
/// </summary>
public abstract class InSceneSetDisableHook : MonoBehaviour
{
    public abstract void Run();
}
