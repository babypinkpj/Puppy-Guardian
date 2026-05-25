using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioResult : MonoBehaviour
{
    [Header("Music")]
    public AudioSource songSource;
    public AudioClip loopsong;
    public AudioClip endloopsong;
    public AudioClip song;

    [Header("SFX")]
    public AudioSource SFXSource;
    public AudioClip sfxPop, sfxHover, sfxClick;
    void Start()
    {
        SFXSource.volume = 1;
        songSource.volume = 1;
        songSource.clip = loopsong;
        songSource.loop = true;
    }

    public void drumloop()
    {
        songSource.Play();
    }

    public void enddrumloop()
    {
        songSource.Stop();
        songSource.loop = false;
        songSource.clip = endloopsong;
        songSource.Play();
    }

    public void startsongagain()
    {
        songSource.loop = true;
        songSource.clip = song;
        songSource.Play();
    }

    public void HoverUI() { SFXSource.PlayOneShot(sfxHover); }
    public void ClickUI() { SFXSource.PlayOneShot(sfxClick); }
}
