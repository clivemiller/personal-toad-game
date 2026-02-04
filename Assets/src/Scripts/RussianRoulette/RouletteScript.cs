using UnityEngine;

public class RouletteScript : MonoBehaviour
{
    Animator visuals;
    SceneTransition sceneTransition;
    public RouletteSoundManager RouletteSoundManager;  
    public int GameState = 0;  // 0 = not started, 1 = spinning, 2 = sweating, 3 = pulling trigger, 3 = dead, 4 = survived
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        visuals = GetComponent<Animator>();
        sceneTransition = GetComponent<SceneTransition>();
        RouletteSoundManager = FindFirstObjectByType<RouletteSoundManager>();

        ActionUponGameState();
    }

    public void ActionUponGameState()
    {
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
                sceneTransition.LoadSceneByName("MainMenuStyled");

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
