using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Turnstile : Entity
{
	Entity turnRight, turnLeft;
	bool active = false;
	bool flipside = false;

	bool enterFlipside;


	public override void init()
	{
		base.init();

		turnRight = getChild(0);
		turnLeft = getChild(1);

		body = new RigidBody(this, RigidBodyType.Static, 0, PhysicsFilter.Player);
		body.addBoxTrigger(new Vector3(4, 1.5f, 2.5f), new Vector3(0, 1.5f, 0.5f), Quaternion.Identity);

		setFlipside(true);
	}

	void setFlipside(bool flipside)
	{
		if (this.flipside != flipside)
		{
			turnRight.scale = flipside ? Vector3.One : Vector3.Zero;
			turnRight.body.setActive(flipside);
			turnLeft.scale = flipside ? Vector3.Zero : Vector3.One;
			turnLeft.body.setActive(!flipside);
			this.flipside = flipside;
		}
	}

	public override void update()
	{
		base.update();

		if (active)
		{
			float border = 1.3f;
			Vector3 playerLocalPosition = getModelMatrix().inverted * Map.current.player.position;
			if (playerLocalPosition.x > border || playerLocalPosition.x < -border)
			{
				setFlipside(!flipside);
				playerLocalPosition.x -= 2 * border * MathF.Sign(playerLocalPosition.x);
				Map.current.player.controller.controller.setPosition(getModelMatrix() * playerLocalPosition);
			}
		}
	}

	public override void onContact(RigidBody other, CharacterController otherController, int shapeID, int otherShapeID, bool isTrigger, bool otherTrigger, ContactType contactType)
	{
		Debug.Assert(isTrigger);
		if (other.entity == Map.current.player)
		{
			if (contactType == ContactType.Found)
			{
				Map.current.timeline.setPaused(true);
				active = true;
				enterFlipside = flipside;
			}
			else if (contactType == ContactType.Lost)
			{
				Map.current.timeline.setPaused(false);
				active = false;
				if (enterFlipside != flipside)
				{
					Map.current.timeline.startCycle();
				}
			}
		}
	}
}
