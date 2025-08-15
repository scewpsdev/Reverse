using Rainfall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Map
{
	public static Map current;


	public int id;
	public string path;

	Cubemap skybox;
	DirectionalLight sun;

	public Scene scene;
	public TimelineManager timeline;
	public PlayerCamera camera;
	public Player player;

	Matrix playerSpawn = Matrix.Identity;


	public Map(int id, string path)
	{
		current = this;

		this.id = id;
		this.path = path;

		skybox = Resource.GetCubemap("rainfall/sky_cubemap_equirect.png");
		sun = new DirectionalLight(Quaternion.AxisAngle(Vector3.Up, 30 * Mathf.Deg2Rad) * Quaternion.AxisAngle(Vector3.Forward, 50 * Mathf.Deg2Rad) * Vector3.Right,
			new Vector3(1.0f, 0.9f, 0.7f) * 40, Renderer.graphics);

		scene = new Scene();

		loadMap(path);

		scene.addEntity(timeline = new TimelineManager());
		scene.addEntity(camera = new PlayerCamera(), playerSpawn);
		scene.addEntity(player = new Player(camera), playerSpawn);

		AudioManager.SetAmbientSound(Resource.GetSound("sounds/ambience.ogg"));
	}

	public void destroy()
	{
		scene.destroy();

		if (current == this)
			current = null;
	}

	void loadMap(string path)
	{
		Model map = Resource.GetModel(path);
		if (map != null)
			loadNode(map.skeleton.rootNode, map);
		else
			scene = null;
	}

	unsafe Entity loadNode(Node node, Model map)
	{
		Entity entity = null;
		bool isKinematic = false;
		bool isInteractable = false;
		bool isInverted = false;
		string target = null;

		bool isPrefab = node.getPropertyCount(map) > 0;
		if (isPrefab)
		{
			for (int i = 0; i < node.getPropertyCount(map); i++)
			{
				node.getProperty(i, map, out CustomProperty* property);
				string name = new string((sbyte*)property->name);

				if (name == "kinematic")
					isKinematic = true;
				else if (name == "interactable")
					isInteractable = true;
				else if (name == "inverted")
					property->getBool(out isInverted);
				else if (name == "target")
					property->getString(out target);
			}

			for (int i = 0; i < node.getPropertyCount(map); i++)
			{
				node.getProperty(i, map, out CustomProperty* property);
				string name = new string((sbyte*)property->name);

				if (name == "elevator")
				{
					property->getDouble(out double goal);
					entity = goal > 0.5 ? new GoalElevator() : new StartElevator();
				}
				else if (name == "barrier")
					entity = new Barrier(isInverted);
				else if (name == "door")
				{
					property->getDouble(out double closed);
					entity = new Door(closed < 0.5);
				}
				else if (name == "button")
					entity = target == "reset" ? new ResetButton() : new Button(target, isInverted);
				else if (name == "turnstile")
					entity = new Turnstile();
				else if (name == "trigger")
				{
					property->getDouble(out double oneTimeUse);
					entity = new TriggerVolume(node.transform.scale * 2, target, false, oneTimeUse < 0.5);
				}
			}
		}

		if (node.name.StartsWith("playerspawn"))
		{
			playerSpawn = node.transform;
			return null;
		}

		if (entity == null)
			entity = new Entity();

		if (node.meshes.Length > 0)
		{
			entity.name = node.name;
			entity.model = map;
			entity.meshNode = node;

			uint filterGroup = isInteractable ? PhysicsFilter.Default | PhysicsFilter.Interactable : PhysicsFilter.Default;
			entity.body = new RigidBody(entity, isKinematic ? RigidBodyType.Kinematic : RigidBodyType.Static, filterGroup);
			for (int i = 0; i < node.meshes.Length; i++)
			{
				MeshCollider collider = Physics.CreateMeshCollider(map.getMeshData(node.meshes[i]), Matrix.Identity);
				entity.body.addMeshCollider(collider, Matrix.Identity);
			}
		}

		if (node.children != null)
		{
			for (int i = 0; i < node.children.Length; i++)
			{
				Entity child = loadNode(node.children[i], map);
				if (child != null)
				{
					if (isPrefab)
						entity.addChild(child, node.children[i].transform);
					else
						scene.addEntity(child, node.children[i].transform);
				}
			}
		}

		return entity;
	}

	public void update()
	{
		Animator.Update(camera.getModelMatrix());
		ParticleSystem.Update(camera.position, camera.rotation);

		scene.update();
	}

	public void fixedUpdate(float delta)
	{
		scene.fixedUpdate(delta);
	}

	public void draw(GraphicsDevice graphics)
	{
		Renderer.DrawSky(skybox, 2, Quaternion.Identity);
		Renderer.DrawEnvironmentMap(skybox, 1);
		Renderer.DrawDirectionalLight(sun);

		scene.draw(graphics);
	}
}
