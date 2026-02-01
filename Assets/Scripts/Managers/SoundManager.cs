using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    SFX,
    Music
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> soundList;
    public static SoundManager instance;

    private AudioSource musicSource,sfxSource;


    public float volume;
    public float pitch;

    private void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        soundList = new List<AudioClip>();

        //audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        //AudioSource.PlayClipAtPoint

    }
}
