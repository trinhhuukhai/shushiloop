using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public partial class SoundManager : MonoBehaviour
{
    public static SoundManager ins;

    [Header("Sound Library (SO)")]
    public SoundLibrary soundLibrary;

    private AudioSource bgSource;
    private AudioSource sfxSource;
    private Dictionary<SoundID, SoundEntry> soundMap;

    void Awake()
    {
        // Singleton
        if (ins != null && ins != this)
        {
            Destroy(gameObject);
            return;
        }
        ins = this;
        DontDestroyOnLoad(gameObject);

        // T?o 2 AudioSource reuse cho BGM & SFX
        bgSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        bgSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;

        // ?? data t? SO vào dictionary
        soundMap = new Dictionary<SoundID, SoundEntry>();
        foreach (var entry in soundLibrary.sounds)
            soundMap[entry.id] = entry;
        //bgSource.mute = !DataManager.ins.setupData.isMusic;

    }

    #region BGM
    //public static void PlayMusicBg(SoundID id)
    //{

    //    if (!ins.soundMap.TryGetValue(id, out var entry)) return;
    //    if (!DataManager.ins.setupData.isMusic) return;

    //    //Debug.Log("play nhac");
    //    ins.bgSource.clip = entry.clip;
    //    ins.bgSource.volume = entry.volume;
    //    ins.bgSource.loop = entry.loop; ins.bgSource.Play();
    //    ins.bgSource.mute = !DataManager.ins.setupData.isMusic;
    //}
    public static void PlayMusicBg(SoundID id)
    {
        if (ins == null)
            return;

        if (!ins.soundMap.TryGetValue(id, out var entry))
            return;

        ins.bgSource.clip = entry.clip;
        ins.bgSource.volume = entry.volume;
        ins.bgSource.loop = entry.loop;
        ins.bgSource.mute = !DataManager.ins.setupData.isMusic;

        ins.bgSource.Play();
    }


    public static void StopMusicBg() => ins.bgSource.Stop();
    //public static void SetupMusicBg(bool on)
    //{
    //    DataManager.ins.setupData.isMusic = on;
    //    ins.bgSource.mute = !on;
    //}
    public static void SetupMusicBg(bool on)
    {
        if (ins == null)
            return;

        DataManager.ins.setupData.isMusic = on;
        ins.bgSource.mute = !on;

        DataManager.ins.SaveData();
    }

    public static void UpdateVolumeBg(float v) => ins.bgSource.volume = v;
    //public static void SetupSound(bool on) => ins.sfxSource.mute = !on;

    public static void SetupSound(bool on)
    {
        if (ins == null)
            return;

        DataManager.ins.setupData.isSound = on;
        ins.sfxSource.mute = !on;

        DataManager.ins.SaveData();
    }

    #endregion

    #region SFX
    public static void PlayEfx(SoundID id)
    {
        if (!ins.soundMap.TryGetValue(id, out var entry)) return;
        if (!DataManager.ins.setupData.isSound) return;

        ins.sfxSource.PlayOneShot(entry.clip, entry.volume);
    }

    public static void StopAllEfx() => ins.sfxSource.Stop();
    #endregion

    #region Music Transition (optional)
    public void ChangeMusic(SoundID id, float duration, UnityAction onComplete = null)
    {
        if (!ins.soundMap.TryGetValue(id, out var entry)) return;
        StartCoroutine(FadeAndSwitch(ins.bgSource, entry, duration, onComplete));
    }

    private IEnumerator FadeAndSwitch(AudioSource src, SoundEntry entry, float dur, UnityAction cb)
    {
        float half = dur * 0.5f;
        float startVol = src.volume;

        // fade out
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(startVol, 0f, t / half);
            yield return null;
        }

        // switch clip
        src.clip = entry.clip;
        src.loop = entry.loop;
        src.Play();

        // fade in
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(0f, entry.volume, t / half);
            yield return null;
        }

        cb?.Invoke();
    }
    #endregion

    #region CONTINUOUS SFX

    // D? li?u cho m?i loop ?ang ch?y
    private class LoopData
    {
        public SoundEntry entry;
        public float rate;
        public Coroutine coroutine;
    }
    private Dictionary<SoundID, LoopData> _loopingData = new Dictionary<SoundID, LoopData>();

    /// <summary>
    /// Start m?t loop cho EFX id v?i t?n su?t repeatsPerSecond (l?n/giây).
    /// N?u ?ã có loop v?i id này, ch? c?p nh?t rate, KHÔNG play l?i hay restart coroutine.
    /// </summary>
    public static void StartLoopingEfx(SoundID id, float repeatsPerSecond)
    {
        if (!ins.soundMap.TryGetValue(id, out var entry)) return;

        // N?u ?ã có loop, ch? update rate
        if (ins._loopingData.TryGetValue(id, out var data))
        {
            data.rate = repeatsPerSecond;
            return;
        }

        // L?n ??u: t?o data, play ngay m?t shot, r?i start coroutine
        var newData = new LoopData
        {
            entry = entry,
            rate = repeatsPerSecond
        };
        ins._loopingData[id] = newData;

        // Play ngay m?t l?n
        if (DataManager.ins.setupData.isSound)
            ins.sfxSource.PlayOneShot(entry.clip, entry.volume);

        // Start coroutine
        newData.coroutine = ins.StartCoroutine(ins.LoopEfxCoroutine(id));
    }

    /// <summary>
    /// D?ng loop EFX cho id (n?u ?ang ch?y).
    /// </summary>
    public static void StopLoopingEfx(SoundID id)
    {
        if (ins._loopingData.TryGetValue(id, out var data))
        {
            ins.StopCoroutine(data.coroutine);
            ins._loopingData.Remove(id);
        }
    }

    private IEnumerator LoopEfxCoroutine(SoundID id)
    {
        var data = _loopingData[id];

        while (true)
        {
            // Ch? interval
            float interval = 1f / Mathf.Max(0.0001f, data.rate);
            yield return new WaitForSeconds(interval);

            if (DataManager.ins.setupData.isSound)
                sfxSource.PlayOneShot(data.entry.clip, data.entry.volume);
        }
    }

    #endregion


    #region THROTTLED SFX

    // l?u th?i ?i?m l?n cu?i cùng play t?ng SoundID
    private Dictionary<SoundID, float> _lastEfxTime = new Dictionary<SoundID, float>();

    /// <summary>
    /// Ch? cho phép play t?i ?a 1 l?n m?i minInterval giây cho cùng 1 SoundID.
    /// </summary>
    public static void PlayEfxThrottled(SoundID id, float minInterval = 0.1f)
    {
        float now = Time.time;
        if (ins._lastEfxTime.TryGetValue(id, out var last) && now - last < minInterval)
            return;                      // ch?a ?? th?i gian cách nhau, b? qua

        ins._lastEfxTime[id] = now;
        PlayEfx(id);                     // g?i bình th??ng
    }

    #endregion
}


