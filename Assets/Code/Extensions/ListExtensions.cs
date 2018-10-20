using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
	public static T TakeRandom<T>(this List<T> list)
	{
		Debug.Assert(list.Count > 0, "Tried to take random item in empty list");

		int index = Random.Range(0, list.Count);

		T item = list[index];
		list.RemoveAt(index);

		return item;
	}
	
}
