using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour{
    public AudioSource[] audioSources;
    public List<AudioSource> audioSources2 = new List<AudioSource>();
    public GameObject audioSourcePrefab;
    public SoundEffect unlock;
    public SoundEffect pickUp;
    public SoundEffect removeNode;
    public SoundEffect removeArrow;
    public SoundEffect levelComplete;
    public SoundEffect rewind;
    public SoundEffect swapNode;
    public SoundEffect changeArrowDir;
    public SoundEffect deny;
    public SoundEffect deny2;
    public SoundEffect brushA;

    public static AudioManager instance;

    void Awake(){
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
        }
        else {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public void PlaySound(SoundEffect sound, bool playReverse = false){

        AudioSource audioSource = GetAudioSource();

        if (sound == null | audioSource == null) return;

        sound.Play(audioSource, playReverse);
    }

    public void StopSound(SoundEffect sound) {
        if (sound == null) return;

        sound.Stop();
    }

    public void StartFadeOut(SoundEffect sound) {
        if (sound == null | sound.source == null) return;

        StartCoroutine(_FadeOut(sound.source, 0.3f));
    }

    public void PlaySoundWithDelay(SoundEffect sound, 
        float delay, bool playReverse = false){

        StartCoroutine(_PlaySoundWithDelay(sound, delay, playReverse));
    }

    private IEnumerator _PlaySoundWithDelay(SoundEffect sound, 
        float delay, bool playReverse = false) {

        yield return new WaitForSeconds(delay);
        PlaySound(sound, playReverse);
    }
    public IEnumerator _FadeOut(AudioSource audioSource, 
        float FadeTime) {

        float startVolume = audioSource.volume;

        while (audioSource.volume > 0) {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    private AudioSource GetAudioSource() {
        for (int i = 0; i < audioSources2.Count; i++) {
            if (audioSources2[i].isPlaying) 
                continue;
            else 
                return audioSources2[i];
        }

        AudioSource audioSource = Instantiate(audioSourcePrefab, Vector3.zero, Quaternion.identity).GetComponent<AudioSource>();
        audioSource.transform.SetParent(this.transform);
        audioSources2.Add(audioSource);
        return null;
    }

}
