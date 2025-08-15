using Rainfall;
using System;
using System.ComponentModel;


public class Player : Entity
{
	public const uint COLOR_NORMAL = 0xFF5789EE;
	public const uint COLOR_INVERTED = 0xFFE32222;
	public const uint COLOR_NEUTRAL = 0xFFFFFFFF;


	const float CAMERA_HEIGHT = 1.5f;
	const float CAMERA_HEIGHT_DUCKED = 0.9f;


	//public Material blueMaterial;
	//public Material redMaterial;
	//public Material defaultMaterial;

	float cameraHeight = 1.5f;
	float interactRange = 3;

	public FirstPersonController controller;
	public PlayerCamera camera;

	Vector4 color = Vector4.One;
	float emissive = 0;

	AnimationState idleAnim, runAnim, fallAnim;

	Interactable currentInteractable;
	Texture crosshair, crosshairHighlight;

	Sound[] stepSound;


	public Player(PlayerCamera camera)
	{
		this.camera = camera;

		material = Material.CreateDeferred(0xFFFFFFFF);
	}

	public override void init()
	{
		model = Resource.GetModel("models/viewmodel.glb");
		animator = Animator.Create(model);
		idleAnim = Animator.CreateAnimation(model, "idle", true, 0.25f);
		idleAnim.animationSpeed = 0.001f;
		runAnim = Animator.CreateAnimation(model, "run", true, 0.25f);
		fallAnim = Animator.CreateAnimation(model, "fall", true, 0.25f);

		crosshair = Resource.GetTexture("textures/ui/crosshair.png");
		crosshairHighlight = Resource.GetTexture("textures/ui/crosshair_highlight.png");

		stepSound = Resource.GetSounds("sounds/step_bare", 3);

		controller = new FirstPersonController(this);
		controller.onStep = onStep;

		body = new RigidBody(this, RigidBodyType.Kinematic, PhysicsFilter.Player, 0);
		body.addCapsuleCollider(controller.controller.radius, controller.controller.height, Vector3.Up * controller.controller.height * 0.5f, Quaternion.Identity);

		runAnim.animationSpeed = FirstPersonController.STEP_FREQUENCY * 0.5f * controller.maxSpeed;
	}

	public override void destroy()
	{
		Resource.FreeModel(model);
		Animator.Destroy(animator);

		controller.destroy();

		body.destroy();
	}

	void onStep()
	{
		Audio.PlayOrganic(stepSound, position);
	}

	public override void update()
	{
		controller.inputLeft = Input.IsKeyDown(KeyCode.A);
		controller.inputRight = Input.IsKeyDown(KeyCode.D);
		controller.inputUp = Input.IsKeyDown(KeyCode.W);
		controller.inputDown = Input.IsKeyDown(KeyCode.S);
		controller.inputJump = Input.IsKeyPressed(KeyCode.Space);
		controller.inputDuck = Input.IsKeyDown(KeyCode.Ctrl);

		controller.update(Time.deltaTime);

		if (position.y < -10)
		{
			//menu.Restart();
		}

		cameraHeight = controller.isDucked ? CAMERA_HEIGHT_DUCKED :
			controller.inDuckTimer != -1 ? Mathf.Lerp(CAMERA_HEIGHT, CAMERA_HEIGHT_DUCKED, controller.inDuckTimer / FirstPersonController.DUCK_TRANSITION_DURATION) :
			controller.isGrounded ? Mathf.MoveTowards(cameraHeight, CAMERA_HEIGHT, 2 * Time.deltaTime) :
			CAMERA_HEIGHT;

		rotation = Quaternion.AxisAngle(Vector3.Up, camera.yaw);
		camera.position = position + new Vector3(0, cameraHeight, 0);

		Matrix sway = calculateWeaponSway(0);
		modelTransform = camera.getModelMatrix() * sway * Matrix.CreateRotation(Vector3.Up, MathF.PI);

		//mesh.material = timeline.paused || timeline.timelines.Count == 0 ? defaultMaterial : timeline.isInverted ? redMaterial : blueMaterial;

		currentInteractable = null;
		PhysicsHit? hit = Physics.Raycast(camera.position, camera.rotation.forward, interactRange, QueryFilterFlags.Default, PhysicsFilter.Interactable);
		if (hit != null)
		{
			Interactable interactable = hit.Value.body.entity as Interactable;
			Debug.Assert(interactable != null);
			if (interactable.canInteract(this))
			{
				currentInteractable = interactable;
				if (Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsKeyPressed(KeyCode.E))
				{
					interactable.interact(this);
				}
			}
		}

		if (controller.isGrounded)
		{
			if (controller.isRunning)
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

		animator.applyAnimation();

		Vector4 playerColor = Mathf.ARGBToVector(Map.current.timeline.paused ? COLOR_NEUTRAL : Map.current.timeline.isInverted ? COLOR_INVERTED : COLOR_NORMAL);
		float playerEmissive = Map.current.timeline.paused ? 0 : Map.current.timeline.isInverted ? 3 : 0;
		color = Vector4.Lerp(color, playerColor, 5 * Time.deltaTime);
		emissive = Mathf.Lerp(emissive, playerEmissive, 5 * Time.deltaTime);
		material.setData(0, color);
		material.setData(2, new Vector4(color.xyz, emissive));
	}

	Simplex simplex = new Simplex();
	float viewmodelVerticalSpeedAnim;
	Vector3 viewmodelLookSwayAnim;
	Matrix calculateWeaponSway(int side)
	{
		Vector3 sway = Vector3.Zero;
		float yawSway = 0;
		float pitchSway = 0;
		float rollSway = 0;

		float swayScale = 1;

		// Idle animation
		float idleProgress = Time.currentTime / 1e9f * MathF.PI * 2 / 6.0f;
		float idleAnimation = (MathF.Cos(idleProgress) * 0.5f - 0.5f) * 0.03f;
		sway.y += idleAnimation;

		float noiseProgress = Time.currentTime / 1e9f * MathF.PI * 2 * (side == 0 ? 1 / 6.0f : 1.1f / 6.0f) + (side == 0 ? 0 : 100);
		Vector3 noise = new Vector3(simplex.sample1f(noiseProgress * 0.2f), simplex.sample1f(-noiseProgress * 0.2f), simplex.sample1f(100 + noiseProgress * 0.2f)) * 0.015f * swayScale;
		sway += noise;

		// Walk animation
		Vector2 viewmodelWalkAnim = Vector2.Zero;
		viewmodelWalkAnim.x = 0.03f * MathF.Sin(controller.distanceWalked * FirstPersonController.STEP_FREQUENCY * MathF.PI);
		viewmodelWalkAnim.y = 0.015f * -MathF.Abs(MathF.Cos(controller.distanceWalked * FirstPersonController.STEP_FREQUENCY * MathF.PI));
		//viewmodelWalkAnim *= 1 - MathHelper.Smoothstep(1.0f, 1.5f, movementSpeed);
		viewmodelWalkAnim *= 1 - MathF.Exp(-controller.velocity.xz.length);
		//viewmodelWalkAnim *= (sprinting && runAnim.layers[1 + 0] != null && runAnim.layers[1 + 0].animationName == "run" || movement.isMoving && walkAnim.layers[1 + 0] != null && walkAnim.layers[1 + 0].animationName == "walk") ? 0 : 1;
		yawSway += viewmodelWalkAnim.x;
		sway.y += viewmodelWalkAnim.y;

		// Vertical speed animation
		float verticalSpeedAnimDst = controller.velocity.y;
		verticalSpeedAnimDst = Math.Clamp(verticalSpeedAnimDst, -5.0f, 5.0f);
		viewmodelVerticalSpeedAnim = Mathf.Lerp(viewmodelVerticalSpeedAnim, verticalSpeedAnimDst * 0.0075f, 1 - MathF.Pow(0.5f, 5 * Time.deltaTime));
		pitchSway += viewmodelVerticalSpeedAnim;

		// Land bob animation
		float timeSinceLanding = (Time.currentTime - controller.lastLandedTime) / 1e9f;
		float landBob = (1.0f - MathF.Pow(0.5f, timeSinceLanding * 4.0f)) * MathF.Pow(0.1f, timeSinceLanding * 4.0f) * 0.5f;
		sway.y -= landBob;

		// Look sway
		float swayYawDst = camera.yaw; // -0.0015f * Input.cursorMove.x;
		float swayPitchDst = camera.pitch; // -0.0015f * Input.cursorMove.y;
		float swayRollDst = camera.yaw; // -0.0015f * Input.cursorMove.x;
		viewmodelLookSwayAnim = Vector3.Lerp(viewmodelLookSwayAnim, new Vector3(swayPitchDst, swayYawDst, swayRollDst), 1 - MathF.Pow(0.5f, 5 * Time.deltaTime));
		pitchSway -= viewmodelLookSwayAnim.x - swayPitchDst;
		yawSway -= viewmodelLookSwayAnim.y - swayYawDst;
		rollSway -= viewmodelLookSwayAnim.z - swayRollDst;

		sway *= swayScale;
		pitchSway *= swayScale * 0.1f;
		yawSway *= swayScale * 0.1f;
		rollSway *= swayScale * 0.1f;

		return Matrix.CreateTranslation(sway) * Matrix.CreateRotation(Vector3.Up, yawSway) * Matrix.CreateRotation(Vector3.Right, pitchSway) * Matrix.CreateRotation(Vector3.Back, rollSway);
	}

	public override void draw(GraphicsDevice graphics)
	{
		Renderer.DrawModel(model, modelTransform, material, animator);

		GUI.Texture(Display.width / 2 - crosshair.width / 2, Display.height / 2 - crosshair.height / 2, crosshair);

		if (currentInteractable != null)
		{
			GUI.Texture(Display.width / 2 - crosshair.width / 2, Display.height / 2 - crosshair.height / 2, crosshair.width, crosshair.height, crosshairHighlight);
		}

#if DEBUG
		Renderer.graphics.drawDebugText(0, 3, "x=" + position.x + ", " + "y=" + position.y + ", " + "z=" + position.z);
#endif
	}
}
