using UnityEngine;

public class RouletteSoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip gun_shoot;
    public AudioClip gun_click;
    public AudioClip revolver_spin;
    public AudioClip pickup_gun;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayGunShootSound()
    {
        PersistentOneShotAudio.Play(gun_shoot, audioSource);
    }

    public void PlayPickupGun()
    {
        audioSource.volume = 0.25f;
        PersistentOneShotAudio.Play(pickup_gun, audioSource);
        audioSource.volume = 1f;
    }


    public void PlayGunClick()
    {
        PersistentOneShotAudio.Play(gun_click, audioSource);
    }

    public void StartRevolverSpin()
    {
        if (revolver_spin == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        audioSource.clip = revolver_spin;
        audioSource.loop = true;
        // slightly lower volume for looping
        audioSource.volume = 0.5f;
        audioSource.Play();
    }

    public void StopRevolverSpin()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.volume = 1f;
    }
}
