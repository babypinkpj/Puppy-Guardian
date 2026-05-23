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
        StartCoroutine(countdown());
        songSource.clip = song;
        songSource.loop = true;
    }
    IEnumerator countdown()
    {
        yield return new WaitForSeconds(1.7f);
        SFXSource.PlayOneShot(sfxCountdown);
        yield return new WaitForSeconds(2.0f);
        SFXSource.PlayOneShot(sfxRiser);
        yield return new WaitForSeconds(1.3f);
        SFXSource.PlayOneShot(sfxGameStart);
        songSource.Play();
    }

    public void HoverUI() { SFXSource.PlayOneShot(sfxHover); }
    public void ClickUI() { SFXSource.PlayOneShot(sfxClick); }
}
