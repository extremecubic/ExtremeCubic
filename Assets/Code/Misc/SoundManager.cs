using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

[Serializable]
public class SoundData
{
	public AudioSource     audioSource;
	public CoroutineHandle fadeHandle;
	public CoroutineHandle playDurationHandle;	
}

public class SoundManager : MonoBehaviour
{
	[SerializeField] SoundData _sharedPowerUpSoundLoop;

	public static SoundManager instance { get; private set; }

	void Awake()
	{
		instance = this;
	}

	void OnDestroy()
	{
		instance = null;	
	}

	public void CreateSound(SoundData data, string name, AudioClip clip, bool loop, Transform parent)
	{
		GameObject soundHolder = new GameObject(name, typeof(AudioSource));
		soundHolder.transform.SetParent(parent);

		data.audioSource = soundHolder.GetComponent<AudioSource>();
		data.audioSource.clip = clip;
		data.audioSource.loop = loop;
	}

	public void PlaySharedPowerUpLoop(AudioClip clip, float stopAfterSec)
	{
		AudioSource sound = _sharedPowerUpSoundLoop.audioSource;

		// if another shared powerup loop is playing, stop it
		if (sound.isPlaying)
		{
			_sharedPowerUpSoundLoop.playDurationHandle.IsRunning = false;
			sound.Stop();
		}

		// if the shared powerup loop is fading out, stop fade and reset to full volume
		if (_sharedPowerUpSoundLoop.fadeHandle.IsRunning)		
			_sharedPowerUpSoundLoop.fadeHandle.IsRunning = false;
					
		// set to new clip and play the loop for specific duration
		sound.clip = clip;
		_sharedPowerUpSoundLoop.playDurationHandle = Timing.RunCoroutine(_PlayForDuration(stopAfterSec, _sharedPowerUpSoundLoop));
	}

	public void StopSharedPowerUpLoop(float fadeTime)
	{
		_sharedPowerUpSoundLoop.playDurationHandle.IsRunning = false;
		_sharedPowerUpSoundLoop.fadeHandle = Timing.RunCoroutine(_FadeSound(fadeTime, _sharedPowerUpSoundLoop));
	}

	public void StopSound(SoundData data, float time = 0.5f)
	{
		if (data.audioSource.isPlaying)
			data.fadeHandle = Timing.RunCoroutineSingleton(_FadeSound(time, data), data.fadeHandle, SingletonBehavior.Abort);
	}

	public void SpawnAndPlaySound(AudioClip clip, float destroyAfter)
	{
		if (clip == null)
			return;

		// spawn object with audiosource
		GameObject soundHolder = new GameObject("soundOneUse", typeof(AudioSource));
		AudioSource audio = soundHolder.GetComponent<AudioSource>();

		// asign clip and play
		audio.clip = clip;
		audio.volume = Constants.masterEffectVolume;
		audio.Play();

		// delete after delay
		Destroy(soundHolder, destroyAfter);
	}

	public void PlaySound(SoundData data, float duration = 0.0f)
	{
		if (data.audioSource.clip != null)
		{
			if (data.fadeHandle.IsRunning)
				data.fadeHandle.IsRunning = false;

			data.audioSource.volume = Constants.masterEffectVolume;

			if (duration == 0)
				data.audioSource.Play();
			else
				data.playDurationHandle = Timing.RunCoroutineSingleton(_PlayForDuration(duration, data), data.playDurationHandle, SingletonBehavior.Overwrite);			
		}
	}

	IEnumerator<float> _PlayForDuration(float time, SoundData sound)
	{
		sound.audioSource.volume = Constants.masterEffectVolume;
		sound.audioSource.Play();
		yield return Timing.WaitForSeconds(time);
		sound.fadeHandle = Timing.RunCoroutine(_FadeSound(0.5f, sound));
	}

	IEnumerator<float> _FadeSound(float time, SoundData sound)
	{
		float startVolume = sound.audioSource.volume;
		
		float fraction = 0;
		while (fraction < 1.0f)
		{
			if (sound == null || sound.audioSource == null)
				yield break;

			fraction = time == 0 ? 1.0f : fraction + Time.deltaTime / time;

			sound.audioSource.volume = Mathf.Lerp(startVolume, 0.0f, fraction);

			yield return Timing.WaitForOneFrame;
		}

		sound.audioSource.Stop();		
	}
}
