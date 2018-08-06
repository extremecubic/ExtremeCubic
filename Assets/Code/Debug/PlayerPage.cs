using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if DEBUG_TOOLS
public class PlayerPage : MonoBehaviour
{
	[SerializeField] GUISkin _skin;

    Character _targetCharacter;

    public void Initialize(Character inCharacter) =>
        _targetCharacter = inCharacter;

	void OnGUI()
	{
        if (_targetCharacter == null)
            return;

		GUI.skin = _skin;

		GUILayout.Window(99, new Rect(0, 0, 350, 200), DrawStats, "Debug");
	}

	void DrawStats(int id)
	{
        Tile currentTile = _targetCharacter.movementComponent.currentTile;
		if (currentTile == null)
			return;

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("PLAYER STATE : {0}", _targetCharacter.stateComponent.currentState.ToString().ToUpper()));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("DASH CHARGED TO : {0}", _targetCharacter.movementComponent.currentDashCharges));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("CURRENT POSITION : X{0} Y{1}", currentTile.position.x, currentTile.position.y));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("CURRENT TILETYPE : {0}", _targetCharacter.movementComponent.currentTile.model.typeName.ToUpper()));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("TILE HEALTH : {0}", currentTile.currentHealth));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("TILE UNBREAKABLE ? : {0}", currentTile.model.data.unbreakable.ToString().ToUpper()));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("TILE DEADLY ? : {0}", currentTile.model.data.deadly.ToString().ToUpper()));
		GUILayout.EndHorizontal();		

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("IS MASTER CLIENT ? : {0}", _targetCharacter.isMasterClient.ToString().ToUpper()));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("PING : {0}", PhotonNetwork.GetPing().ToString()));
		GUILayout.EndHorizontal();

		if (PhotonNetwork.isMasterClient)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Width(100);
			if (GUILayout.Button("Reset Round"))
				Match.instance.level.ResetRound();

			GUILayout.EndHorizontal();
		} 		
	}
}

#endif