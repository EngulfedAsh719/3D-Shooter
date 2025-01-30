using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    public AudioClip gunShotClip;
    public AudioClip reloadClip;
    public AudioClip footstepClip;
    public AudioClip emptyGunClip; 
    public AudioClip newWaveSound;

    [Header("Zombie Sounds")]
    public AudioClip zombieDeathClip;
    public AudioClip zombieIdleClip;
    public AudioClip zombieAttackClip;

    [Header("Pickup Sounds")]
    public AudioClip medkitPickupClip;
    
    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("3D Sound Settings")]
    [Range(1f, 50f)]
    public float maxZombieAudioDistance = 20f;
    [Range(0.1f, 10f)]
    public float minTimeBetweenZombieIdles = 3f;
    [Range(1, 5)]
    public int maxSimultaneousZombieIdles = 3;

    [Header("Default Volume Levels")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSFXVolume = 0.8f;
    [Range(0f, 1f)] public float defaultZombieVolume = 0.8f;
    [Range(0f, 1f)] public float defaultFootstepsVolume = 0.6f;

    private AudioSource audioSource; 
    private AudioSource footstepSource;
    private AudioSource musicSource;
    private readonly List<AudioSource> activeZombieAudioSources = new List<AudioSource>();
    private float lastZombieIdleTime;

    // Текущие уровни громкости
    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float zombieVolume;
    private float footstepsVolume;

    #region Volume Properties
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateAllVolumes();
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.Save();
        }
    }

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            UpdateMusicVolume();
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.Save();
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            UpdateSFXVolume();
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }
    }

    public float ZombieVolume
    {
        get => zombieVolume;
        set
        {
            zombieVolume = Mathf.Clamp01(value);
            UpdateZombieVolume();
            PlayerPrefs.SetFloat("ZombieVolume", zombieVolume);
            PlayerPrefs.Save();
        }
    }

    public float FootstepsVolume
    {
        get => footstepsVolume;
        set
        {
            footstepsVolume = Mathf.Clamp01(value);
            UpdateFootstepsVolume();
            PlayerPrefs.SetFloat("FootstepsVolume", footstepsVolume);
            PlayerPrefs.Save();
        }
    }
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);
        zombieVolume = PlayerPrefs.GetFloat("ZombieVolume", defaultZombieVolume);
        footstepsVolume = PlayerPrefs.GetFloat("FootstepsVolume", defaultFootstepsVolume);
    }

    private void InitializeAudioSources()
    {
        // Основной источник для звуковых эффектов
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Источник для шагов
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepClip;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;

        // Источник для музыки
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.priority = 0;

        UpdateAllVolumes();
        
        if (backgroundMusic != null)
        {
            StartBackgroundMusic();
        }
    }

    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateSFXVolume();
        UpdateZombieVolume();
        UpdateFootstepsVolume();
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    private void UpdateSFXVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume * masterVolume;
        }
    }

    private void UpdateZombieVolume()
    {
        foreach (var zombieSource in activeZombieAudioSources)
        {
            if (zombieSource != null)
            {
                zombieSource.volume = zombieVolume * masterVolume;
            }
        }
    }

    private void UpdateFootstepsVolume()
    {
        if (footstepSource != null)
        {
            footstepSource.volume = footstepsVolume * masterVolume;
        }
    }

    public void PlayEmptyGunSound()
    {
        PlaySound(emptyGunClip);
    }

    // Остальные методы остаются без изменений...
    public void PlayGunShot()
    {
        PlaySound(gunShotClip);
    }

    public void PlayReload()
    {
        PlaySound(reloadClip);
    }

    public void StartFootsteps()
    {
        if (!footstepSource.isPlaying)
        {
            footstepSource.Play();
        }
    }

    public void StopFootsteps()
    {
        if (footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    public void PlayZombieAttack(Vector3 position)
    {
        Play3DSound(zombieAttackClip, position, zombieVolume);
    }

    public void PlayZombieDeath(Vector3 position)
    {
        Play3DSound(zombieDeathClip, position, zombieVolume);
    }

    public bool TryPlayZombieIdle(Vector3 position)
    {
        if (Time.time - lastZombieIdleTime < minTimeBetweenZombieIdles || 
            activeZombieAudioSources.Count >= maxSimultaneousZombieIdles)
            return false;

        lastZombieIdleTime = Time.time;
        Play3DSound(zombieIdleClip, position, zombieVolume * Random.Range(0.8f, 1f));
        return true;
    }

    private AudioSource Play3DSound(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return null;

        GameObject audioObject = new GameObject("3D_Sound");
        audioObject.transform.position = position;
        
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = maxZombieAudioDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.volume = volumeMultiplier * masterVolume;
        source.pitch = Random.Range(0.95f, 1.05f);
        source.Play();

        if (clip == zombieIdleClip)
        {
            activeZombieAudioSources.Add(source);
        }
        else
        {
            Destroy(audioObject, clip.length);
        }

        return source;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    public void PlayNewWaveSound()
    {
        if (newWaveSound != null)
        {
            audioSource.PlayOneShot(newWaveSound, sfxVolume * masterVolume);
        }

        else Debug.Log("Звук анимации не назначем");
    }

    public void PlayMedkitPickup(Vector3 position)
    {
        if (medkitPickupClip != null)
        {
            audioSource.PlayOneShot(medkitPickupClip, sfxVolume * masterVolume);
        }
    }

    private void Update()
    {
        CleanupZombieAudioSources();
    }

    private void CleanupZombieAudioSources()
    {
        activeZombieAudioSources.RemoveAll(source => source == null || !source.isPlaying);
    }

    #region Music Control
    public void StartBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeBackgroundMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }
    #endregion

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += PauseBackgroundMusic;
            GameManager.Instance.OnGameResumed += ResumeBackgroundMusic;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= PauseBackgroundMusic;
            GameManager.Instance.OnGameResumed -= ResumeBackgroundMusic;
        }
    }
}