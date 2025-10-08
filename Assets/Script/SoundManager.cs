using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip backgroundMusic;
    public AudioClip placeClip;
    public AudioClip winClip;
    public AudioClip loseClip;

    private bool musicStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartMusicIfNeeded();
    }

    public void StartMusicIfNeeded()
    {
        if (musicStarted) return;
        if (musicSource == null || backgroundMusic == null) return;
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.Play();
        musicStarted = true;
    }

    public void PlayPlace()
    {
        if (sfxSource != null && placeClip != null)
        {
            sfxSource.PlayOneShot(placeClip);
        }
    }

    public void PlayWin()
    {
        if (sfxSource != null && winClip != null)
        {
            sfxSource.PlayOneShot(winClip);
        }
    }

    public void PlayLose()
    {
        if (sfxSource != null && loseClip != null)
        {
            sfxSource.PlayOneShot(loseClip);
        }
    }
}


