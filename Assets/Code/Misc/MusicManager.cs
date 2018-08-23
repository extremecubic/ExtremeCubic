using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class MusicManager : MonoBehaviour
{
	[SerializeField] SoundData _sharedPowerUpSoundLoop;

	public static MusicManager instance { get; private set; }

	void Awake()
	{
		instance = this;
	}

	void OnDestroy()
	{
		instance = null;	
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
		{
			_sharedPowerUpSoundLoop.fadeHandle.IsRunning = false;
			sound.volume = _sharedPowerUpSoundLoop.originalVolume;
		}

		// set to new clip and play the loop for specific duration
		sound.clip = clip;
		_sharedPowerUpSoundLoop.playDurationHandle = Timing.RunCoroutine(_PlayForDuration(stopAfterSec, _sharedPowerUpSoundLoop));
	}

	public void StopSharedPowerUpLoop(float fadeTime)
	{
		_sharedPowerUpSoundLoop.playDurationHandle.IsRunning = false;
		_sharedPowerUpSoundLoop.fadeHandle = Timing.RunCoroutine(_FadeSound(fadeTime, _sharedPowerUpSoundLoop));
	}

	IEnumerator<float> _PlayForDuration(float time, SoundData sound)
	{
		sound.audioSource.Play();
		yield return Timing.WaitForSeconds(time);
		sound.fadeHandle = Timing.RunCoroutine(_FadeSound(0.5f, sound));
	}

	IEnumerator<float> _FadeSound(float time, SoundData sound)
	{
		float startVolume = sound.audioSource.volume;
		sound.originalVolume = startVolume;

		float fraction = 0;
		while (fraction < 1.0f)
		{
			fraction = time == 0 ? 1.0f : fraction + Time.deltaTime / time;

			sound.audioSource.volume = Mathf.Lerp(startVolume, 0.0f, fraction);

			yield return Timing.WaitForOneFrame;
		}

		sound.audioSource.Stop();
		sound.audioSource.volume = startVolume;
	}
}
