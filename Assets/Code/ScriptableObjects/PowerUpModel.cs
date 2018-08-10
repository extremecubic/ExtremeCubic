using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PowerUpType : int
{
	InfiniteDash   = 0x00000001,
	SuperSpeed     = 0x00000002,
	SlowdownOthers = 0x00010003,

	EffectOthersFlag = 0x00010000,
	None             = 0x00000000,
}

[Serializable]
public struct PowerUp
{
	[Header("SETTINGS FOR ALL")]
	public PowerUpType type;
	public float       duration;
	public GameObject  prefab;

	[Header("Pickup Feedback")]
	public AudioClip      pickupSound;
	public ParticleSystem pickupParticle;

	[Header("During power feedback")]
	public AudioClip      characterLoopSound;
	public ParticleSystem characterParticle;

	public AudioClip sharedLoopSound;

	[Header("FOR POWERUPS THAT CHANGE A PROPERTY LIKE SPEED")]
	public float modifier;
}

[CreateAssetMenu(fileName = "PowerUpModel", menuName = "Data Models/PowerUp Model", order = 2)]
public class PowerUpModel : ScriptableObject
{
	public PowerUp[] powerUps;

	public PowerUp GetPowerUpFromType(PowerUpType type)
	{
		for(int i =0; i < powerUps.Length; i++)
		{
			if (powerUps[i].type == type)
				return powerUps[i];
		}

		Debug.LogErrorFormat("No powerUp of type {0} exist in this PowerUp Model", type.ToString());
		return new PowerUp();
	}

	public bool EffectOthersOnly(PowerUpType type)
	{
		return (type & PowerUpType.EffectOthersFlag) == PowerUpType.EffectOthersFlag;
	}
}
