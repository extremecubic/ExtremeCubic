using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

public class CharacterPowerUpComponent : MonoBehaviour
{
	[SerializeField] PowerUpModel _powerUps;

	Character _character;

	CoroutineHandle _handle;
	PowerUpType _currentPowerUp = PowerUpType.None;

	public int   extraDashCharges  { get; private set; } = 0;
	public float speedMultiplier   { get; private set; } = 1.0f;

	void Awake()
	{
		_character = GetComponent<Character>();
	}

	public void AddPower(PowerUpType type, Vector3 pickupPos)
	{
		// abort old powerup if one is running
		if (_currentPowerUp != PowerUpType.None)
			AbortPowerUp();

		// set wich powerup is active
		_currentPowerUp = type;

		// spawn feedback for powerup
		SpawnPickupFeedback(pickupPos);
		SpawnPowerFeedback();

		PowerUp powerUp = _powerUps.GetPowerUpFromType(type);

		// run CoRoutine that sets a effect from powerUp and resets it after time runs out
		if (type == PowerUpType.InfiniteDash)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => extraDashCharges = 1000, powerUp.duration));
		else if (type == PowerUpType.SuperSpeed)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => speedMultiplier = powerUp.modifier, powerUp.duration));
	}
	
	public void AbortPowerUp()
	{
		if (_currentPowerUp == PowerUpType.None)
			return;

		// kill ongoing coroutine
		if(_handle.IsRunning)
		   Timing.KillCoroutines(_handle);

		// stop feedback and reset all values to default
		AbortPowerFeedBack();
		ResetAll();

		// set current to none
		_currentPowerUp = PowerUpType.None;
	}

	void SpawnPickupFeedback(Vector3 pickupPos)
	{
		PowerUp powerUp = _powerUps.GetPowerUpFromType(_currentPowerUp);

		if (powerUp.pickupSound != null)
		{
			// spawn object with audiosource
			GameObject soundHolder = new GameObject("soundOneUsePowerPickup", typeof(AudioSource));
			AudioSource audio = soundHolder.GetComponent<AudioSource>();

			// asign clip and play
			audio.clip = powerUp.pickupSound;
			audio.Play();

			// delete after delay
			Destroy(soundHolder, 5);
		}

		// spawn a pickup particle
		if (powerUp.pickupParticle != null)
		{
			ParticleSystem system = Instantiate(powerUp.pickupParticle, pickupPos, powerUp.pickupParticle.transform.rotation);
			Destroy(system.gameObject, 8);
		}
	}

	void SpawnPowerFeedback()
	{
		PowerUp powerUp = _powerUps.GetPowerUpFromType(_currentPowerUp);

		// start sound to play during powerup
		if (powerUp.loopSound != null)
		{
			_character.soundComponent.SetClipToSound(CharacterSoundComponent.CharacterSound.PowerupLoop, powerUp.loopSound);
			_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.PowerupLoop);
		}

		// start particle on player during powerup
		if (powerUp.characterParticle)
			_character.ParticleComponent.StartPowerUpParticle(powerUp.characterParticle, true);
	}

	void AbortPowerFeedBack()
	{
		// stop sound and particle that was used during powerup
		_character.soundComponent.StopSound(CharacterSoundComponent.CharacterSound.PowerupLoop);
		_character.ParticleComponent.StartPowerUpParticle(null, false);
	}

	void ResetAll()
	{
		// reset all power values to 0
		extraDashCharges = 0;
		speedMultiplier = 1.0f;
	}

	IEnumerator<float> _RunPowerUp(Action onStart, float duration)
	{
		onStart();
		yield return Timing.WaitForSeconds(duration);
		AbortPowerUp();
	}
}
