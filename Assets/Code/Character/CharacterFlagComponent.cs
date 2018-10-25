using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CharacterFlagComponent : MonoBehaviour
{
  
	// holds all possible character flags with a bool value
	// everyflag also have a handle for coroutines for setting removing
	// flags on a timer
    Dictionary<CharacterFlag, bool> _flags = new Dictionary<CharacterFlag, bool>(); 
    Dictionary<CharacterFlag, CoroutineHandle> _durationHandles = new Dictionary<CharacterFlag, CoroutineHandle>();

	// add all states to dictionarys
    public void ManualAwake()
    {      
        foreach (CharacterFlag stateValue in System.Enum.GetValues(typeof(CharacterFlag)))
        {
            _flags.Add(stateValue, false);
            _durationHandles.Add(stateValue, new CoroutineHandle());
        }
    }

	public bool GetFlag(CharacterFlag flag)
	{
		return _flags[flag];
	}

	// stops any ongoing timer of this flag
	// and sets it directly
    public void SetFlag(CharacterFlag flag, bool value)
    {
        Timing.KillCoroutines(_durationHandles[flag]);
        _flags[flag] = value;
    }

	// set a flag for a duration of time and then
	// set it back to initail state
    public void SetFlag(CharacterFlag flag, bool value, float duration, SingletonBehavior collisionBehaviour)
    {
        if (duration <= 0)
            throw new System.Exception("SetFlag parameter 'inDuration' cannot be zero or lower.");

        _durationHandles[flag] = Timing.RunCoroutineSingleton(_FlagDuration(flag, value, duration), _durationHandles[flag], collisionBehaviour);
    }

    IEnumerator<float> _FlagDuration(CharacterFlag flag, bool initialState, float duration)
    {
        _flags[flag] = initialState;
        yield return Timing.WaitForSeconds(duration);
        _flags[flag] = !initialState;
    }
}

public enum CharacterFlag
{
    Cooldown_Dash,
    Cooldown_Walk,
}
