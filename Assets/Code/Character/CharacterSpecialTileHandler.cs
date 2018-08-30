using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpecialTileHandler : MonoBehaviour
{

	Character _character;

	public void ManualAwake(Character character)
	{
		_character = character;
	}

	public void OnEnterSpecialTile(Tile tile)
	{
		if (tile.model.data.specialType == SpecialTile.PowerDash) { ForceDash(tile); }		
	}

	void ForceDash(Tile tile)
	{
		tile.PlaySound(TileSounds.Special);

		Vector3 right = tile.view.transform.right;
		Vector2DInt direction = new Vector2DInt((int)right.x, (int)right.z);
		_character.movementComponent.NetworkOnGettingDashed(tile.position.x, tile.position.y, direction.x, direction.y, tile.model.data.intValue);
	}
	
}
