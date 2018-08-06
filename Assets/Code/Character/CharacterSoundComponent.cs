using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CharacterSoundComponent : MonoBehaviour
{
	public enum CharacterSound
	{
		Walk,
		Dash,
		Punch,
		Death,
		Charge,

		Count,
	}

	AudioSource[] _sounds;

	public void ManualAwake(CharacterDatabase.ViewData data, Transform parent)
	{
		_sounds = new AudioSource[(int)CharacterSound.Count];

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

		_sounds[(int)CharacterSound.Walk] = soundHolderWalk.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Walk].clip = data.walkSound;

		_sounds[(int)CharacterSound.Dash] = soundHolderDash.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Dash].clip = data.dashSound;

		_sounds[(int)CharacterSound.Punch] = soundHolderPunch.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Punch].clip = data.hitSound;

		_sounds[(int)CharacterSound.Death] = soundHolderDeath.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Death].clip = data.deathSound;

		_sounds[(int)CharacterSound.Charge] = soundHolderCharge.GetComponent<AudioSource>();
		_sounds[(int)CharacterSound.Charge].clip = data.chargeSound;
		_sounds[(int)CharacterSound.Charge].loop = true;
	}

	public void PlaySound(CharacterSound type)
	{
		_sounds[(int)type].Play();
	}

	public void StopSound(CharacterSound type, float fadeInSeconds = 0.5f)
	{
		if (_sounds[(int)type].isPlaying)
			Timing.RunCoroutine(_fadeSound(fadeInSeconds, (int)type));
	}


	IEnumerator<float> _fadeSound(float time, int sound)
	{
		float startVolume = _sounds[sound].volume;

		float fraction = 0;
		while(fraction < 1.0f)
		{
			fraction = time == 0 ? 1.0f : fraction + Time.deltaTime / time;

			_sounds[sound].volume = Mathf.Lerp(startVolume, 0.0f, fraction);

			yield return Timing.WaitForOneFrame;
		}

		_sounds[sound].Stop();
		_sounds[sound].volume = startVolume;

	}
}
