using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// EVERYTHING HERE IS CALLED LOCALLY ON ALL CLIENTS
// RPC HAVE ALREADY BEEN SENT FROM MASTERCLIENT BEFORE WE END UP HERE
// SO DONT CALL ANY RPC´S FROM HERE TO AVOID DUPLICATE CALLS
public class CharacterSpecialTileHandler : MonoBehaviour
{
	Character _character;

	public void ManualAwake(Character character)
	{
		_character = character;
	}

	public void OnEnterSpecialTile(Tile tile, Vector2DInt targetTileCoords)
	{
		if      (tile.model.data.specialType == SpecialTile.PowerDash) { ForceDash(tile); }
		else if (tile.model.data.specialType == SpecialTile.Teleport)  { Teleport(tile, targetTileCoords); }
	}

	void ForceDash(Tile tile)
	{
		tile.PlaySound(TileSounds.Special);

		Vector3 right = tile.view.transform.right;
		Vector2DInt direction = new Vector2DInt((int)right.x, (int)right.z);
		_character.movementComponent.NetworkOnGettingDashed(tile.position.x, tile.position.y, direction.x, direction.y, tile.model.data.intValue);
	}

	void Teleport(Tile tile, Vector2DInt targetTileCoords)
	{
		if (targetTileCoords == Constants.NOT_FOUND_SPECIALTILE)
		{
			// do fail feedback here that teleport could not be done
			// the target tile is probably occupied
			Debug.Log("Failed to preform teleport due to no tyarget tile found");
			return;
		}

		// do Teleport here
	}
	
}
