using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public enum DeathType
{
	Sink,
	Quicksand,
	Mine,
	FlyToTarget,
}

// EVERYTHING HERE IS CALLED LOCALLY ON ALL CLIENTS
// RPC HAVE ALREADY BEEN SENT FROM MASTERCLIENT BEFORE WE END UP HERE
// SO DONT CALL ANY RPC´S FROM HERE TO AVOID DUPLICATE CALLS
// OR ONLY CALL RPC IF IS MASTER CLIENT
public class CharacterDeathComponent : Photon.MonoBehaviour
{
	Character       _character;
	CoroutineHandle _respawnHandle;

	void Awake()
	{
		_character = GetComponent<Character>();	
	}

	public void KillPlayer(Tile deathTile, double delta)
	{
		DeathType type = deathTile.model.data.deathType;

		// spawn level specific feedback from edge and empty tiles
		if (deathTile.model.typeName == "empty" || deathTile.model.typeName == Constants.EDGE_TYPE)
		{
			Level lvl = Level.instance;
			type = lvl.deathType;
			if (lvl.emptyDeathParticle != null)
			{
				ParticleSystem particle = Instantiate(lvl.emptyDeathParticle, transform.position, lvl.emptyDeathParticle.transform.rotation);
				Destroy(particle, 10);
			}

			if (lvl.emptyDeathsound != null)
				MusicManager.instance.SpawnAndPlaySound(lvl.emptyDeathsound, 5);
		}

		if (type == DeathType.Sink)
		   Timing.RunCoroutineSingleton(_sink(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
		else if(type == DeathType.Quicksand)
			Timing.RunCoroutineSingleton(_quicksand(deathTile), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
		else if (type == DeathType.Mine)
			Timing.RunCoroutineSingleton(_Explode(deathTile), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
		else if (type == DeathType.FlyToTarget)
			Timing.RunCoroutineSingleton(_FlyToTarget(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);

		// replace old tile with a new one if flaged from editor
		if (deathTile.model.data.replaceTileOnDeath)
		{
			TileMap TM = Level.instance.tileMap;
			TM.SetTile(deathTile.position, new Tile(deathTile.position, deathTile.model.data.replacementTile, 0, 1, TM.tilesFolder), 0.0f);
		}

		// start respawn countdown on all clients in case of server migration
		// only the master client will then send the rpc that does the actual respawn
		if (Match.instance.currentGameModeType == GameMode.TurfWar)
			_respawnHandle = Timing.RunCoroutine(_RespawnCounter(delta));
	}

	public IEnumerator<float> _sink()
	{
		float accelearation = 1.0f;

		while (_character.stateComponent.currentState == CharacterState.Dead)
		{
			accelearation += _character.model.fallAcceleration * Time.deltaTime;
			transform.position += Vector3.down * _character.model.fallSpeed * accelearation * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}
	}

	public IEnumerator<float> _quicksand(Tile deathTile)
	{
		CharacterModel model = _character.model;		
		Vector3 rotation = transform.rotation.eulerAngles;

		deathTile.PlaySound(TileSounds.Kill);

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

	public IEnumerator<float> _Explode(Tile deathTile)
	{
		MusicManager.instance.SpawnAndPlaySound(deathTile.model.data.killSound, 5);
		Match.instance.gameCamera.DoShake(_character.model.collideCameraShakeDuration, _character.model.collideCameraShakeSpeed, _character.model.collideCameraShakeIntensity, _character.model.collideCameraShakeIntensityDamping);
		
		if (deathTile.model.data.killParticle)
		{
			GameObject particle = Instantiate(deathTile.model.data.killParticle, new Vector3(deathTile.position.x, 0, deathTile.position.y), deathTile.model.data.killParticle.transform.rotation);
			Destroy(particle, 8.0f);
		}

		Vector3 direction = new Vector3(Random.Range(-0.8f, 0.8f), 1.0f, Random.Range(-0.8f, 0.8f)).normalized;

		while (_character.stateComponent.currentState == CharacterState.Dead)
		{
			transform.position += direction * _character.model.speedExplode * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}
	}

	IEnumerator<float> _FlyToTarget()
	{
		float accelearation = 1.0f;
		Vector3 targetPosition = Match.instance.level.flyToTargetTransform.position;

		float timeTofall = 0.05f;
		while (timeTofall > 0)
		{
			timeTofall -= Time.deltaTime;
			transform.position += Vector3.down * 30 * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}

		Vector3 direction = (targetPosition - transform.position).normalized;

		while (_character.stateComponent.currentState == CharacterState.Dead)
		{
			accelearation += _character.model.flyAcceleration * Time.deltaTime;
			transform.position += direction * _character.model.flySpeed * accelearation * Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}
	}

	IEnumerator<float> _RespawnCounter(double delta)
	{
		double respawnTime = 5.0f;
		double timer = respawnTime;

		if (Constants.onlineGame)
			timer = respawnTime - (PhotonNetwork.time - delta);

		while (timer > 0.0f)
		{
			// abort if our state is not dead anymore
			// this will probably mean that the round ended
			// during this respawn
			if (_character.stateComponent.currentState != CharacterState.Dead)
				yield break;

			timer -= Time.deltaTime;
			yield return Timing.WaitForOneFrame;
		}

		RespawnCharacter();
	}

	void RespawnCharacter()
	{
		// only find respawn tile on master client and send the tile to respawn on 
		// to all other clients
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
		{
			Tile spawnTile = Match.instance.level.tileMap.GetRandomFreeSpawnTile();
			_character.photonView.RPC("ReSpawn", PhotonTargets.All, spawnTile.position.x, spawnTile.position.y);
		}

		if (!Constants.onlineGame)
		{
			Tile spawnTile = Match.instance.level.tileMap.GetRandomFreeSpawnTile();
			_character.ReSpawn(spawnTile.position.x, spawnTile.position.y);
		}
	}

}
