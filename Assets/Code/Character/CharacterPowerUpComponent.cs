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

	public int   extraDashCharges { get; private set; } = 0;
	public float speedMultiplier  { get; private set; } = 1.0f;
	public bool  invertControlls  { get; private set; } = false;

	void Awake()
	{
		_character = GetComponent<Character>();
	}

	void OnDestroy()
	{
		if (_handle.IsRunning)
			_handle.IsRunning = false;
	}

	public void RegisterPowerup(PowerUpType type, Vector3 pickupPos)
	{
		// spawn feedback for powerup
		SpawnPickupFeedback(pickupPos, type);

		// check if powerup should effect me or others
		if (_powerUps.EffectOthersOnly(type))
		{
			// get all other players and add the powerup to them(these are usually negative power effects)
			CharacterPowerUpComponent[] all = FindObjectsOfType<CharacterPowerUpComponent>();
			for (int i = 0; i < all.Length; i++)
				if (all[i] != this)
					all[i].AddPower(type);

			// start the powerUp sound loop
			// becuase this power effects multiple players we only start one sound 
			// for all to share instead of one sound per character
			PowerUp powerUp = _powerUps.GetPowerUpFromType(type);
			if (powerUp.sharedLoopSound != null)
				Match.instance.musicManager.PlaySharedPowerUpLoop(powerUp.sharedLoopSound, powerUp.duration);

			return;
		}

		// if not effect others only add the power to this player
		AddPower(type);
	}

	void AddPower(PowerUpType type)
	{
		// abort old powerup if one is running
		if (_currentPowerUp != PowerUpType.None)
			AbortPowerUp();

		// set wich powerup is active
		_currentPowerUp = type;

		// spawn feedback that stays active during entire power duration
		SpawnPowerFeedback();

		PowerUp powerUp = _powerUps.GetPowerUpFromType(type);

		// run Coroutine that sets a effect from powerUp and resets it after time runs out
		if (type == PowerUpType.InfiniteDash)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => extraDashCharges = 1000, powerUp.duration));
		else if (type == PowerUpType.SuperSpeed)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => speedMultiplier = powerUp.modifier, powerUp.duration));
		else if (type == PowerUpType.SlowdownOthers)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => speedMultiplier = powerUp.modifier, powerUp.duration));
		else if (type == PowerUpType.invertControllOthers)
			_handle = Timing.RunCoroutine(_RunPowerUp(() => invertControlls = true, powerUp.duration));
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

	void SpawnPickupFeedback(Vector3 pickupPos, PowerUpType type)
	{
		PowerUp powerUp = _powerUps.GetPowerUpFromType(type);

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
		// only used for powers that only effect the player that picks it up
		if (powerUp.characterLoopSound != null)
		{
			_character.soundComponent.SetClipToSound(CharacterSound.PowerupLoop, powerUp.characterLoopSound);
			_character.soundComponent.PlaySound(CharacterSound.PowerupLoop);
		}

		// start particle on player during powerup
		if (powerUp.characterParticle)
			_character.ParticleComponent.StartPowerUpParticle(powerUp.characterParticle, true);
	}

	void AbortPowerFeedBack()
	{
		// stop sound and particle that was used during powerup
		_character.soundComponent.StopSound(CharacterSound.PowerupLoop);
		_character.ParticleComponent.StartPowerUpParticle(null, false);
	}

	void ResetAll()
	{
		// reset all power values to 0
		extraDashCharges = 0;
		speedMultiplier = 1.0f;
		invertControlls = false;
	}

	IEnumerator<float> _RunPowerUp(Action onStart, float duration)
	{
		onStart();
		yield return Timing.WaitForSeconds(duration);
		AbortPowerUp();
	}
}
