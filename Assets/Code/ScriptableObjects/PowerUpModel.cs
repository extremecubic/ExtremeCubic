﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PowerUpType
{
	InfiniteDash,
	SuperSpeed,

	None,
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
	public AudioClip      loopSound;
	public ParticleSystem characterParticle;

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
}
