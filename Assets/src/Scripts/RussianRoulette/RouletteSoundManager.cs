using UnityEngine;

public class RouletteSoundManager : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip gun_pickup;
    public AudioClip gun_spin;
    public AudioClip gun_shoot;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    
    public void PlayGunPickupSound()
    {
        PersistentOneShotAudio.Play(gun_pickup, audioSource);
    }

    public void PlayGunSpinSound()
    {
        PersistentOneShotAudio.Play(gun_spin, audioSource);
    }

    public void PlayGunShootSound()
    {
        PersistentOneShotAudio.Play(gun_shoot, audioSource);
    }
}
