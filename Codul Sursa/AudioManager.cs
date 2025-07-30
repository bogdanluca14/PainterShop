using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Varibile Globale

    // Referinte Globale
    public static AudioManager instance;
    public AudioSource audioObject;

    // Variabile Locale

    private GameObject lastAudio;

    // Assignul referinte
    private void Awake()
    {
        if (instance == null)
            instance = this;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    // Porneste clip audio
    public void PlayAudioClip(AudioClip clip)
    {
        if (lastAudio != null)
            Destroy(lastAudio);

        AudioSource audioSource = Instantiate(audioObject, Vector3.zero, Quaternion.identity);
        lastAudio = audioSource.gameObject;

        audioSource.clip = clip;
        audioSource.volume = 0.7f;
        audioSource.Play();
        float clipLength = audioSource.clip.length;

        try
        {
            Destroy(audioSource.gameObject, clipLength);
        }
        catch { }
    }
}
