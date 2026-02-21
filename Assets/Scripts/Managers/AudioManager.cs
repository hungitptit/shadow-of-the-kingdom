using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource bgmSource;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        bgmSource.loop = true;
        bgmSource.Play();
    }
}