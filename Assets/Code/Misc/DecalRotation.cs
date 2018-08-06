using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalRotation : MonoBehaviour
{
    [SerializeField] float _rotationSpeed = 90;
    
    void Start()
    {
        transform.Rotate(0, Random.Range(0,360), 0);
    }

	void Update ()
    {
        transform.Rotate(Vector3.up * (_rotationSpeed * Time.deltaTime));
    }
}
