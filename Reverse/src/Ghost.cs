using Rainfall;


public class Ghost : Entity
{
	static Material defaultMaterial;
	static Material invertedMaterial;

	static Ghost()
	{
		defaultMaterial = Material.CreateDeferred(Player.COLOR_NORMAL);
		invertedMaterial = Material.CreateDeferred(Player.COLOR_INVERTED, 0, 1, Mathf.ARGBToVector(Player.COLOR_INVERTED).xyz, 3);
	}


	AnimationState idleAnim, runAnim, fallAnim;

	public Vector3 targetPosition;
	public Quaternion targetRotation;

	bool visible;
	bool inverted;
	bool running;
	bool grounded;


	public Ghost(bool inverted)
	{
		this.inverted = inverted;
	}

	public override void init()
	{
		model = Resource.GetModel("models/player.glb");
		animator = Animator.Create(model);
		idleAnim = Animator.CreateAnimation(model, "idle", true, 0.25f);
		runAnim = Animator.CreateAnimation(model, "run", true, 0.25f);
		fallAnim = Animator.CreateAnimation(model, "fall", true, 0.25f);

		modelTransform = Matrix.CreateRotation(Vector3.Up, MathF.PI);

		material = inverted ? invertedMaterial : defaultMaterial;
	}

	public override void update()
	{
		position = Vector3.Lerp(position, targetPosition, 5 * Time.deltaTime);
		rotation = Quaternion.Slerp(rotation, targetRotation, 5 * Time.deltaTime);

		if (grounded)
		{
			if (running)
			{
				animator.setAnimation(runAnim);
			}
			else
			{
				animator.setAnimation(idleAnim);
			}
		}
		else
		{
			animator.setAnimation(fallAnim);
		}

		animator.currentAnimation.animationSpeed = inverted == Map.current.timeline.isInverted ? 1 : -1;

		animator.applyAnimation();
	}

	public void updateState(bool visible, PlayerState frame)
	{
		targetPosition = frame.position;
		targetRotation = frame.rotation;
		running = frame.running;
		grounded = frame.grounded;

		if (visible && !this.visible)
		{
			position = targetPosition;
			rotation = targetRotation;
		}

		this.visible = visible;
	}

	public override void draw(GraphicsDevice graphics)
	{
		if (visible)
		{
			base.draw(graphics);
		}
	}
}