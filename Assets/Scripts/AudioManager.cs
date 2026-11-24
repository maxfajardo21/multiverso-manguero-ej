using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Musica del modo")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public static void Play(AudioClip clip)
    {
        if (clip == null || Instance == null || Instance.sfxSource == null) return;
        Instance.sfxSource.PlayOneShot(clip);
    }
}
