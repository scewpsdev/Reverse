using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class TriggerVolume : EventTrigger
{
	string targetName;
	bool inverted;
	bool oneTimeUse;

	Door target;
	bool activated = false;


	public TriggerVolume(Vector3 size, string targetName, bool inverted, bool oneTimeUse)
		: base(size, Vector3.Zero, onContact, PhysicsFilter.Default | PhysicsFilter.Player)
	{
		this.targetName = targetName;
		this.inverted = inverted;
		this.oneTimeUse = oneTimeUse;
	}

	static void onContact(EventTrigger t, RigidBody body, ContactType contactType)
	{
		TriggerVolume trigger = t as TriggerVolume;
		if (body.entity == Map.current.player && (!trigger.activated && contactType == ContactType.Found || !trigger.oneTimeUse))
		{
			trigger.target.activate();
			trigger.activated = contactType == ContactType.Found;
		}
	}

	public override void update()
	{
		base.update();

		if (target == null && targetName != null)
		{
			target = scene.getEntityByName(targetName) as Door;
			target.inverted = inverted;
		}
	}
}
