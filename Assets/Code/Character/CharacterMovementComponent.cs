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
		_tileMap = Level.instance.tileMap;

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

	public void OnTryWalk(Vector2DInt direction)
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Walk))
			return;

		// cant walk to tile if occupied by other player or if not walkable tile
		Tile targetTile = currentTile.GetRelativeTile(direction);
		if (targetTile.IsOccupied() || !targetTile.model.data.walkable)
			return;

		if (Constants.onlineGame)
			photonView.RPC("NetworkWalk", PhotonTargets.All, currentTile.position.x, currentTile.position.y, targetTile.position.x, targetTile.position.y);

		if (!Constants.onlineGame)
			NetworkWalk(currentTile.position.x, currentTile.position.y, targetTile.position.x, targetTile.position.y);
	}

	public void OnTryCharge()
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Dash))
			return;

		// send to all other then me just for starting feedback, we start coroutine instead
		if (Constants.onlineGame)
			photonView.RPC("NetworkCharge", PhotonTargets.Others); 

		Timing.RunCoroutineSingleton(_Charge(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	void OnDash(int fromX, int fromY, int directionX, int directionY, int dashCharges)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkDash", PhotonTargets.All, fromX, fromY, directionX, directionY, dashCharges);

		if (!Constants.onlineGame)
			NetworkDash(fromX, fromY, directionX, directionY, dashCharges);
	} 

	public void OnGettingDashed(Vector2DInt startTile, Vector2DInt direction, int hitPower)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkOnGettingDashed", PhotonTargets.All, startTile.x, startTile.y, direction.x, direction.y, hitPower);

		if (!Constants.onlineGame)
			NetworkOnGettingDashed(startTile.x, startTile.y, direction.x, direction.y, hitPower);
	}

	public void OnDashingOther(Vector2DInt lastTile, Quaternion rot, Vector2DInt targetTile)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkOnDashingOther", PhotonTargets.All, lastTile.x, lastTile.y, targetTile.x, targetTile.y, rot.x, rot.y, rot.z, rot.w);

		if (!Constants.onlineGame)
			NetworkOnDashingOther(lastTile.x, lastTile.y, targetTile.x, targetTile.y, rot.x, rot.y, rot.z, rot.w);
	}

	void OnClaimPowerUp()
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkClaimPowerUp", PhotonTargets.All, currentTile.position.x, currentTile.position.y);

		if (!Constants.onlineGame)
			NetworkClaimPowerUp(currentTile.position.x, currentTile.position.y);
	}

	void OnHittingObstacle(Vector2DInt direction)
	{
		if (Constants.onlineGame)
		{
			if (PhotonNetwork.isMasterClient)
				photonView.RPC("NetworkOnhittingObstacle", PhotonTargets.All, currentTile.position.x, currentTile.position.y, direction.x, direction.y);
			else
				_character.stateComponent.SetState(CharacterState.Frozen);
		}

		if (!Constants.onlineGame)
			NetworkOnhittingObstacle(currentTile.position.x, currentTile.position.y, direction.x, direction.y);
	}

	bool OnEnterSpecialTile()
	{
		if (currentTile.model.data.isSpecialTile)
		{
			if (Constants.onlineGame)
			{
				if (PhotonNetwork.isMasterClient)
					photonView.RPC("NetworkClaimSpecialTile", PhotonTargets.All, currentTile.position.x, currentTile.position.y);
				else
					_stateComponent.SetState(CharacterState.Frozen);
			}

			if (!Constants.onlineGame)
				NetworkClaimSpecialTile(currentTile.position.x, currentTile.position.y);

			return true;
		}
		return false;
	}

	bool OnDeadlyTile()
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient && currentTile.model.data.deadly)
		{
			photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y);
			return true;
		}

		if (!Constants.onlineGame && currentTile.model.data.deadly)
		{
			Die(currentTile.position.x, currentTile.position.y);
			return true;
		}
		return false;
	}

	bool OnDeadlyEdge()
	{
		if (currentTile.model.typeName == Constants.EDGE_TYPE)
		{
			// stop movement and flag frozen locally and only handle death on server
			// will be corrected by server if say a collision happened on server but not locally that would have prevented us from exiting map
			if (Constants.onlineGame)
			{
				if (PhotonNetwork.isMasterClient)
					photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y);
				else
					_stateComponent.SetState(CharacterState.Frozen);
			}

			if (!Constants.onlineGame)
				Die(currentTile.position.x, currentTile.position.y);

			return true;
		}
		return false;
	}

	public void ResetAll()
	{
		Timing.KillCoroutines(gameObject.GetInstanceID());
		_stateComponent.SetState(CharacterState.Idle);

		transform.rotation = Quaternion.Euler(Vector3.zero);
		_lastTargetRotation = transform.rotation;
	}

	void StopMovementAndAddCooldowns()
	{
		// reset state and add cooldowns
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Dash, true, _model.dashCooldown, SingletonBehavior.Overwrite);
	}

	void SetNewTileReferences(Vector2DInt tile)
	{
		// remove old reference and set to new
		currentTile.RemovePlayer();
		currentTile = _tileMap.GetTile(tile);
		currentTile.SetCharacter(_character);
	}

	IEnumerator<float> _Charge()
	{
		_stateComponent.SetState(CharacterState.Charging);

		// start sound and charge particles
		_character.ParticleComponent.EmitCharge(true);
		_character.soundComponent.PlaySound(CharacterSound.Charge);

		float chargeAmount = _model.dashMinCharge;

		bool invert = _character.powerUpComponent.invertControlls;

		int controllerID = 0;
		if (!Constants.onlineGame)
			controllerID = _character.playerID;

		while (Input.GetButton(Constants.BUTTON_CHARGE + controllerID.ToString()))
		{
			chargeAmount += (_model.dashChargeRate * Time.deltaTime);
			chargeAmount = Mathf.Clamp(chargeAmount, _model.dashMinCharge, _model.dashMaxCharge);

			// while charging direction can be changed
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

	IEnumerator<float> _Walk(Vector2DInt fromTilePos, Vector2DInt toTilePos)
	{
		_stateComponent.SetState(CharacterState.Walking);

		_character.soundComponent.PlaySound(CharacterSound.Walk);

		// only handle tilebreaks on server
		if (!currentTile.model.data.unbreakable)
			Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

		// get references to tiles
		Tile fromTile   = _tileMap.GetTile(fromTilePos);
		Tile targetTile = _tileMap.GetTile(toTilePos);		

		// Calculate lerp positions
		// lerp from current position (will catch up if laging)
		Vector3 fromPosition   = new Vector3(transform.position.x, 1, transform.position.z);
		Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

		// Calculate lerp rotations
		// get the movement direction based on vector between starttile and endtile
		// flip x and z to get the correct rotation in worldspace
		Vector3 movementDirection = (targetPosition - new Vector3(fromTile.position.x, 1, fromTile.position.y)).normalized;
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

		// Update tile player references NOTE: this is done right when a player starts moving to avoid players being able to move to the same tile (lerping is used when getting hit when not physiclly att target tile)
		SetNewTileReferences(targetTile.position);

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
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
	}

	public IEnumerator<float> _Dash(Vector2DInt direction, int dashStrength, bool fromCollision = false)
	{
		_stateComponent.SetState(CharacterState.Dashing); // set state to dashing

		// only play dash sound if this was a volentary dash
		if (!fromCollision)
		{
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
				{
					OnDeadlyTile();
					yield break;
				}

			// hurt tile if it is destructible(will only detect break on master client)
			if (!currentTile.model.data.unbreakable)
				Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

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
		Tile fromTile = _tileMap.GetTile(tile);
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
		Quaternion fromRotation = transform.rotation;
		Quaternion targetRotation = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;

		int rollCount = 1;

		// save our target rotation so we can use this as fromrotation if we would get interupted by dash and not have time to finish the lerp
		_lastTargetRotation = targetRotation;

		// do the movement itself
		float movementProgress = 0;
		float rotationProgress = 0;
		while (movementProgress < 1)
		{
			rotationProgress += ((_model.collideSpeed * _character.powerUpComponent.speedMultiplier) * _model.numCollideRolls) * Time.deltaTime;
			movementProgress += (_model.collideSpeed * _character.powerUpComponent.speedMultiplier) * Time.deltaTime;
			movementProgress = Mathf.Clamp01(movementProgress);

			if (movementProgress >= 1)
				rotationProgress = 1;

			float yPos = 1 + (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideBounceHeight);
			float xPos = fromTile.position.x + ((-movementDirection.x) * (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideFlyBackAmount));
			float zPos = fromTile.position.y + ((-movementDirection.z) * (Mathf.Sin(movementProgress * (float)Math.PI) * _model.collideFlyBackAmount));

			transform.position = new Vector3(xPos, yPos, zPos);
			transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, rotationProgress);

			if (rotationProgress >= 1 && rollCount < _model.numCollideRolls)
			{
				fromRotation = transform.rotation;
				targetRotation = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;
				_lastTargetRotation = targetRotation;

				rotationProgress = 0;
				rollCount++;
			}

			yield return Timing.WaitForOneFrame;
		}

		yield return Timing.WaitForSeconds(_model.collideStunTime);

		StopMovementAndAddCooldowns();
	}
	
	public IEnumerator<float> _Correct(Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float time)
	{
		float fraction = 0;
		float timer = 0;
		while (fraction < 1)
		{
			timer += Time.deltaTime;
			fraction           = Mathf.InverseLerp(0, time, timer);
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
