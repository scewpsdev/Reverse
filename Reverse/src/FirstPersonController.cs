﻿using Rainfall;


public class FirstPersonController
{
	public enum MoveType
	{
		Walk,
		Ladder,
	}

	class CollisionCallback : ControllerHitCallback
	{
		FirstPersonController movement;


		public CollisionCallback(FirstPersonController movement)
		{
			this.movement = movement;
		}

		public void onShapeHit(ControllerHit hit)
		{
			movement.onCollision(hit.position, hit.normal);
		}
	}


	public const float COLLIDER_HEIGHT = 1.75f;
	public const float COLLIDER_HEIGHT_DUCKED = 1.15f;
	//public static readonly Vector3 COLLIDER_CENTER = new Vector3(0, 0.5f * COLLIDER_HEIGHT, 0);
	//public static readonly Vector3 COLLIDER_CENTER_DUCKED = new Vector3(0, 0.5f * COLLIDER_HEIGHT_DUCKED, 0);
	//public static readonly Vector3 COLLIDER_CENTER_DUCKED_AIR = new Vector3(0, 0.5f * COLLIDER_HEIGHT_DUCKED + (COLLIDER_HEIGHT - COLLIDER_HEIGHT_DUCKED), 0);

	const float MAX_AIR_SPEED = 0.3f;
	public const float LADDER_SPEED = 1.0f;
	const float ACCELERATION = 10.0f;
	const float AIR_ACCELERATION = 10.0f;
	const float FRICTION = 6.0f;
	const float STOP_SPEED = 1.0f;

	const float GRAVITY = -12.0f;
	const float JUMP_HEIGHT = 1.0f;
	const float JUMP_POWER = 6; //4.89898f; // sqrt(2*-gravity*jumpHeight)
	const float JUMP_PEAK_TIME = JUMP_POWER / GRAVITY; // 0.387f
	const float JUMP_BUFFER_TIME = 0.3f;
	const float JUMP_COYOTE_TIME = 0.2f;
	const int JUMP_STAMINA_COST = 3;
	const float JUMP_POWER_LADDER = 2.7f;
	const float DODGE_RELEASE_WINDOW = 0.3f;
	public const float DUCK_TRANSITION_DURATION = 0.24f;

	public const float STEP_FREQUENCY = 0.6f;
	const float FALL_IMPACT_MIN_SPEED = 1.0f;
	const float FALL_DMG_THRESHHOLD = 12.0f;


	public CharacterController controller;

	MoveType moveType = MoveType.Walk;

	public bool inputLeft, inputRight, inputUp, inputDown;
	public bool inputJump, inputDuck;

	Vector3 fsu = Vector3.Zero;

	public bool isDucked { get; private set; } = false;
	public float inDuckTimer = -1.0f;

	public float maxSpeed = 3.6f * 1.5f;

	public bool isGrounded = false;
	RigidBody currentPlatform;
	Vector3 lastPlatformPosition;

	public float distanceWalked = 0.0f;
	public int lastStep = 0;
	public float airborneTime = 0.0f;

	float lastJumpInput = -1;

	float lastGroundedTime = 0;
	public float lastJumpedTime = 0;
	public float lastLandedTime = 0;

	public Vector3 velocity;
	float lastVerticalVelocity;

	public Func<bool> canJump;
	public Action onJump;
	public Action onStep;
	public Action onLadderStep;
	public Action<float> onLand;


	public FirstPersonController(Entity entity, float radius = 0.3f, float height = 2.0f, float stepOffset = 0.3f, uint filterMask = 1)
	{
		controller = new CharacterController(entity, radius, Vector3.Up * height * 0.5f, height, stepOffset, filterMask, new CollisionCallback(this));
	}

	public void destroy()
	{
		controller.destroy();
	}

	Vector3 updateMovementInputs()
	{
		Vector3 fsu = Vector3.Zero;

		{
			//if (currentAction == null || !currentAction.lockMovement)
			{
				if (inputLeft)
					fsu.x--;
				if (inputRight)
					fsu.x++;
				if (inputDown)
					fsu.z--;
				if (inputUp)
					fsu.z++;
			}

			{
				/*
				if (currentAction != null)
				{
					fsu += currentAction.movementInput;
				}
				*/

				if (fsu.lengthSquared > 0.0f)
				{
					fsu = fsu.normalized;
					fsu *= maxSpeed;

					/*
					if (currentAction != null)
					{
						fsu *= currentAction.movementSpeedMultiplier;
					}
					*/
				}
			}
		}

		//if (currentAction == null || currentAction.movementSpeedMultiplier > 0.0f)
		{
			if (inputJump)
			{
				lastJumpInput = Time.gameTime;
			}

			if (inputDuck)
			{
				if (inDuckTimer == -1.0f)
				{
					if (isGrounded)
						inDuckTimer = 0.0f;
					else if (!isDucked)
					{
						isDucked = true;
						controller.move(Vector3.Up * (COLLIDER_HEIGHT - COLLIDER_HEIGHT_DUCKED));
					}
				}
			}
			else
			{
				if (isDucked)
				{
					Span<PhysicsHit> hits = stackalloc PhysicsHit[16];
					int numHits = Physics.SweepSphere(controller.radius, controller.entity.getPosition() + new Vector3(0.0f, controller.height - controller.radius, 0.0f), Vector3.Up, 0.5f, hits);

					bool headBlocked = false;
					for (int i = 0; i < numHits; i++)
					{
						if (hits[i].body != null && hits[i].body.entity != controller.entity)
						//if (!hits[i].isTrigger)
						{
							headBlocked = true;
							break;
						}
					}

					if (!headBlocked)
					{
						isDucked = false;
						inDuckTimer = -1.0f;

						if (!isGrounded)
						{
							controller.move(Vector3.Up * (COLLIDER_HEIGHT_DUCKED - COLLIDER_HEIGHT));
						}
					}
				}
				else if (inDuckTimer != -1)
				{
					inDuckTimer = -1;
					isDucked = false;
				}
			}
		}


		/*
		if (currentAction != null && currentAction.maxSpeed != 0.0f)
			fsu *= currentAction.maxSpeed / MAX_GROUND_SPEED;
		else if (isGrounded)
		{
			if (isDucked)
			{
				fsu *= DUCK_SPEED_MULTIPLIER;
			}
			else
			{
				switch (walkMode)
				{
					case WalkMode.Walk:
						fsu *= WALK_SPEED_MULTIPLIER;
						break;
					case WalkMode.Sprint:
						fsu *= SPRINT_SPEED_MULTIPLIER;
						break;
					default:
						break;
				}
			}
		}
		*/


		/*
		if (isCursorLocked && (currentAction == null || !currentAction.lockRotation))
		{
			Vector2 lookVector = InputManager.lookVector;
			yaw -= lookVector.x;
			pitch = Math.Clamp(pitch - lookVector.y, -MathHelper.PiOver2, MathHelper.PiOver2);
		}
		*/


		return fsu;
	}

	static Vector3 friction(Vector3 velocity, float frametime)
	{
		float entityFriction = 1.0f;
		float edgeFriction = 1.0f;
		float fric = FRICTION * entityFriction * edgeFriction; // sv_friction * ke * ef

		float l = velocity.length;
		Vector3 vn = velocity / l;

		if (l >= STOP_SPEED)
			return (1.0f - frametime * fric) * velocity;
		else if (l >= MathF.Max(0.01f, frametime * STOP_SPEED * fric) && l < STOP_SPEED)
			return velocity - frametime * STOP_SPEED * fric * vn;
		else // if (l < MathHelper.Max(0.1f, frametime * STOP_SPEED * fric)
			return Vector3.Zero;
	}

	static Vector3 updateVelocityGround(Vector3 velocity, Vector3 wishdir, float frametime, float maxSpeed, Vector3 forward, Vector3 right, Vector3 up)
	{
		velocity.y = velocity.y + 0.5f * GRAVITY * frametime;

		Vector3 accel = wishdir.x * right + wishdir.y * up + wishdir.z * forward;
		float accelMag = accel.length;
		Vector3 accelDir = accelMag > 0.0f ? accel / accelMag : Vector3.Zero;

		float entityFriction = 1.0f;

		velocity = friction(velocity, frametime);
		float m = MathF.Min(maxSpeed, wishdir.length);
		float currentSpeed = Vector3.Dot(velocity, accelDir);
		float l = m;
		float addSpeed = Mathf.Clamp(l - currentSpeed, 0.0f, entityFriction * frametime * m * ACCELERATION);

		velocity = velocity + accelDir * addSpeed;

		velocity.y = velocity.y + 0.5f * GRAVITY * frametime;

		return velocity;
	}

	Vector3 updateVelocityAir(Vector3 velocity, Vector3 wishdir, float frametime, Vector3 forward, Vector3 right, Vector3 up)
	{
		velocity.y = velocity.y + 0.5f * GRAVITY * frametime;

		Vector3 accel = wishdir.x * right + wishdir.y * up + wishdir.z * forward;
		float accelMag = accel.length;
		Vector3 accelDir = accelMag > 0.0f ? accel / accelMag : Vector3.Zero;

		float entityFriction = 1.0f;

		float m = MathF.Min(maxSpeed, wishdir.length);
		float currentSpeed = Vector3.Dot(velocity, accelDir);
		float l = MathF.Min(m, MAX_AIR_SPEED);
		float addSpeed = Mathf.Clamp(l - currentSpeed, 0.0f, entityFriction * frametime * m * AIR_ACCELERATION);

		velocity = velocity + accelDir * addSpeed;

		velocity.y = velocity.y + 0.5f * GRAVITY * frametime;

		return velocity;
	}

	Vector3 updateVelocityLadder(Vector3 velocity, Vector3 wishdir, Vector3 ladderNormal, float frametime, Vector3 forward, Vector3 right, Vector3 up, bool topEdge, bool bottomEdge)
	{
		Vector3 u = wishdir.x * right + wishdir.y * up + wishdir.z * forward;
		Vector3 n = ladderNormal;

		if (topEdge || bottomEdge && Vector3.Dot(u, n) > 0.0f)
		{
			Vector3 cu = Vector3.Cross(Vector3.Up, n);
			velocity = u - Vector3.Dot(u, n) * (Vector3.Cross(n, cu / cu.length));
			velocity *= LADDER_SPEED * 0.5f;
		}
		else
		{
			Vector3 cu = Vector3.Cross(Vector3.Up, n);
			velocity = u - Vector3.Dot(u, n) * (n + Vector3.Cross(n, cu / cu.length));
			velocity *= LADDER_SPEED;
		}

		return velocity;
	}

	public void update(float deltaTime)
	{
		fsu = updateMovementInputs();

		Vector3 forward = controller.entity.getRotation().forward;
		Vector3 right = controller.entity.getRotation().right;
		Vector3 up = controller.entity.getRotation().up;


		if (moveType == MoveType.Walk)
		{
			if (lastJumpInput != -1 && Time.gameTime - lastJumpInput <= JUMP_BUFFER_TIME)
			{
				if ((isGrounded || Time.gameTime - lastGroundedTime <= JUMP_COYOTE_TIME) && velocity.y < 0.5f * JUMP_POWER)
				{
					velocity.y = JUMP_POWER;
					isGrounded = false;
					lastJumpInput = 0;

					lastJumpedTime = Time.gameTime;

					if (onJump != null)
						onJump();
				}
			}


			if (isGrounded)
			{
				velocity = updateVelocityGround(velocity, fsu, deltaTime, maxSpeed, forward, right, up);
			}
			else
			{
				velocity = updateVelocityAir(velocity, fsu, deltaTime, forward, right, up);
			}


			// Position update
			{
				Vector3 displacement = velocity * deltaTime;

				// Root Motion
				/*
				if (isGrounded && currentAction != null && currentAction.rootMotion)
				{
					Vector3 rootMotionDisplacement = currentActionState[2].layers[0].rootMotionDisplacement;
					rootMotionDisplacement = Quaternion.FromAxisAngle(Vector3.Up, MathF.PI + yaw) * rootMotionDisplacement;
					displacement += rootMotionDisplacement;
				}
				*/

				if (currentPlatform != null)
				{
					Vector3 currentPlatformPosition = currentPlatform.entity.getPosition();
					if (lastPlatformPosition != Vector3.Zero)
					{
						Vector3 delta = currentPlatformPosition - lastPlatformPosition;
						displacement += delta;
						displacement.y = MathF.Max(displacement.y, delta.y);
					}
					lastPlatformPosition = currentPlatformPosition;
				}
				else
				{
					lastPlatformPosition = Vector3.Zero;
				}

				isGrounded = false;
				ControllerCollisionFlag flags = controller.move(displacement);
				if ((flags & ControllerCollisionFlag.Down) != 0)
				{
					velocity.y = MathF.Max(velocity.y, -velocity.xz.length);
					isGrounded = true;
					lastGroundedTime = Time.gameTime;
				}

				currentPlatform = null;
				//if (velocity.y < 0.5f)
				{
					Span<PhysicsHit> hits = stackalloc PhysicsHit[16];
					int numHits = Physics.OverlapSphere(controller.radius, controller.entity.getPosition() + Vector3.Up * (controller.radius - 0.2f), hits);
					if (hits != null && hits.Length > 0)
					{
						for (int i = 0; i < hits.Length; i++)
						{
							if (hits[i].body != null && hits[i].body.entity != controller.entity)
							{
								isGrounded = true;
								lastGroundedTime = Time.gameTime;
								if (hits[i].body != null && hits[i].body.type == RigidBodyType.Kinematic)
									currentPlatform = hits[i].body;
								break;
							}
						}
					}
				}
			}

			if (isGrounded)
			{
				distanceWalked += velocity.xz.length * deltaTime;
				int stepsWalked = (int)(distanceWalked * STEP_FREQUENCY);
				if (stepsWalked > lastStep)
				{
					if (onStep != null)
						onStep();
					lastStep = stepsWalked;
				}
			}
		}

		if (!isGrounded)
			airborneTime += deltaTime;

		if (inDuckTimer >= 0.0f)
		{
			inDuckTimer += deltaTime;
			if (!isGrounded || inDuckTimer >= DUCK_TRANSITION_DURATION)
			{
				isDucked = true;
			}
		}

		if (isDucked)
		{
			controller.resize(COLLIDER_HEIGHT_DUCKED);
			if (isGrounded)
				controller.offset = Vector3.Zero;
			else
				controller.offset = new Vector3(0, COLLIDER_HEIGHT - COLLIDER_HEIGHT_DUCKED, 0);
		}
		else
		{
			controller.resize(COLLIDER_HEIGHT);
			controller.offset = Vector3.Zero;
		}

		lastVerticalVelocity = velocity.y;
	}

	public void onCollision(Vector3 position, Vector3 normal)
	{
		if (Vector3.Dot(velocity, normal) < 0.0f)
		{
			Vector3 lastVelocity = velocity;
			// If this is a slope, don't modify velocity to allow for smooth climbing
			if (MathF.Abs(normal.x) > 0.999f || MathF.Abs(normal.y) > 0.999f || MathF.Abs(normal.z) > 0.999f || normal.y < 0.001f)
			{
				float bounceCoefficient = 1.0f;
				Vector3 newVelocity = velocity - bounceCoefficient * Vector3.Dot(velocity, normal) * normal;
				velocity = newVelocity;
			}

			float velocityChange = MathF.Abs(velocity.y - lastVelocity.y);
			bool groundHit = normal.y > 0.5f && velocityChange > FALL_IMPACT_MIN_SPEED;
			if (groundHit)
			{
				lastLandedTime = Time.gameTime;

				if (onLand != null)
					onLand(velocityChange);
			}
		}
	}

	public bool isRunning => fsu.lengthSquared > 0 && velocity.lengthSquared > 0.1f;
}