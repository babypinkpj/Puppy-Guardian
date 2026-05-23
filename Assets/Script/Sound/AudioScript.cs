using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Rendering.DebugUI;

public class AudioScript : MonoBehaviour
{
    //song
    private AudioSource songStart;
    public AudioClip songSelection;
    //sfx
    public AudioSource startSource;
    public AudioSource songSource;
    public AudioClip sfxStart, sfxHover, sfxClick, sfxReturn, sfxSlide, sfxSlideMusic,
        sfxSelectedSlide, sfxSelectedSlideMusic, sfxStartGame;

    private void Start()
    {
        songStart = GetComponent<AudioSource>();
        songStart.clip = songSelection;
        songStart.loop = true;
        StartCoroutine(song());

    }
    IEnumerator song()
    {
        startSource.PlayOneShot(sfxStart);
        yield return new WaitForSeconds(0.4f); songStart.Play();
    }

    public void HoverUI() { startSource.PlayOneShot(sfxHover); }
    public void ClickUI() { startSource.PlayOneShot(sfxClick); }
    public void ReturnUI() { startSource.PlayOneShot(sfxReturn); }
    public void SlideUI() { startSource.PlayOneShot(sfxSlide); }
    public void SlideSongUI() { songSource.PlayOneShot(sfxSlideMusic); }
    public void SelectedSlideUI() { startSource.PlayOneShot(sfxSelectedSlide); }
    public void SelectedSlideSongUI() { songSource.PlayOneShot(sfxSelectedSlideMusic); }
    public void StartGameUI() { startSource.PlayOneShot(sfxStartGame); songStart.Stop(); }
}
