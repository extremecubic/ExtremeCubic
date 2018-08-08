using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterParticlesComponent : MonoBehaviour
{
	CharacterDatabase.ViewData _data;

	ParticleSystem _trail;
	ParticleSystem _hit;
	ParticleSystem _charge;

	Vector3 _dashForward;

	public void ManualAwake(CharacterDatabase.ViewData data, Transform parent)
	{
		_data = data;
		CreateTrail(parent);
		CreateCharge(parent);
	}

	void CreateTrail(Transform parent)
	{
		if (_data.trailParticle == null)
			return;

		_trail = Instantiate(_data.trailParticle, transform.position, _data.trailParticle.transform.rotation, parent);
		_trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	void CreateCharge(Transform parent)
	{
		_charge = Instantiate(_data.chargeupParticle, transform.position, _data.chargeupParticle.transform.rotation, parent);
		_charge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	public void EmitTrail(bool emit, Vector3 dashForward)
	{
		if (_trail == null)
			return;

		if (emit)
		{
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
		if (_data.chargeupParticle == null)
			return;		

		if (emit)		
			_charge.Play(true);	
		else				
			_charge.Stop(true, ParticleSystemStopBehavior.StopEmitting);					
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
	}
}
