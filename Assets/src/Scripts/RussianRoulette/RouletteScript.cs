using UnityEngine;

public class RouletteScript : MonoBehaviour
{
    Animator visuals;
    SceneTransition sceneTransition;
    public RouletteSoundManager RouletteSoundManager;  
    public int GameState = 0;  // 0 = not started, 1 = spinning, 2 = sweating, 3 = pulling trigger, 3 = dead, 4 = survived

    private void EnsureReferences()
    {
        if (visuals == null)
        {
            visuals = GetComponent<Animator>();
        }

        if (sceneTransition == null)
        {
            sceneTransition = GetComponent<SceneTransition>();
        }

        if (RouletteSoundManager == null)
        {
            RouletteSoundManager = FindFirstObjectByType<RouletteSoundManager>();
        }
    }

    public void Reset()
    {
        EnsureReferences();
        CancelInvoke();

        if (RouletteSoundManager != null)
        {
            RouletteSoundManager.StopRevolverSpin();
        }

        GameState = 0;
        ActionUponGameState();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureReferences();

        ActionUponGameState();
    }

    public void ActionUponGameState()
    {
        EnsureReferences();

        switch (GameState)
        {
            case 0:
                // Not started
                visuals.SetTrigger("still");

                break;
            case 1:
                // Spinning
                visuals.SetTrigger("spin");
                // wait for animation to finish.
                // spin animation is 5.02 seconds long
                Invoke("SetToSweating", 5.02f);

                break;
            case 2:
                // Sweating
                if (RouletteSoundManager != null)
                {
                    // Defensive: if the spin stop animation event didn't fire,
                    // ensure the looping spin audio never carries into sweat/other states.
                    RouletteSoundManager.StopRevolverSpin();
                }
                visuals.SetTrigger("sweat");
                break;
            case 3:
                // Pulling trigger
                // 1/6 chance of dying
                int random = Random.Range(0, 6);
                Debug.Log("Random number for trigger pull: " + random);
                if (random == 0)
                {
                    GameState = 4; // Dead
                }
                else
                {
                    GameState = 5; // Survived
                }
                ActionUponGameState();
                break;
            case 4:
                RouletteSoundManager.PlayGunShootSound();
                sceneTransition.LoadScene();

                break;
            case 5:
                RouletteSoundManager.PlayGunClick();
                visuals.SetTrigger("live");
                Invoke("Survive", 1.08f);

                break;
            case 6:
                // Exiting the roulette game (e.g., Stop button). Make sure no
                // scheduled state changes or looping audio continues after we leave.
                CancelInvoke();
                if (RouletteSoundManager != null)
                {
                    RouletteSoundManager.StopRevolverSpin();
                }
                SceneTransitionManager.Load("Desk", false);

                break;
            default:
                break;
        }
    }

    private void SetToSweating()
    {
        GameState = 2;
        ActionUponGameState();
    }

    private void Survive()
    {
        GameState = 0;
        ActionUponGameState();
    }

    // Animation Event wrappers
    public void StartRevolverSpin()
    {
        if (RouletteSoundManager != null)
        {
            RouletteSoundManager.StartRevolverSpin();
        }
    }

    public void StopRevolverSpin()
    {
        if (RouletteSoundManager != null)
        {
            RouletteSoundManager.StopRevolverSpin();
        }
    }

    public void PlayPickupGun()
    {
        if (RouletteSoundManager != null)
        {
            RouletteSoundManager.PlayPickupGun();
        }
    }
}
