using UnityEngine;

public class ResetRouletteState : InSceneSetDisableHook
{
    [SerializeField]
    private RouletteScript rouletteScript;

    public override void Run()
    {
        if (rouletteScript == null)
        {
            rouletteScript = FindFirstObjectByType<RouletteScript>();
        }

        if (rouletteScript != null)
        {
            rouletteScript.Reset();
        }
    }
}
