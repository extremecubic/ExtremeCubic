using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStateComponent : MonoBehaviour
{
    public CharacterState currentState { get; private set; } = CharacterState.Idle;

    public void ManualAwake()
    {		
    }

	public void SetState(CharacterState state)
	{
		currentState = state;
	}
}

public enum CharacterState
{
    Idle,
    Walking,
    Charging,
    Dashing,
	Frozen,
	
    Dead,
}