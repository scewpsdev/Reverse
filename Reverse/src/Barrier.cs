using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Barrier : Entity
{
	static Material defaultMaterial;
	static Material invertedMaterial;

	static Barrier()
	{
		//defaultMaterial = Material.CreateDeferred(Mathf.VectorToARGB(new Vector4(0, 0, 1, 0.3f)));
		//invertedMaterial = Material.CreateDeferred(Mathf.VectorToARGB(new Vector4(1, 0, 0, 0.3f)));
		defaultMaterial = Material.CreateForward(Mathf.ColorAlpha(Player.COLOR_NORMAL, 0.3f), 0, 1, Mathf.SRGBToLinear(Mathf.ARGBToVector(Player.COLOR_NORMAL).xyz), 5);
		invertedMaterial = Material.CreateForward(Mathf.ColorAlpha(Player.COLOR_INVERTED, 0.3f), 0, 1, Mathf.SRGBToLinear(Mathf.ARGBToVector(Player.COLOR_INVERTED).xyz), 5);
	}


	bool inverted;


	public Barrier(bool inverted)
	{
		this.inverted = inverted;
	}

	public override void init()
	{
		base.init();

		body = new RigidBody(this, RigidBodyType.Static);
		body.addBoxCollider(scale, Vector3.Zero, Quaternion.Identity);
	}

	public override void update()
	{
		base.update();

		body.setActive(inverted != Map.current.timeline.isInverted);
	}

	public override void draw(GraphicsDevice graphics)
	{
		base.draw(graphics);

		Renderer.DrawModel(Resource.GetModel("rainfall/cube.gltf"), getModelMatrix(), inverted ? invertedMaterial : defaultMaterial, null, false, false);
	}
}
