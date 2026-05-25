using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioGameplay : MonoBehaviour
{
    //song
    [Header("Music")]
    public AudioSource songSource;
    public AudioClip song;
    public AudioClip gameoverSong;

    [Header("SFX")]
    public AudioSource SFXSource;
    public AudioClip sfxCountdown, sfxRiser, sfxGameStart, sfxHover, sfxClick, sfxchaos, sfxpowerdown, 
        sfxbirds, sfxNear, sfxCountdown10, sfxTime, sfxHeal;

    private void Start()
    {
        songSource.clip = song;
        songSource.loop = true;
    }

    public void PlayNumberBeep() { SFXSource.PlayOneShot(sfxCountdown); }
    public void PlayStartRiser() { SFXSource.PlayOneShot(sfxRiser); }
    public void PlayGameStart() 
    { 
        SFXSource.PlayOneShot(sfxGameStart); 
        songSource.Play(); 
    }

    public void HoverUI() { SFXSource.PlayOneShot(sfxHover); }
    public void ClickUI() { SFXSource.PlayOneShot(sfxClick); }
}
