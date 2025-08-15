using Rainfall;


public class PlayerCamera : Camera
{
	public float sensitivity = 0.002f;


	public override void init()
	{
		yaw = rotation.eulers.y;
	}

	public override void update()
	{
		bool locked = true; // !interact.isPaused;
		Input.cursorMode = locked ? CursorMode.Disabled : CursorMode.Normal;

		if (locked)
		{
			pitch = Mathf.Clamp(pitch - Input.cursorMove.y * sensitivity, -0.5f * MathF.PI, 0.5f * MathF.PI);
			yaw -= Input.cursorMove.x * sensitivity;
			rotation = Quaternion.AxisAngle(Vector3.Up, yaw) * Quaternion.AxisAngle(Vector3.Right, pitch);
		}
	}
}