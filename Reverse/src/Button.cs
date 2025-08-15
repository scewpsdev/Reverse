using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


public class Button : Entity, Interactable
{
	static Material defaultMaterial;
	static Material invertedMaterial;

	static Button()
	{
		defaultMaterial = Material.CreateDeferred(Player.COLOR_NORMAL);
		invertedMaterial = Material.CreateDeferred(Player.COLOR_INVERTED, 0, 1, Mathf.ARGBToVector(Player.COLOR_INVERTED).xyz, 3);
	}


	string doorName;
	bool inverted;
	Door door;

	Vector3 startPosition;

	bool pressed = false;

	Sound pressSound;


	public Button(string doorName, bool inverted)
	{
		this.doorName = doorName;
		this.inverted = inverted;

		pressSound = Resource.GetSound("sounds/button.ogg");
	}

	public override void init()
	{
		base.init();

		material = inverted ? invertedMaterial : defaultMaterial;

		startPosition = position;
	}

	public override void update()
	{
		base.update();

		if (door == null && doorName != null)
		{
			door = scene.getEntityByName(doorName) as Door;
			door.inverted = inverted;
		}

		if (pressed)
		{
			position = Vector3.Lerp(position, startPosition + rotation.forward * 0.2f, 5 * Time.deltaTime);
		}
	}

	public bool canInteract(Player player)
	{
		return Map.current.timeline.isInverted == inverted && !pressed;
	}

	public void interact(Player player)
	{
		pressed = true;
		door.activate();

		Audio.PlayOrganic(pressSound, position);
	}
}

public class ResetButton : Entity, Interactable
{
	static new Material material;

	static ResetButton()
	{
		material = Material.CreateDeferred(0xFF58a938);
	}


	Vector3 startPosition;

	bool pressed = false;

	Sound pressSound;


	public ResetButton()
	{
		pressSound = Resource.GetSound("sounds/button.ogg");
	}

	public override void init()
	{
		base.init();

		base.material = material;

		startPosition = position;
	}

	public override void update()
	{
		base.update();

		if (pressed)
		{
			position = Vector3.Lerp(position, startPosition + rotation.forward * 0.2f, 5 * Time.deltaTime);
		}
	}

	public bool canInteract(Player player)
	{
		return !pressed;
	}

	public void interact(Player player)
	{
		pressed = true;
		GameState.instance.resetMap();

		Audio.PlayOrganic(pressSound, position);
	}
}
