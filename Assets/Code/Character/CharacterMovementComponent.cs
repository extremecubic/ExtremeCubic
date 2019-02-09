using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using Photon;

public partial class CharacterMovementComponent : Photon.MonoBehaviour
{
	public Tile currentTile       { get; private set; }
	public int currentDashCharges { get; private set; }

	Character _character;
	CharacterModel _model;

	CharacterStateComponent _stateComponent;
	CharacterFlagComponent _flagComponent;

	public Vector2DInt _lastMoveDirection = Vector2DInt.Up;

	CollisionTracker _collisionTracker;

	// used to keep track of the rotation the player last was targeting when getting interupted by getting hit
	Quaternion _lastTargetRotation; 

	TileMap _tileMap;

	public void ManualAwake()
	{
		_tileMap = Match.instance.level.tileMap;

		_character = GetComponent<Character>();
		_model = _character.model;

		_stateComponent = _character.stateComponent;
		_flagComponent = _character.flagComponent;

		_collisionTracker = FindObjectOfType<CollisionTracker>();

		_lastTargetRotation = transform.rotation;
	}

	void OnDestroy()
	{
		Timing.KillCoroutines(gameObject.GetInstanceID());
	}

	public void SetSpawnTile(Vector2DInt inSpawnTile)
	{
		currentTile = _tileMap.GetTile(inSpawnTile);
		currentTile.SetCharacter(_character);
	}	

	public void ResetAll()
	{
		Timing.KillCoroutines(gameObject.GetInstanceID());
		_stateComponent.SetState(CharacterState.Idle);

		transform.rotation = Quaternion.Euler(Vector3.zero);
		_lastTargetRotation = transform.rotation;
	}

	public void StopMovementAndAddCooldowns()
	{
		// reset state and add cooldowns
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Dash, true, _model.dashCooldown, SingletonBehavior.Overwrite);
	}

	public void StopMovementAndAddWalkCooldown()
	{
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
	}

	void SetNewTileReferences(Vector2DInt tile)
	{
		// dont set new refernces if we are alredy set as the current occupying player in the target tile
		// the reason that we dont want to reset the same player is that RemovePlayer() will
		// set the current player as the last occupying player. We will then be 
		// last and current player at the same time witch is not correct
		Tile targetTile = _tileMap.GetTile(tile);
		if (targetTile.currentCharacter != null && targetTile.currentCharacter == _character)
			return;

		// remove old reference and set to new
		currentTile.RemovePlayer();
		currentTile = _tileMap.GetTile(tile);
		currentTile.SetCharacter(_character);
	}

	public void TeleportToTile(Vector2DInt targetTile)
	{
		// cancel ongoing movement(the master client on the other clients can have movement left if we have very low ping)
		Timing.KillCoroutines(gameObject.GetInstanceID());

		// same as above, correct rotation if the last movement wasent quite finished when we got told to teleport
		transform.rotation = _lastTargetRotation;

		// cancel possible ongoing feedback from ex dash
		_character.ParticleComponent.StopAll();

		// teleport player, change tilereferences and add cooldown
		// only do walk cooldown here so we can do a quick dash when teleport is done if we want to
		transform.position = new Vector3(targetTile.x, 1, targetTile.y);
		SetNewTileReferences(targetTile);
		StopMovementAndAddWalkCooldown();
	}

	IEnumerator<float> _Charge()
	{
		_stateComponent.SetState(CharacterState.Charging);

		// start sound and charge particles
		_character.ParticleComponent.EmitCharge(true);
		_character.soundComponent.PlaySound(CharacterSound.Charge);

		float chargeAmount = _model.dashMinCharge;

		// check if we are under the invert controlls powerup
		bool invert = _character.powerUpComponent.invertControlls;

		// in online play the controller id is always 0
		// if in local play get the id of this player so we know
		// from witch controller we will accept input
		int controllerID = 0;
		if (!Constants.onlineGame)
			controllerID = _character.playerPhotonID;

		while (Input.GetButton(Constants.BUTTON_CHARGE + controllerID.ToString()))
		{
			chargeAmount += (_model.dashChargeRate * Time.deltaTime);
			chargeAmount = Mathf.Clamp(chargeAmount, _model.dashMinCharge, _model.dashMaxCharge);

			// while charging direction can be changed
			// check if the direction should be inverted or not
			if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + controllerID.ToString()) > 0)
				_lastMoveDirection = invert == false ? Vector2DInt.Up : Vector2DInt.Down;
			if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + controllerID.ToString()) < 0)
				_lastMoveDirection = invert == false ? Vector2DInt.Down : Vector2DInt.Up;
			if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + controllerID.ToString()) < 0)
				_lastMoveDirection = invert == false ? Vector2DInt.Left : Vector2DInt.Right;
			if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + controllerID.ToString()) > 0)
				_lastMoveDirection = invert == false ? Vector2DInt.Right : Vector2DInt.Left;

			currentDashCharges = (int)chargeAmount + _character.powerUpComponent.extraDashCharges;

			yield return Timing.WaitForOneFrame;
		}

		Vector2DInt currentPos = currentTile.position;

		OnDash(currentPos.x, currentPos.y, _lastMoveDirection.x, _lastMoveDirection.y, currentDashCharges);
	}

	IEnumerator<float> _Walk(Vector2DInt direction)
	{
		_stateComponent.SetState(CharacterState.Walking);

		_character.soundComponent.PlaySound(CharacterSound.Walk);

		// will only break the tile from server and then
		// send rpc to all clients to do the same
		if (!currentTile.model.data.unbreakable)
			Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

		if (currentTile.model.data.changeColorTile)
			Match.instance.level.ChangeColorTile(currentTile.position.x, currentTile.position.y, direction.x, direction.y);

		// get references to tiles
		Tile targetTile = currentTile.GetRelativeTile(direction);

		// Calculate lerp positions
		// lerp from current position (will catch up if laging)
		Vector3 fromPosition   = new Vector3(transform.position.x, 1, transform.position.z);
		Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

		// Calculate lerp rotations
		// get the movement direction based on vector between starttile and endtile
		// flip x and z to get the correct rotation in worldspace
		Vector3 movementDirection = (targetPosition - new Vector3(currentTile.position.x, 1, currentTile.position.y)).normalized;
		Vector3 movementDirectionRight = new Vector3(movementDirection.z, movementDirection.y, -movementDirection.x);

		// do lerp from current rotation if desynced(will catch up)
		// target is calculated using the target rotation we had during last movement if we are lagging behind
		// this prevents crooked target rotations
		Quaternion fromRotation = transform.rotation;
		Quaternion targetRotation = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;

		// save our target rotation so we can use this as fromrotation if we would get interupted by dash and not have time to finish the lerp
		_lastTargetRotation = targetRotation;

		// Save last move direction if we would do dash and not give any direction during chargeup
		_lastMoveDirection = new Vector2DInt((int)movementDirection.x, (int)movementDirection.z);

		// Update tile player references 
		SetNewTileReferences(targetTile.position);

		// if we take a step on our own it means that if we die it was not becuase of another player
		_character.dashingPlayerID = Constants.INVALID_ID;

		// do the movement itself
		float movementProgress = 0;
		while (movementProgress < 1)
		{
			movementProgress += (_model.walkSpeed * _character.powerUpComponent.speedMultiplier) * Time.deltaTime;
			movementProgress = Mathf.Clamp01(movementProgress);

			transform.position = Vector3.Lerp(fromPosition, targetPosition, movementProgress);
			transform.position = new Vector3(transform.position.x, 1 + Mathf.Sin(movementProgress * (float)Math.PI), transform.position.z);

			transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, movementProgress);

			yield return Timing.WaitForOneFrame;
		}

		// check if we ended up on deadly tile
		// only server handle death detection
		if (OnDeadlyTile())
			yield break;

		// check if tile contains any power up and pick it up
		if (currentTile.ContainsPowerUp())
			OnClaimPowerUp();

		if (OnEnterSpecialTile())
			yield break;
			
		currentTile.OnPlayerLand();

		// reset state and add cooldown
		StopMovementAndAddWalkCooldown();
	}

	public IEnumerator<float> _Dash(Vector2DInt direction, int dashStrength, bool fromCollision = false)
	{
		_stateComponent.SetState(CharacterState.Dashing); // set state to dashing	

		// only play dash sound if this was a volentary dash
		if (!fromCollision)
		{
			// if it was a volentary dash it means that if we die it was not becuase of another player
			_character.dashingPlayerID = Constants.INVALID_ID;

			Vector2DInt currentPos = currentTile.position;
			Vector2DInt targetPos  = currentTile.GetRelativeTile(direction).position;

			_character.soundComponent.PlaySound(CharacterSound.Dash);
			_character.ParticleComponent.EmitTrail(true, (new Vector3(targetPos.x, 1, targetPos.y) - new Vector3(currentPos.x, 1, currentPos.y)).normalized);
		}

		// stop feedback from charge		
		_character.soundComponent.StopSound(CharacterSound.Charge);

		// loop over all dash charges
		for (int i = 0; i < dashStrength; i++)
		{
			// get next tile in dash path			
			// current tile is corrected before coroutine if lagging so this is safe
			Tile targetTile = currentTile.GetRelativeTile(direction);

			// abort dash if running into non walkable tile
			if (!targetTile.model.data.walkable)
			{
				OnHittingObstacle(direction);
				yield break;
			}

			// Calculate lerp positions
			Vector3 fromPosition   = transform.position; // interpolate from current position to avoid teleporting if lagging
			Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

			// use the position of current tile instead of position of player to calculate rotation, otherwise we can get crooked rotation if laging
			Vector3 currentTilePos = new Vector3(currentTile.position.x, 1, currentTile.position.y);

			// Calculate lerp rotations
			// note: use last target rotation as base if we was in middle of movement when this dash started from getting hit from other player
			// this will make the rotation that was left from last movement to be added to this rotation and will be caught up
			Vector3 movementDirection = (targetPosition - currentTilePos).normalized;
			Quaternion fromRotation = transform.rotation;
			Quaternion targetRotation = Quaternion.Euler(movementDirection * (90 * _model.dashRotationSpeed)) * _lastTargetRotation;

			// if we will hit someone we need the target rotation we had last time becuase we wont start moving towards the future last target
			Quaternion previousLastTargetRotation = _lastTargetRotation;

			_lastTargetRotation = targetRotation;

			// handle collision in online game
			if (Constants.onlineGame)
				if (IsCollidingOnline(previousLastTargetRotation, targetTile, direction, dashStrength, i))
					yield break;

			// handle collision in local game
			if (!Constants.onlineGame)
				if (IsCollidingLocal(previousLastTargetRotation, targetTile, direction, dashStrength, i))									
					yield break;				

			// hurt tile if it is destructible(will only detect break on master client)
			if (!currentTile.model.data.unbreakable)
				Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

			// change color of tile if it is colorable(will only detect this on master client)
			if (currentTile.model.data.changeColorTile)
				Match.instance.level.ChangeColorTile(currentTile.position.x, currentTile.position.y, direction.x, direction.y);

			// Update tile player references 
			SetNewTileReferences(targetTile.position);

			// only use potential speed multipliers from powerups if we initiated the dash ourselfs
			float speedMultiplier = fromCollision ? 1.0f : _character.powerUpComponent.speedMultiplier;

			// do the movement itself
			float movementProgress = 0;
			while (movementProgress < 1)
			{
				movementProgress += (_model.dashSpeed * speedMultiplier) * Time.deltaTime;
				movementProgress = Mathf.Clamp01(movementProgress);

				transform.position = Vector3.Lerp(fromPosition, targetPosition, movementProgress);
				transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, movementProgress);

				yield return Timing.WaitForOneFrame;
			}

			// check if we exited the map after every tile
			if (OnDeadlyEdge())
				yield break;			

			// check if tile contains any power up and pick it up
			if (currentTile.ContainsPowerUp())
				OnClaimPowerUp();

			if (OnEnterSpecialTile())
				yield break;
		}

		// check if we ended up on deadly tile
		// only server handle death detection
		if (OnDeadlyTile())
			yield break;	

		// add cooldowns and stop feedback
		_character.ParticleComponent.EmitTrail(false, Vector3.zero);
		StopMovementAndAddCooldowns();
	}

	public IEnumerator<float> _ObstacleCollide(Vector2DInt tile, Vector2DInt direction)
	{
		// get references to tiles
		Tile fromTile   = _tileMap.GetTile(tile);
		Tile targetTile = fromTile.GetRelativeTile(direction);

		Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

		// Calculate lerp rotations
		// get the movement direction based on vector between starttile and endtile
		// flip x and z to get the correct rotation in worldspace
		Vector3 movementDirection = (targetPosition - new Vector3(fromTile.position.x, 1, fromTile.position.y)).normalized;
		Vector3 movementDirectionRight = new Vector3(-movementDirection.z, movementDirection.y, movementDirection.x);

		// do lerp from current rotation if desynced(will catch up)
		// target is calculated using the target rotation we had during last movement if we are lagging behind
		// this prevents crooked target rotations
		Quaternion fromRotation   = transform.rotation;
		Quaternion targetRotation = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;

		int rollCount = 1;

		// save our target rotation so we can use this as fromrotation if we would get interupted by dash and not have time to finish the lerp
		_lastTargetRotation = targetRotation;

		// do the movement itself
		float movementProgress = 0;
		float rotationProgress = 0;
		while (movementProgress < 1)
		{
			// rotation progress is muliplied by how many 90 degree rotations we want to do during the movement
			rotationProgress += (_model.collideSpeed * _model.numCollideRolls) * Time.deltaTime;
			movementProgress += _model.collideSpeed * Time.deltaTime;
			movementProgress = Mathf.Clamp01(movementProgress);

			// make sure we set the last rotation progress to 1 if the movement is done
			if (movementProgress >= 1)
				rotationProgress = 1;

			// offset position in oposite direction of dash
			float yPos = 1 + (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideBounceHeight);
			float xPos = fromTile.position.x + ((-movementDirection.x) * (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideFlyBackAmount));
			float zPos = fromTile.position.y + ((-movementDirection.z) * (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideFlyBackAmount));

			transform.position = new Vector3(xPos, yPos, zPos);
			transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, rotationProgress);

			// reset rotation progress and add another 90 degress to target
			if (rotationProgress >= 1 && rollCount < _model.numCollideRolls)
			{
				fromRotation        = transform.rotation;
				targetRotation      = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;
				_lastTargetRotation = targetRotation;

				rotationProgress = 0;
				rollCount++;
			}

			yield return Timing.WaitForOneFrame;
		}

		if (OnDeadlyTile())
			yield break;

		_character.soundComponent.PlaySound(CharacterSound.StunnedSound, _model.collideStunTime - 0.3f);
		_character.ParticleComponent.EmitStunned(true);

		yield return Timing.WaitForSeconds(_model.collideStunTime);

		_character.ParticleComponent.EmitStunned(false);
		StopMovementAndAddCooldowns();
	}

	public IEnumerator<float> _Correct(Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float time)
	{
		float fraction = 0;
		float timer = 0;
		while (fraction < 1)
		{
			timer += Time.deltaTime;
			fraction = Mathf.InverseLerp(0, time, timer);
			transform.position = Vector3.Lerp(from, to, fraction);
			transform.rotation = Quaternion.Lerp(fromRot, toRot, fraction);
			yield return Timing.WaitForOneFrame;
		}
	}

#if DEBUG_TOOLS
	public void InfiniteDash()
	{
		Character[] c = FindObjectsOfType<Character>();

		foreach(Character p in c)
		{
			CharacterMovementComponent m = p.GetComponent<CharacterMovementComponent>();
			p.GetComponent<PhotonView>().RPC("NetworkDash", PhotonTargets.All, m.currentTile.position.x, m.currentTile.position.y, m._lastMoveDirection.x, m._lastMoveDirection.y, 100);
		}		
	}
#endif
	
}
