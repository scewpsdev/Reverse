using Rainfall;
using System.IO;
using System.Net.Mail;


public class GameState : State
{
	public static GameState instance;

	static string[] mapList = [
		"maps/level1.glb",
		"maps/level2.glb",
		"maps/level3.glb",
		"maps/level3.1.glb",
		"maps/level4.glb",
	];


	public Map map;

	int queuedMap = -1;


	public override void init()
	{
		instance = this;

		loadMap(0);
	}

	public void loadMap(int id)
	{
		queuedMap = id;
	}

	public void unloadMap()
	{
		map.destroy();
		map = null;
	}

	public void resetMap()
	{
		loadMap(map.id);
	}

	public override void destroy()
	{
		if (map != null)
			unloadMap();
	}

	public override void update()
	{
		if (queuedMap != -1)
		{
			if (map != null)
				unloadMap();
			if (queuedMap < mapList.Length)
			{
				map = new Map(queuedMap, mapList[queuedMap]);
				if (map.scene == null)
				{
					map.destroy();
					map = null;
				}
			}
			queuedMap = -1;
		}

		if (map != null)
		{
			map.update();

			if (map.player.position.y < -10)
				resetMap();
		}
	}

	public override void fixedUpdate(float delta)
	{
		if (map != null)
			map.fixedUpdate(delta);
	}

	public override void draw(GraphicsDevice graphics)
	{
		if (map != null)
			map.draw(graphics);
		else
			Renderer.graphics.drawDebugText(1, 10, "No map loaded");
	}
}