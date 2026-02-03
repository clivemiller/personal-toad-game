using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class PersistentOneShotAudio : MonoBehaviour
{
    private AudioSource source;

    public static void Play(AudioClip clip, AudioSource template = null, float volumeScale = 1f)
    {
        if (clip == null)
        {
            return;
        }

        GameObject go = new GameObject($"OneShotAudio_{clip.name}");
        DontDestroyOnLoad(go);

        AudioSource src = go.AddComponent<AudioSource>();
        PersistentOneShotAudio lifetime = go.AddComponent<PersistentOneShotAudio>();
        lifetime.source = src;

        // Copy settings from a template source if provided.
        if (template != null)
        {
            src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            src.mute = template.mute;
            src.bypassEffects = template.bypassEffects;
            src.bypassListenerEffects = template.bypassListenerEffects;
            src.bypassReverbZones = template.bypassReverbZones;
            src.priority = template.priority;
            src.pitch = template.pitch;
            src.panStereo = template.panStereo;
            src.spatialBlend = template.spatialBlend;
            src.reverbZoneMix = template.reverbZoneMix;
            src.dopplerLevel = template.dopplerLevel;
            src.spread = template.spread;
            src.rolloffMode = template.rolloffMode;
            src.minDistance = template.minDistance;
            src.maxDistance = template.maxDistance;
            src.volume = template.volume * volumeScale;
        }
        else
        {
            src.volume = volumeScale;
            src.spatialBlend = 0f;
        }

        src.loop = false;
        src.playOnAwake = false;
        src.clip = clip;
        src.Play();

        lifetime.StartCoroutine(lifetime.DestroyWhenFinished());
    }

    private IEnumerator DestroyWhenFinished()
    {
        if (source == null || source.clip == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float pitch = Mathf.Max(0.01f, Mathf.Abs(source.pitch));
        float duration = (source.clip.length / pitch) + 0.05f;

        yield return new WaitForSecondsRealtime(duration);
        Destroy(gameObject);
    }
}
