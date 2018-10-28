using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

// stores a audiosource with it's own
// handles for playing and fading the
// sound for a set duration
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

	// create a gameobject with a audiosource and sets it's properties
	// that reference to the audiosource is set in the passed in "SoundData" structure
	// for later acces
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
		// only stop sound if it is playing and it is not already fading out
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

	// play an entire sound or play a sound for a set duration
	public void PlaySound(SoundData data, float duration = 0.0f)
	{
		if (data.audioSource.clip != null)
		{
			// check if the sound is already playing and is fading out
			// in that case abort fadeout and reset volume so it can be played again
			if (data.fadeHandle.IsRunning)
				data.fadeHandle.IsRunning = false;

			data.audioSource.volume = Constants.masterEffectVolume;

			// if duration is set to 0 play normally
			// else start the coroutine that playes the sound fo a set duration
			if (duration == 0)
				data.audioSource.Play();
			else
				data.playDurationHandle = Timing.RunCoroutineSingleton(_PlayForDuration(duration, data), data.playDurationHandle, SingletonBehavior.Overwrite);			
		}
	}

	IEnumerator<float> _PlayForDuration(float time, SoundData sound)
	{
		// set and play
		sound.audioSource.volume = Constants.masterEffectVolume;
		sound.audioSource.Play();

		// wait for duration
		yield return Timing.WaitForSeconds(time);

		// start coroutine that fades out the sound
		sound.fadeHandle = Timing.RunCoroutine(_FadeSound(0.5f, sound));
	}

	IEnumerator<float> _FadeSound(float time, SoundData sound)
	{
		// get current volume of sound
		float startVolume = sound.audioSource.volume;
		
		float fraction = 0;
		while (fraction < 1.0f)
		{
			// break out if the sound would get destroyed during fade
			if (sound == null || sound.audioSource == null)
				yield break;

			// avoid division by zero by setting the fraction directly to 1.0f
			// if we want the sound to be stopped directly
			fraction += time == 0 ? 1.0f : Time.deltaTime / time;

			sound.audioSource.volume = Mathf.Lerp(startVolume, 0.0f, fraction);

			yield return Timing.WaitForOneFrame;
		}

		sound.audioSource.Stop();		
	}
}
