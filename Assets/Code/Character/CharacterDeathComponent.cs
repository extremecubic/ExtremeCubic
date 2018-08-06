using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum DeathType
{
	Sink,
	Quicksand,
	Mine,
}

public class CharacterDeathComponent : MonoBehaviour
{
	Character _character;

	void Awake()
	{
		_character = GetComponent<Character>();	
	}

	public void KillPlayer(Tile deathTile)
	{
		DeathType type = deathTile.model.data.deathType;

		if (type == DeathType.Sink)
		   Timing.RunCoroutineSingleton(_sink(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
		else if(type == DeathType.Quicksand)
			Timing.RunCoroutineSingleton(_quicksand(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
		else if (type == DeathType.Mine)
			Timing.RunCoroutineSingleton(_Explode(deathTile), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	public IEnumerator<float> _sink()
	{
		while (_character.stateComponent.currentState == CharacterState.Dead)
		{
			transform.position += Vector3.down * _character.model.sinkSpeed * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}
	}

	public IEnumerator<float> _quicksand()
	{
		CharacterModel model = _character.model;		
		Vector3 rotation = transform.rotation.eulerAngles;

		float fraction = 0.0f;
		while (fraction < 1.0f)
		{
			fraction += Time.deltaTime / model.durationQvick;
			float moveSpeed     = Mathf.Lerp(model.startEndMoveSpeedQvick.x,     model.startEndMoveSpeedQvick.y,     fraction) * model.moveCurveQvick.Evaluate(fraction);
			float rotationSpeed = Mathf.Lerp(model.startEndRotationspeedQvick.x, model.startEndRotationspeedQvick.y, fraction) * model.rotationCurveQvick.Evaluate(fraction);

			rotation += Vector3.up      * rotationSpeed * Time.deltaTime;
			rotation += Vector3.right   * rotationSpeed * Time.deltaTime;
			transform.rotation = Quaternion.Euler(rotation);

			transform.position += Vector3.down * moveSpeed * Time.deltaTime;

			yield return Timing.WaitForOneFrame;
		}
	}

	public IEnumerator<float> _Explode(Tile tile)
	{
		if (tile.model.data.killParticle)
		{
			GameObject particle = Instantiate(tile.model.data.killParticle, new Vector3(tile.position.x, 0, tile.position.y), tile.model.data.killParticle.transform.rotation);
			Destroy(particle, 8.0f);
		}

		Vector3 direction = new Vector3(Random.Range(-0.8f, 0.8f), 1.0f, Random.Range(-0.8f, 0.8f)).normalized;

		while (_character.stateComponent.currentState == CharacterState.Dead)
		{
			transform.position += direction * _character.model.speedExplode * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}
	}

}
