using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CharacterDatabase : MonoBehaviour
{
    static CharacterDatabase _instance;
    public static CharacterDatabase instance => _instance ?? (_instance = FindObjectOfType<CharacterDatabase>()); 

	[Header("GAMEPLAY MODELS")]
    [SerializeField] CharacterModel _standardModel; public CharacterModel standardModel => _standardModel;

	[Header("VISUAL REPRESENTATION MODELS")]
	[SerializeField] ViewData[] _characterModelViews;

	[Space(5), Header("model to use if fails to find correct one")]
	[SerializeField] ViewData _fallBackView;

	Dictionary<string, ViewData> _characterViews = new Dictionary<string, ViewData>();

	[Serializable]
	public class ViewData
	{
		public string name;
		public GameObject[] prefabs;

		[Header("SOUNDS"), Space(2)]
		public AudioClip walkSound;
		public AudioClip dashSound;
		public AudioClip hitSound;
		public AudioClip deathSound;
		public AudioClip chargeSound;

		[Header("PARTICLES"), Space(2)]
		public ParticleSystem hitParticle;
		public ParticleSystem trailParticle;
		[Tooltip("will always set the forward of trails transform to the direction of the player dash")]
		public bool trailForwardAsDashDirection;
		public ParticleSystem chargeupParticle;

	}

	void Awake()
	{
		for (int i =0; i < _characterModelViews.Length; i++)		
			_characterViews.Add(_characterModelViews[i].name, _characterModelViews[i]);		
	}

	void OnDestroy()
	{
		_instance = null;
	}

	public ViewData GetViewFromName(string name)
	{
		if (_characterViews.ContainsKey(name))
			return _characterViews[name];
		else
			return _fallBackView;
		
	}

	public ViewData GetFirstView()
	{
		return _characterModelViews[0];
	}
}
