using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalRotation : MonoBehaviour
{
	[Header("DECAL ROTATION")]
	[SerializeField] bool  _decalRotate = false;
    [SerializeField] float _decalRotationSpeed = 90;

	[Header("UV SCROLL")]
	[SerializeField] bool    _uvScroll = false;
	[SerializeField] Vector2 _scrollAmount;

	[Header("3D Rotation")]
	[SerializeField] bool _3DRotation = false;
	[SerializeField] bool _randomizeStartRotation = false;
	[SerializeField] Vector3 _rotationMin;
	[SerializeField] Vector3 _rotationMax;
	Vector3 _rotationSpeed;
	Vector3 _currentRotation;

	Material _material;
    
    void Start()
    {
		if (_decalRotate)
			transform.Rotate(0, Random.Range(0,360), 0);

		if (_3DRotation)
		{
			_rotationSpeed = new Vector3(Random.Range(_rotationMin.x, _rotationMax.x), Random.Range(_rotationMin.y, _rotationMax.y), Random.Range(_rotationMin.z, _rotationMax.z));
			if (_randomizeStartRotation)
				_currentRotation = new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
		}



		_material = GetComponent<Renderer>().material;
    }

	void Update ()
    {
		if (_decalRotate)
			transform.Rotate(Vector3.up * (_decalRotationSpeed * Time.deltaTime));

		if (_uvScroll)		
			_material.mainTextureOffset += _scrollAmount * Time.deltaTime;	
		
		if (_3DRotation)
		{
			_currentRotation += (_rotationSpeed * Time.deltaTime);
			transform.rotation = Quaternion.Euler(_currentRotation);
		}

    }
}
