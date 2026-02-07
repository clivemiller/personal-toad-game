using UnityEngine;

public class SoundCleaner : MonoBehaviour
{
    public AudioSource audioSource;
    public void Clean()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.volume = 1f;
    }
}
