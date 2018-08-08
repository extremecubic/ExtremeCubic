using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

public class CharacterPowerUpComponent : MonoBehaviour
{
	[SerializeField] PowerUpModel _powerUps;

	CoroutineHandle _handle;
	PowerUpType _currentPowerUp = PowerUpType.None;

	public int   extraDashCharges  { get; private set; } = 0;
	public float speedMultiplier   { get; private set; } = 1.0f;

	public void AddPower(PowerUpType type)
	{
		if (_currentPowerUp != PowerUpType.None)
			AbortPowerUp();

		_currentPowerUp = type;

		if      (type == PowerUpType.InfiniteDash)  _handle = Timing.RunCoroutine(_RunPowerUp(StartInfiniteDash, StopInfiniteDash, _powerUps.GetPowerUpFromType(type).duration));
		else if (type == PowerUpType.SuperSpeed)    _handle = Timing.RunCoroutine(_RunPowerUp(StartSuperSpeed,   StopSuperSpeed,   _powerUps.GetPowerUpFromType(type).duration));
	}
	
	public void AbortPowerUp()
	{
		if (_currentPowerUp == PowerUpType.None)
			return;

		// kill ongoing coroutine
		Timing.KillCoroutines(_handle);

		// run the stop function of current powerup
		if      (_currentPowerUp == PowerUpType.InfiniteDash) StopInfiniteDash();
		else if (_currentPowerUp == PowerUpType.SuperSpeed)   StopSuperSpeed();

		// set current to none
		_currentPowerUp = PowerUpType.None;
	}

	IEnumerator<float> _RunPowerUp(Action onStart, Action onEnd, float duration)
	{
		onStart();
		yield return Timing.WaitForSeconds(duration);
		onEnd();
	}

	void StartInfiniteDash()
	{
		PowerUp powerUp = _powerUps.GetPowerUpFromType(PowerUpType.InfiniteDash);
		extraDashCharges = 1000;
	}

	void StopInfiniteDash()
	{
		extraDashCharges = 0;
	}

	void StartSuperSpeed()
	{
		PowerUp powerUp = _powerUps.GetPowerUpFromType(PowerUpType.SuperSpeed);
		speedMultiplier = powerUp.modifier;
	}

	void StopSuperSpeed()
	{
		speedMultiplier = 1.0f;
	}
	

}
