using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

[Serializable]
public class SoundData
{
	public AudioSource audioSource;
	public CoroutineHandle fadeHandle;
	public CoroutineHandle playDurationHandle;
	[HideInInspector] public float originalVolume;
}

public enum CharacterSound
{
	Walk,
	Dash,
	Punch,
	Death,
	Charge,
	PowerupLoop,
	StunnedSound,

	Count,
}

public class CharacterSoundComponent : MonoBehaviour
{	
	SoundData[] _sounds;

	public void ManualAwake(CharacterDatabase.ViewData data, Transform parent)
	{
		_sounds = new SoundData[(int)CharacterSound.Count];
		for (int i = 0; i < _sounds.Length; i++)
			_sounds[i] = new SoundData();

		CreateSounds(data, parent);
	}
	
	void CreateSounds(CharacterDatabase.ViewData data, Transform parent)
	{
		GameObject soundHolderWalk = new GameObject("walkSound", typeof(AudioSource));
		soundHolderWalk.transform.SetParent(parent);

		GameObject soundHolderDash = new GameObject("dashSound", typeof(AudioSource));
		soundHolderDash.transform.SetParent(parent);

		GameObject soundHolderPunch = new GameObject("punchSound", typeof(AudioSource));
		soundHolderPunch.transform.SetParent(parent);

		GameObject soundHolderDeath = new GameObject("DeathSound", typeof(AudioSource));
		soundHolderDeath.transform.SetParent(parent);

		GameObject soundHolderCharge = new GameObject("ChargeSound", typeof(AudioSource));
		soundHolderCharge.transform.SetParent(parent);

		GameObject soundHolderpowerLoop = new GameObject("PowerUpLoopSound", typeof(AudioSource));
		soundHolderpowerLoop.transform.SetParent(parent);

		GameObject soundHolderStunned = new GameObject("StunnedSound", typeof(AudioSource));
		soundHolderStunned.transform.SetParent(parent);

		_sounds[(int)CharacterSound.Walk].audioSource = soundHolderWalk.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Walk].audioSource.clip = data.walkSound;

		_sounds[(int)CharacterSound.Dash].audioSource = soundHolderDash.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Dash].audioSource.clip = data.dashSound;

		_sounds[(int)CharacterSound.Punch].audioSource = soundHolderPunch.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Punch].audioSource.clip = data.hitSound;

		_sounds[(int)CharacterSound.Death].audioSource = soundHolderDeath.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Death].audioSource.clip = data.deathSound;

		_sounds[(int)CharacterSound.Charge].audioSource = soundHolderCharge.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Charge].audioSource.clip = data.chargeSound;
		_sounds[(int)CharacterSound.Charge].audioSource.loop = true;

		_sounds[(int)CharacterSound.PowerupLoop].audioSource = soundHolderpowerLoop.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.PowerupLoop].audioSource.loop = true;

		_sounds[(int)CharacterSound.StunnedSound].audioSource = soundHolderStunned.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.StunnedSound].audioSource.clip = data.stunnedSound;
		_sounds[(int)CharacterSound.StunnedSound].audioSource.loop = true;
	}

	public void PlaySound(CharacterSound type, float duration = 0)
	{
		if (_sounds[(int)type].fadeHandle.IsRunning)
		{
			_sounds[(int)type].fadeHandle.IsRunning = false;
			_sounds[(int)type].audioSource.volume = _sounds[(int)type].originalVolume;
		}

		if (duration == 0)
			_sounds[(int)type].audioSource.Play();
		else
			_sounds[(int)type].playDurationHandle = Timing.RunCoroutineSingleton(_PlayForDuration(duration, type), _sounds[(int)type].playDurationHandle, SingletonBehavior.Overwrite);
	}

	public void StopSound(CharacterSound type, float fadeInSeconds = 0.5f)
	{
		if (_sounds[(int)type].audioSource.isPlaying)
			_sounds[(int)type].fadeHandle = Timing.RunCoroutineSingleton(_FadeSound(fadeInSeconds, (int)type), _sounds[(int)type].fadeHandle, SingletonBehavior.Abort);		
	}

	public void SetClipToSound(CharacterSound type, AudioClip clip)
	{
		_sounds[(int)type].audioSource.clip = clip;
	}

	public void StopAll()
	{
		for (int i =0; i< _sounds.Length; i++)
		{
			if (_sounds[i].audioSource.isPlaying)
				_sounds[i].fadeHandle = Timing.RunCoroutineSingleton(_FadeSound(0.5f, i), _sounds[i].fadeHandle, SingletonBehavior.Abort);
		}
	}

	IEnumerator<float> _PlayForDuration(float time, CharacterSound type)
	{
		_sounds[(int)type].audioSource.Play();
		yield return Timing.WaitForSeconds(time);
		_sounds[(int)type].fadeHandle = Timing.RunCoroutine(_FadeSound(0.5f, (int)type));
	}

	IEnumerator<float> _FadeSound(float time, int sound)
	{
		float startVolume = _sounds[sound].audioSource.volume;
		_sounds[sound].originalVolume = startVolume;

		float fraction = 0;
		while(fraction < 1.0f)
		{
			// sound can be null if sound is fading out while unity is destroying objects on scene change
			// null check to be safe
			if (_sounds[sound] == null)
				yield break;

			fraction = time == 0 ? 1.0f : fraction + Time.deltaTime / time;

			_sounds[sound].audioSource.volume = Mathf.Lerp(startVolume, 0.0f, fraction);

			yield return Timing.WaitForOneFrame;
		}

		_sounds[sound].audioSource.Stop();
		_sounds[sound].audioSource.volume = startVolume;

	}
}
