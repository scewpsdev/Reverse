using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


public class Door : Entity
{
	static new Material material;

	static Door()
	{
		material = Material.CreateDeferred(Mathf.VectorToARGB(new Vector4(0.2f, 0.2f, 0.2f, 1.0f)));
	}


	Matrix closedState, openState;

	List<Tuple<float, bool>> activateTimes = new List<Tuple<float, bool>>();

	public bool defaultState;
	public bool inverted = false;
	public float activateDuration = 1;
	public bool activateOnTouch = false;

	public bool allowMovement = true;
	public bool allowRotation = true;


	public Door(bool open)
	{
		base.material = material;
		defaultState = open;
	}

	public override void init()
	{
		base.init();

		closedState = getModelMatrix();
		openState = getChild(0).getModelMatrix();
	}

	public override void update()
	{
		base.update();

		calculateCurrentState(out _, out float activationProgress);

		if (allowMovement)
			position = Vector3.Lerp(closedState.translation, openState.translation, Easing.easeInOutSin(activationProgress));
		if (allowRotation)
			rotation = Quaternion.Slerp(closedState.rotation, openState.rotation, Easing.easeInOutSin(activationProgress));
	}

	public void activate()
	{
		calculateCurrentState(out bool currentState, out _);
		activateTimes.Add(new Tuple<float, bool>(Map.current.timeline.time, !currentState));
		activateTimes.Sort((Tuple<float, bool> a, Tuple<float, bool> b) => a.Item1 < b.Item1 ? -1 : a.Item1 > b.Item1 ? 1 : 0);
	}

	void calculateCurrentState(out bool currentState, out float activationProgress)
	{
		if (inverted)
		{
			bool state = defaultState;
			float progress = defaultState ? 1 : 0;
			for (int i = activateTimes.Count - 1; i >= 0; i--)
			{
				bool last = i == 0 || Map.current.timeline.time > activateTimes[i - 1].Item1;

				state = activateTimes[i].Item2;
				float time0 = activateTimes[i].Item1;
				float time1 = last ? Map.current.timeline.time : activateTimes[i - 1].Item1;
				float timeSinceLastInteraction = time0 - time1;
				float progressSinceLastInteraction = timeSinceLastInteraction / activateDuration;
				int direction = state ? 1 : -1;
				progress = Mathf.Clamp(progress + progressSinceLastInteraction * direction, 0, 1);

				if (last)
					break;
			}
			currentState = state;
			activationProgress = progress;
		}
		else
		{
			bool state = defaultState;
			float progress = defaultState ? 1 : 0;
			for (int i = 0; i < activateTimes.Count; i++)
			{
				bool last = i == activateTimes.Count - 1 || Map.current.timeline.time < activateTimes[i + 1].Item1;

				state = activateTimes[i].Item2;
				float time0 = activateTimes[i].Item1;
				float time1 = last ? Map.current.timeline.time : activateTimes[i + 1].Item1;
				float timeSinceLastInteraction = time1 - time0;
				float progressSinceLastInteraction = timeSinceLastInteraction / activateDuration;
				int direction = state ? 1 : -1;
				progress = Mathf.Clamp(progress + progressSinceLastInteraction * direction, 0, 1);

				if (last)
					break;
			}
			currentState = state;
			activationProgress = progress;
		}
	}

	public override void onContact(RigidBody other, CharacterController otherController, int shapeID, int otherShapeID, bool isTrigger, bool otherTrigger, ContactType contactType)
	{
		if (other.entity == Map.current.player && contactType == ContactType.Found)
		{
			calculateCurrentState(out _, out float activationProgress);
			if (activationProgress == 0 && activateOnTouch)
			{
				activate();
			}
		}
	}
}
