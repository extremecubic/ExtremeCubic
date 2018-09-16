using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CameraController : MonoBehaviour
{
	Vector3 _eulerRotation;
	Vector3 _position;
	Vector3 _shakeOffset = Vector3.zero;

	[SerializeField] float _freeFlightRotationSpeed = 100.0f;
	[SerializeField] float _freeFlightMovementSpeed = 15.0f;
	[SerializeField] bool  _frozen = true;

	[SerializeField] CharacterModel _model;

	void Awake()
	{
		_eulerRotation = transform.rotation.eulerAngles;
		_position      = transform.position;
	}

	void Update()
	{
#if DEBUG_TOOLS

		if (Input.GetKeyDown(KeyCode.F))
			_frozen = !_frozen;

		if (Input.GetKeyDown(KeyCode.X))
			DoShake(_model.collideCameraShakeDuration, _model.collideCameraShakeSpeed, _model.collideCameraShakeIntensity, _model.collideCameraShakeIntensityDamping);

		if (!_frozen)
			UpdateTransformFreeFlight();
#endif

		transform.position = _position + _shakeOffset;
	}

	void UpdateTransformFreeFlight()
	{
		_eulerRotation.x -= Input.GetAxisRaw("Mouse Y") * _freeFlightRotationSpeed * Time.deltaTime;
		_eulerRotation.y += Input.GetAxisRaw("Mouse X") * _freeFlightRotationSpeed * Time.deltaTime;

		transform.rotation = Quaternion.Euler(_eulerRotation);

		_position += (transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical")) * _freeFlightMovementSpeed * Time.deltaTime;
	}

	public void DoShake(float duration, float shakeSpeed, float intensity, float intensityDamping)
	{
		Timing.RunCoroutineSingleton(_shake( duration, shakeSpeed, intensity, intensityDamping), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	IEnumerator<float> _shake(float duration, float shakeSpeed, float intensity, float intensityDamping)
	{		
		Vector3 target = Vector3.zero;
		Vector3 from   = Vector3.zero;

		float fraction = 0;
		float timeElapsed = 0.0f;

		// devides the intensity radius with how many shakes there will be in total and multiplies with damping
		// ex an damping of 1 will result in a linear damping from start value to 0 during the entire shake
		// 0.5 will result in half of the start value by the end of entire shake
		float dampingPerShake = (intensity / (duration * shakeSpeed)) * intensityDamping;

		while (timeElapsed < duration)
		{
			target   = Random.insideUnitSphere * intensity;
			from     = _shakeOffset;
			fraction = 0;

			// clamp to a minimum intensity of 0.1f
			intensity = Mathf.Clamp(intensity -= dampingPerShake, 0.1f, float.PositiveInfinity);

			while (fraction < 1)
			{
				timeElapsed += Time.deltaTime;
				fraction += shakeSpeed * Time.deltaTime;

				_shakeOffset = Vector3.Lerp(from, target, fraction);

				yield return Timing.WaitForOneFrame;
			}
		}

		// always end with interpolating back to 0,0,0
		from     = _shakeOffset;
		target   = Vector3.zero;
		fraction = 0;
		while(fraction < 1)
		{
			fraction += shakeSpeed * Time.deltaTime;
			_shakeOffset = Vector3.Lerp(from, target, fraction);
			yield return Timing.WaitForOneFrame;
		}
	
	}
}
