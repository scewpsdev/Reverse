using Rainfall;


public struct PlayerState
{
	public Vector3 position;
	public Quaternion rotation;
	public bool running;
	public bool grounded;
}

public struct Timeline
{
	public float startTime;
	public int direction;
	public List<PlayerState> frames;

	public void getCurrentFrame(float time, out bool visible, out PlayerState frame)
	{
		float timeSinceStart = (time - startTime) * direction;
		int fps = 60;
		int frameIdx = (int)MathF.Floor(timeSinceStart * fps);
		if (frameIdx >= 0 && frameIdx < frames.Count)
		{
			visible = true;
			frame = frames[frameIdx];
		}
		else
		{
			visible = false;
			frame = new PlayerState();
		}
	}
}

public class TimelineManager : Entity
{
	public float time;
	public int direction => timelines.Count % 2 * 2 - 1;
	public bool paused = false;

	public bool hasWon = false;

	public List<Timeline> timelines = new List<Timeline>();
	List<Ghost> ghosts = new List<Ghost>();


	public override void init()
	{
	}

	public override void update()
	{
		if (Input.IsKeyPressed(KeyCode.F1))
			startCycle();
	}

	public override void fixedUpdate(float delta)
	{
		if (!paused)
		{
			if (timelines.Count > 0)
			{
				time += direction * delta;
			}

			updateCycle();
		}
	}

	public void setPaused(bool paused)
	{
		this.paused = paused;
	}

	public void startCycle()
	{
		Timeline timeline = new Timeline();
		timeline.startTime = time;
		timeline.direction = -(timelines.Count % 2 * 2 - 1);
		timeline.frames = new List<PlayerState>();
		timelines.Add(timeline);

		if (timelines.Count > 1)
		{
			Ghost ghost = createGhost(timelines[timelines.Count - 2]);
			ghosts.Add(ghost);
		}
	}

	void updateCycle()
	{
		for (int i = 0; i < timelines.Count; i++)
		{
			if (i < timelines.Count - 1)
			{
				timelines[i].getCurrentFrame(time, out bool visible, out PlayerState frame);
				ghosts[i].updateState(visible, frame);
			}
			else
			{
				timelines[i].frames.Add(capturePlayerState());
			}
		}
	}

	PlayerState capturePlayerState()
	{
		PlayerState state = new PlayerState();
		state.position = Map.current.player.position;
		state.rotation = Map.current.player.rotation;
		state.running = Map.current.player.controller.isRunning;
		state.grounded = Map.current.player.controller.isGrounded;
		return state;
	}

	Ghost createGhost(Timeline timeline)
	{
		timeline.getCurrentFrame(time, out _, out PlayerState frame);
		Ghost ghost = new Ghost(timeline.direction == -1);
		scene.addEntity(ghost, frame.position, frame.rotation);
		return ghost;
	}

	public override void draw(GraphicsDevice graphics)
	{
		Renderer.graphics.drawDebugText(0, 1, "t = " + time);
		Renderer.graphics.drawDebugText(0, 2, "cycles = " + timelines.Count);

		//if (hasWon)
		//{
		//	Vector2 victoryLabelSize = defaultStyle.CalcSize(victoryLabel);
		//	GUI.Label(new Rect(Screen.width / 2 - victoryLabelSize.x / 2, Screen.height / 2 - victoryLabelSize.y / 2, 100, 100), victoryLabel, defaultStyle);
		//}
	}

	public bool isInverted => timelines.Count > 0 && timelines.Count % 2 == 0;
}
