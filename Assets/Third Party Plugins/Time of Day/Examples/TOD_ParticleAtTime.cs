using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class TOD_ParticleAtTime : MonoBehaviour
{
	public AnimationCurve Emission = new AnimationCurve() {
		keys = new Keyframe[] { new Keyframe(0, 0), new Keyframe(12, 1), new Keyframe(24, 0) }
	};

	private ParticleSystem particleComponent;
	ParticleSystem.EmissionModule eModule;

	protected void Start()
	{
		particleComponent = GetComponent<ParticleSystem>();
		eModule = particleComponent.emission;
	}

	protected void Update()
	{
		eModule.rateOverTimeMultiplier = Emission.Evaluate(TOD_Sky.Instance.Cycle.Hour);
	}
}
