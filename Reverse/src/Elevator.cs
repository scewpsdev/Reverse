using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Elevator : Entity
{
	Entity door1, door2;

	protected bool open = false;
	protected float openTime = -1;
	protected float openDuration = 2;


	public override void init()
	{
		base.init();

		door1 = getChild(1);
		door2 = getChild(2);

		Debug.Assert(body == null);
		body = new RigidBody(this, RigidBodyType.Static, 0, PhysicsFilter.Player);
		body.addSphereTrigger(1.0f, Vector3.Up * 1.0f);
	}

	public override void update()
	{
		base.update();

		float rotation = open ? Easing.easeOutBounce(openProgress) * 30 : (1 - Easing.easeInOutSin(openProgress)) * 30;

		door1.rotation = Quaternion.AxisAngle(Vector3.Up, -rotation * Mathf.Deg2Rad);
		door2.rotation = Quaternion.AxisAngle(Vector3.Up, rotation * Mathf.Deg2Rad);
	}

	public void setOpen(bool open)
	{
		this.open = open;
		openTime = Time.gameTime;
	}

	public float openProgress => openTime != -1 ? MathF.Min((Time.gameTime - openTime) / openDuration, 1) : 1;
}

public class StartElevator : Elevator
{
	bool started = false;


	public override void init()
	{
		base.init();

		setOpen(true);
	}

	public override void onContact(RigidBody other, CharacterController otherController, int shapeID, int otherShapeID, bool isTrigger, bool otherTrigger, ContactType contactType)
	{
		Debug.Assert(isTrigger);
		if (contactType == ContactType.Lost && other.entity == Map.current.player && !started)
		{
			Map.current.timeline.startCycle();
			started = true;
		}
	}
}

public class GoalElevator : Elevator
{
	public override void init()
	{
		base.init();

		setOpen(true);
	}

	public override void update()
	{
		base.update();

		if (!open && openProgress == 1)
		{
			GameState.instance.loadMap(Map.current.id + 1);
		}
	}

	public override void onContact(RigidBody other, CharacterController otherController, int shapeID, int otherShapeID, bool isTrigger, bool otherTrigger, ContactType contactType)
	{
		Debug.Assert(isTrigger);
		if (contactType == ContactType.Found && other.entity == Map.current.player)
		{
			setOpen(false);
		}
	}
}
