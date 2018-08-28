using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalRotation : MonoBehaviour
{
	[Header("ROTATION")]
	[SerializeField] bool  _rotate = false;
    [SerializeField] float _rotationSpeed = 90;

	[Header("UV SCROLL")]
	[SerializeField] bool    _uvScroll = false;
	[SerializeField] Vector2 _scrollAmount;

	Material _material;
    
    void Start()
    {
		if (_rotate)
			transform.Rotate(0, Random.Range(0,360), 0);

		_material = GetComponent<Renderer>().material;
    }

	void Update ()
    {
		if (_rotate)
			transform.Rotate(Vector3.up * (_rotationSpeed * Time.deltaTime));

		if (_uvScroll)		
			_material.mainTextureOffset += _scrollAmount * Time.deltaTime;		
    }
}
