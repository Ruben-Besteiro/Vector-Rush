using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool loop = true;

    public static MusicManager Instance;
    private AudioSource audioSource;

    // La idea es que la música case con el nivel, y dé pistas al jugador sobre dónde saltar
    // Pero como dicha música no existe, pues he puesto una del Sonic

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        
        if (musicClip != null)
        {
            audioSource.clip = musicClip;
            audioSource.volume = 0.5f;
            audioSource.loop = loop;
            audioSource.playOnAwake = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("MusicPlayer: No se ha asignado ningún AudioClip en el inspector.");
        }
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public void PlayMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void RestartMusic()
    {
        audioSource.Stop();
        audioSource.Play();
    }
}
