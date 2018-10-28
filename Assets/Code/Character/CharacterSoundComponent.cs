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
	RespawnSound,

	Count,
}

public class CharacterSoundComponent : MonoBehaviour
{	
	SoundData[] _sounds;

	public void ManualAwake(CharacterDatabase.ViewData data, Transform parent)
	{
		// create sound holders for all character sounds
		_sounds = new SoundData[(int)CharacterSound.Count];
		for (int i = 0; i < _sounds.Length; i++)
			_sounds[i] = new SoundData();

		CreateSounds(data, parent);
	}
	
	void CreateSounds(CharacterDatabase.ViewData data, Transform parent)
	{
		SoundManager SM = SoundManager.instance;

		// create all sounds through the sound manager
		SM.CreateSound(_sounds[(int)CharacterSound.Walk],         "WalkSound",        data.walkSound,    false, parent);
		SM.CreateSound(_sounds[(int)CharacterSound.Dash],         "DashSound",        data.dashSound,    false, parent);
		SM.CreateSound(_sounds[(int)CharacterSound.Punch],        "PunchSound",       data.hitSound,     false, parent);
		SM.CreateSound(_sounds[(int)CharacterSound.Death],        "DeathSound",       data.deathSound,   false, parent);
		SM.CreateSound(_sounds[(int)CharacterSound.Charge],       "ChargeSound",      data.chargeSound,  true,  parent);
		SM.CreateSound(_sounds[(int)CharacterSound.PowerupLoop],  "PowerUpLoopSound", null,              true,  parent);
		SM.CreateSound(_sounds[(int)CharacterSound.StunnedSound], "StunnedSound",     data.stunnedSound, false, parent);
		SM.CreateSound(_sounds[(int)CharacterSound.RespawnSound], "RespawnSound",     data.respawnSound, false, parent);
	}

	public void PlaySound(CharacterSound type, float duration = 0)
	{
		SoundManager SM = SoundManager.instance;
		SM.PlaySound(_sounds[(int)type], duration);
	}

	public void StopSound(CharacterSound type, float fadeInSeconds = 0.5f)
	{
		SoundManager SM = SoundManager.instance;
		SM.StopSound(_sounds[(int)type], fadeInSeconds);
	}

	public void SetClipToSound(CharacterSound type, AudioClip clip)
	{
		_sounds[(int)type].audioSource.clip = clip;
	}

	public void StopAll()
	{
		SoundManager SM = SoundManager.instance;

		for (int i =0; i< _sounds.Length; i++)
			SM.StopSound(_sounds[i], 0.25f);
	}
}
