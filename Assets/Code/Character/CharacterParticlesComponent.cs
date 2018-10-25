using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterParticlesComponent : MonoBehaviour
{
	CharacterDatabase.ViewData _data;

	ParticleSystem _trail;
	ParticleSystem _hit;
	ParticleSystem _charge;
	ParticleSystem _powerUpLoop;
	ParticleSystem _stunned;

	Vector3 _dashForward;

	public void ManualAwake(CharacterDatabase.ViewData data, Transform parent)
	{
		// create all character particles that will be reused
		_data = data;
		CreateParticle(parent, ref _trail,   _data.trailParticle);
		CreateParticle(parent, ref _charge,  _data.chargeupParticle);
		CreateParticle(parent, ref _stunned, _data.stunnedParticle);
	}
	
	// creates a particle and sets it to a parent
	void CreateParticle(Transform parent, ref ParticleSystem system, ParticleSystem systemPrefab)
	{
		if (systemPrefab == null)
			return;

		system = Instantiate(systemPrefab, transform.position, systemPrefab.transform.rotation, parent);
		system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	public void EmitTrail(bool emit, Vector3 dashForward)
	{
		if (_trail == null)
			return;

		if (emit)
		{
			// will set the rotation of the trail particle
			// so it does not rotate along with the character
			if (_data.trailForwardAsDashDirection)
			{
				_dashForward = dashForward;
				_trail.transform.forward = _dashForward;
			}
			_trail.Play(true);
		}
		else
			_trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}

	public void EmitCharge(bool emit)
	{
		if (_charge == null)
			return;		

		if (emit)		
			_charge.Play(true);	
		else				
			_charge.Stop(true, ParticleSystemStopBehavior.StopEmitting);					
	}

	public void EmitPowerUp(ParticleSystem system, bool emit)
	{
		if (!emit)
		{
			_powerUpLoop.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			return;
		}

		// will remove the old powerup particle if one is active
		if (_powerUpLoop)
		{
			_powerUpLoop.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(_powerUpLoop.gameObject, 5);
		}

		// create the new powerup loop
		_powerUpLoop = Instantiate(system, transform.position, system.transform.rotation, transform);			
	}

	public void EmitStunned(bool emit)
	{
		if (_stunned == null)
			return;

		if (emit)
		{
			// make sure that the particle is located at the top of
			// the player in world space and not local space
			_stunned.transform.position = transform.position + Vector3.up;
			_stunned.Play(true);
		}
		else
			_stunned.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}

	public void SpawnHitEffect(Vector2DInt a, Vector2DInt b)
	{
		if (_data.hitParticle == null)
			return;

		// spawn hit particle abit away from dahing player in the direction of player getting dashed
		Vector3 spawnPosition = new Vector3(a.x, 1, a.y) + ((new Vector3(b.x, 1, b.y) - new Vector3(a.x, 1, a.y)) * 2.0f);
		ParticleSystem p = Instantiate(_data.hitParticle, spawnPosition, _data.hitParticle.transform.rotation);
		Destroy(p, 8);
	}

	void LateUpdate()
	{
		// if we are emitting a trail and it should not be following the local rotation
		// we need to set the rotation to the forward of dashdirection every frame
		if (_trail.isEmitting && _data.trailForwardAsDashDirection)
		{
			_trail.transform.forward = _dashForward;
		}	
	}

	public void StopAll()
	{
		if (_charge != null)		
			_charge.Stop(true, ParticleSystemStopBehavior.StopEmitting);			
		
		if (_trail != null)		
			_trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);

		if (_stunned != null)
			_stunned.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}
}
