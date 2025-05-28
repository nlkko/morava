using Godot;
using System;

public partial class FreeCameraMovement : Node3D
{
	[Export]
	float velocity = 4.0f;

	[Export]
	float orbit_velocity = 4.0f;
	private float _target_orbit;


	[Export]
	float circular_radius = 0.0f;

	[Export]
	float circular_velocity = 0.2f;

	private float _circular_angle = 0.0f;

	private Camera3D _camera;

	public override void _Ready()
	{
		_target_orbit = Rotation.Y;
		_camera = GetNode<Camera3D>("Camera");
	}

	public override void _Process(double delta)
	{
		// movement
		Vector2 input_vector = Input.GetVector("camera_left", "camera_right", "camera_backward", "camera_forward");

		Basis yaw = new Basis(Basis.X, Vector3.Up, Basis.Z).Orthonormalized();

		// scaling forward movement so pitched ortho camera velocity seems constant
		Vector3 move_vector = yaw * new Vector3(input_vector.X, 0, input_vector.Y / Mathf.Sin(Rotation.X));
		Position += move_vector * velocity * (float)delta;

		// orbit
		if (Input.IsActionJustPressed("camera_orbit_right"))
		{
			_target_orbit += Mathf.Tau / 8;
		}
		if (Input.IsActionJustPressed("camera_orbit_left"))
		{
			_target_orbit -= Mathf.Tau / 8;
		}

		Rotation = new Vector3(
			Rotation.X,
			Mathf.Lerp(Rotation.Y, _target_orbit, 1.0f - (float)Math.Pow(2.0, -4.0 * delta * orbit_velocity)),
			Rotation.Z
			);

		if (Math.Abs(Rotation.Y - _target_orbit) < 0.02)
		{
			Rotation = new Vector3(Rotation.X, _target_orbit, Rotation.Z);
		}

		// circling
		if (circular_radius != 0)
		{
			_circular_angle -= (float)Math.Tau * circular_velocity * (float)delta;
			_camera.Position = new Vector3(
				(float)Math.Cos(_circular_angle) * circular_radius,
				(float)Math.Sin(_circular_angle) * circular_radius,
				_camera.Position.Z
				);
		}
	}
}
