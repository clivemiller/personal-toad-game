using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Makes an AudioSource loop as seamlessly as possible by alternating two DSP-scheduled sources
/// and crossfading around the loop point.
///
/// Usage:
/// - Put this on the same GameObject as the looping AudioSource.
/// - Assign a clip to the AudioSource.
/// - Leave <see cref="AudioSource.loop"/> enabled or disabled; this component will manage looping.
///
/// Notes:
/// - This can't fix a bad loop edit (non-zero-crossing loop point), but it masks clicks/gaps.
/// - Best results: trim the clip to loop cleanly and use PCM/Decompress On Load when possible.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SeamlessAudioLooper : MonoBehaviour
{
    [Header("Loop")]
    [Tooltip("Seconds of overlap used to crossfade at each loop point. Small values (0.01-0.10) are typical.")]
    [SerializeField]
    [Min(0f)]
    private float crossfadeSeconds = 0.05f;

    [Tooltip("How far ahead (DSP time) we schedule future loop starts. Higher is safer; too high delays reacting to changes.")]
    [SerializeField]
    [Min(0.02f)]
    private float scheduleLookaheadSeconds = 0.12f;

    [Tooltip("If true, starts looping automatically on enable (when the AudioSource has a clip).")]
    [SerializeField]
    private bool playOnEnable = true;

    private AudioSource sourceA;
    private AudioSource sourceB;

    private AudioClip activeClip;
    private double clipDurationSeconds;
    private float baseVolume;

    private double nextAStartDsp;
    private double nextBStartDsp;
    private double aSegmentStartDsp;
    private double bSegmentStartDsp;
    private double strideSeconds;

    private bool isRunning;

    private void Awake()
    {
        sourceA = GetComponent<AudioSource>();
        EnsureSecondSource();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartLooping();
        }
    }

    private void OnDisable()
    {
        StopLooping();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (sourceA == null || sourceB == null)
        {
            StopLooping();
            return;
        }

        if (activeClip == null)
        {
            StopLooping();
            return;
        }

        // If someone swapped the clip at runtime, restart cleanly.
        if (sourceA.clip != activeClip)
        {
            StartLooping();
            return;
        }

        double dspTime = AudioSettings.dspTime;

        // Keep both sources scheduled far enough ahead.
        if (dspTime + scheduleLookaheadSeconds >= nextAStartDsp)
        {
            ScheduleSegment(sourceA, nextAStartDsp);
            aSegmentStartDsp = nextAStartDsp;
            nextAStartDsp += strideSeconds;
        }

        if (dspTime + scheduleLookaheadSeconds >= nextBStartDsp)
        {
            ScheduleSegment(sourceB, nextBStartDsp);
            bSegmentStartDsp = nextBStartDsp;
            nextBStartDsp += strideSeconds;
        }

        // Drive crossfade volumes. (Main-thread changes aren't sample-accurate, but are usually sufficient.)
        sourceA.volume = baseVolume * GetSegmentGain(dspTime, aSegmentStartDsp);
        sourceB.volume = baseVolume * GetSegmentGain(dspTime, bSegmentStartDsp);
    }

    /// <summary>
    /// Starts (or restarts) seamless looping using the current AudioSource clip.
    /// </summary>
    public void StartLooping()
    {
        sourceA = GetComponent<AudioSource>();
        if (sourceA == null)
        {
            return;
        }

        EnsureSecondSource();

        activeClip = sourceA.clip;
        if (activeClip == null)
        {
            StopLooping();
            return;
        }

        ApplySettingsToSecondSource();

        // We manage looping ourselves.
        sourceA.loop = false;
        sourceB.loop = false;
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;

        baseVolume = Mathf.Clamp01(sourceA.volume);

        clipDurationSeconds = GetClipDurationSeconds(activeClip);
        if (clipDurationSeconds <= 0.0)
        {
            StopLooping();
            return;
        }

        float clampedCrossfade = crossfadeSeconds;
        // Crossfade cannot be >= half the clip or the schedule stride becomes invalid.
        float maxCrossfade = Mathf.Max(0f, (float)(clipDurationSeconds * 0.49));
        if (clampedCrossfade > maxCrossfade)
        {
            clampedCrossfade = maxCrossfade;
        }

        // This is the time between starts of alternating sources.
        // A starts, then B starts (duration - crossfade) later.
        double startOffset = clipDurationSeconds - clampedCrossfade;
        if (startOffset <= 0.001)
        {
            StopLooping();
            return;
        }

        strideSeconds = startOffset * 2.0;

        // Stop any previous schedule.
        sourceA.Stop();
        sourceB.Stop();

        // Ensure deterministic initial gains.
        sourceA.volume = 0f;
        sourceB.volume = 0f;

        double dspNow = AudioSettings.dspTime;
        double aStart = dspNow + Mathf.Max(0.02f, scheduleLookaheadSeconds);
        double bStart = aStart + startOffset;

        ScheduleSegment(sourceA, aStart);
        ScheduleSegment(sourceB, bStart);

        aSegmentStartDsp = aStart;
        bSegmentStartDsp = bStart;
        nextAStartDsp = aStart + strideSeconds;
        nextBStartDsp = bStart + strideSeconds;

        isRunning = true;
    }

    /// <summary>
    /// Stops seamless looping and stops both sources.
    /// </summary>
    public void StopLooping()
    {
        isRunning = false;

        if (sourceA != null)
        {
            sourceA.Stop();
            sourceA.volume = baseVolume > 0f ? baseVolume : sourceA.volume;
        }

        if (sourceB != null)
        {
            sourceB.Stop();
            sourceB.volume = 0f;
        }

        activeClip = null;
        clipDurationSeconds = 0.0;
        strideSeconds = 0.0;
        nextAStartDsp = 0.0;
        nextBStartDsp = 0.0;
        aSegmentStartDsp = double.NegativeInfinity;
        bSegmentStartDsp = double.NegativeInfinity;
    }

    private void EnsureSecondSource()
    {
        if (sourceA == null)
        {
            return;
        }

        // Prefer an existing extra AudioSource if someone already added one.
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources != null && sources.Length >= 2)
        {
            sourceB = sources[0] == sourceA ? sources[1] : sources[0];
        }
        else
        {
            sourceB = gameObject.AddComponent<AudioSource>();
        }

        ApplySettingsToSecondSource();
    }

    private void ApplySettingsToSecondSource()
    {
        if (sourceA == null || sourceB == null)
        {
            return;
        }

        // Keep the clip identical.
        sourceB.clip = sourceA.clip;

        // Match routing.
        sourceB.outputAudioMixerGroup = sourceA.outputAudioMixerGroup;

        // Match core playback characteristics.
        sourceB.mute = sourceA.mute;
        sourceB.bypassEffects = sourceA.bypassEffects;
        sourceB.bypassListenerEffects = sourceA.bypassListenerEffects;
        sourceB.bypassReverbZones = sourceA.bypassReverbZones;
        sourceB.priority = sourceA.priority;
        sourceB.pitch = sourceA.pitch;
        sourceB.panStereo = sourceA.panStereo;
        sourceB.spatialBlend = sourceA.spatialBlend;
        sourceB.reverbZoneMix = sourceA.reverbZoneMix;
        sourceB.dopplerLevel = sourceA.dopplerLevel;
        sourceB.spread = sourceA.spread;
        sourceB.rolloffMode = sourceA.rolloffMode;
        sourceB.minDistance = sourceA.minDistance;
        sourceB.maxDistance = sourceA.maxDistance;
        sourceB.spatialize = sourceA.spatialize;
        sourceB.spatializePostEffects = sourceA.spatializePostEffects;
        sourceB.ignoreListenerPause = sourceA.ignoreListenerPause;
        sourceB.ignoreListenerVolume = sourceA.ignoreListenerVolume;

        // We manage these.
        sourceB.loop = false;
        sourceB.playOnAwake = false;
        sourceB.volume = 0f;
    }

    private static double GetClipDurationSeconds(AudioClip clip)
    {
        if (clip == null)
        {
            return 0.0;
        }

        // Prefer sample-accurate duration.
        if (clip.frequency > 0)
        {
            return clip.samples / (double)clip.frequency;
        }

        return clip.length;
    }

    private void ScheduleSegment(AudioSource src, double startDsp)
    {
        if (src == null || activeClip == null)
        {
            return;
        }

        // Ensure we play from the start of the clip.
        src.timeSamples = 0;
        src.clip = activeClip;
        src.PlayScheduled(startDsp);
    }

    private float GetSegmentGain(double dspTime, double segmentStartDsp)
    {
        if (clipDurationSeconds <= 0.0)
        {
            return 0f;
        }

        double t = dspTime - segmentStartDsp;
        if (t < 0.0 || t >= clipDurationSeconds)
        {
            return 0f;
        }

        float gain = 1f;

        float cf = Mathf.Max(0f, crossfadeSeconds);
        if (cf > 0f)
        {
            if (t < cf)
            {
                gain = Mathf.Clamp01((float)(t / cf));
            }
            else if (t > clipDurationSeconds - cf)
            {
                gain = Mathf.Clamp01((float)((clipDurationSeconds - t) / cf));
            }
        }

        return gain;
    }

    private void OnValidate()
    {
        crossfadeSeconds = Mathf.Max(0f, crossfadeSeconds);
        scheduleLookaheadSeconds = Mathf.Max(0.02f, scheduleLookaheadSeconds);
    }
}
