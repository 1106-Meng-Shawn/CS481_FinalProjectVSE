// Edwin Sotres
// esotres@unr.edu

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource overlaySource;

    [Header("Audio Clips")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmLevel1;
    public AudioClip bgmLevel2;
    public AudioClip bgmVictory;
    public AudioClip bgmDefeat;

    [Header("SFX Clips")]
    public AudioClip buttonClick;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float overlayVolume = 0.8f;

    [Header("Fade Settings")]
    public float musicFadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            musicVolume = Instance.musicVolume;
            sfxVolume = Instance.sfxVolume;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        overlaySource = gameObject.AddComponent<AudioSource>();
        overlaySource.loop = false;
        overlaySource.playOnAwake = false;
        overlaySource.volume = overlayVolume;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name, true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name, true);
    }

    private void PlayMusicForScene(string sceneName, bool loop = true)
    {
        AudioClip clipToPlay = sceneName switch
        {
            "MainMenu" => bgmMainMenu,
            "SampleScene" => bgmLevel1,
            "Level2" => bgmLevel2,
            _ => bgmMainMenu
        };

        if (sceneName == "SampleScene")
        {
            int customIndex = PlayerPrefs.GetInt("NextLevelMusicIndex", -1);
            if (customIndex >= 0)
            {
                AudioClip[] options = { bgmLevel1, bgmLevel2 };
                if (customIndex < options.Length)
                {
                    StopAllCoroutines();
                    StartCoroutine(FadeMusicCoroutine(options[customIndex], true));
                    return;
                }
            }
        }

        if (clipToPlay != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeMusicCoroutine(clipToPlay, loop));
        }
    }

    private IEnumerator FadeMusicCoroutine(AudioClip newClip, bool loop)
    {
        if (musicSource.isPlaying)
        {
            float t = 0f;
            float startVol = musicSource.volume;
            while (t < musicFadeDuration)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / musicFadeDuration);
                yield return null;
            }
            musicSource.Stop();
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        float t2 = 0f;
        while (t2 < musicFadeDuration)
        {
            t2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t2 / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = musicVolume;
    }

    public void PlayOverlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(OverlayCoroutine(clip));
    }

    private IEnumerator OverlayCoroutine(AudioClip clip)
    {
        float originalVol = musicSource.volume;

        float t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(originalVol, musicVolume * 0.25f, t / musicFadeDuration);
            yield return null;
        }

        overlaySource.volume = 0f;
        overlaySource.clip = clip;
        overlaySource.Play();

        t = 0f;
        while (t < musicFadeDuration)
        {
            t += Time.deltaTime;
            overlaySource.volume = Mathf.Lerp(0f, overlayVolume, t / musicFadeDuration);
            yield return null;
        }

        overlaySource.volume = overlayVolume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayButtonClick() => PlaySFX(buttonClick);

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
}
