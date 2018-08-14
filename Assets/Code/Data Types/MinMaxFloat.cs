using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

[Serializable]
public struct MinMaxFloat
{
	public float min;
	public float max;

	public MinMaxFloat(float min, float max)
	{
		this.min = min;
		this.max = max;
	}

	public float RandomRange()
	{
		return Random.Range(min, max);
	}
	
}
