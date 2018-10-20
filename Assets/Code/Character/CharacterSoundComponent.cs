using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

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
		SoundManager MM = SoundManager.instance;

		MM.CreateSound(_sounds[(int)CharacterSound.Walk],         "WalkSound",        data.walkSound,    false, parent);
		MM.CreateSound(_sounds[(int)CharacterSound.Dash],         "DashSound",        data.dashSound,    false, parent);
		MM.CreateSound(_sounds[(int)CharacterSound.Punch],        "PunchSound",       data.hitSound,     false, parent);
		MM.CreateSound(_sounds[(int)CharacterSound.Death],        "DeathSound",       data.deathSound,   false, parent);
		MM.CreateSound(_sounds[(int)CharacterSound.Charge],       "ChargeSound",      data.chargeSound,  true,  parent);
		MM.CreateSound(_sounds[(int)CharacterSound.PowerupLoop],  "PowerUpLoopSound", null,              true,  parent);
		MM.CreateSound(_sounds[(int)CharacterSound.StunnedSound], "StunnedSound",     data.stunnedSound, false, parent);
	}

	public void PlaySound(CharacterSound type, float duration = 0)
	{
		SoundManager MM = SoundManager.instance;
		MM.PlaySound(_sounds[(int)type], duration);
	}

	public void StopSound(CharacterSound type, float fadeInSeconds = 0.5f)
	{
		SoundManager MM = SoundManager.instance;
		MM.StopSound(_sounds[(int)type], fadeInSeconds);
	}

	public void SetClipToSound(CharacterSound type, AudioClip clip)
	{
		_sounds[(int)type].audioSource.clip = clip;
	}

	public void StopAll()
	{
		SoundManager MM = SoundManager.instance;

		for (int i =0; i< _sounds.Length; i++)
			MM.StopSound(_sounds[i], 0.25f);
	}
}
