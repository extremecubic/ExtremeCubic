using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CharacterFlagComponent : MonoBehaviour
{
  
    Dictionary<CharacterFlag, bool> _flags = new Dictionary<CharacterFlag, bool>(); // Can be replaced with bit enum when I am not lazy
    Dictionary<CharacterFlag, CoroutineHandle> _durationHandles = new Dictionary<CharacterFlag, CoroutineHandle>();

    public void ManualAwake()
    {      
        foreach (CharacterFlag stateValue in System.Enum.GetValues(typeof(CharacterFlag)))
        {
            _flags.Add(stateValue, false);
            _durationHandles.Add(stateValue, new CoroutineHandle());
        }
    }
    
    public bool GetFlag(CharacterFlag flag) => _flags[flag];
    public void SetFlag(CharacterFlag flag, bool value)
    {
        // Sets the flag directly
        Timing.KillCoroutines(_durationHandles[flag]);

        _flags[flag] = value;
    }

    public void SetFlag(CharacterFlag flag, bool value, float duration, SingletonBehavior collisionBehaviour)
    {
        // Sets the flag and inverts it after 'inDuration'
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
