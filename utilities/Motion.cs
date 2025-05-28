using Godot;
using System;

public partial class Motion: Node3D
{
	// rotation
	[Export]
	Vector3 axis = Vector3.Up;

	[Export]
	float radial_velocity = 0.0f;

	// bobbing
	[Export]
	public float bob_amplitude = 0.0f; // The height of the bobbing motion

	[Export]
	public float bob_frequency = 0.0f; // How fast the object bobs up and down

	// circling
	[Export]
	float circular_radius = 0.0f;

	[Export]
	float circular_velocity = 0.0f;

	private float _circular_angle = 0.0f;

	private Vector3 initial_position;

	public override void _Ready()
	{
		initial_position = GlobalTransform.Origin;
	}

	public override void _Process(double delta)
	{
		// rotation
		Rotate(axis.Normalized(), radial_velocity * (float)delta);

		// bobbing
		float bob_offset = Mathf.Sin((float)Time.GetTicksMsec() / 1000 * bob_frequency) * bob_amplitude;
		Vector3 new_position = initial_position + new Vector3(0, bob_offset, 0);

		GlobalTransform = new Transform3D(GlobalTransform.Basis, new_position);

		// circling
		if (circular_radius != 0)
		{
			_circular_angle -= (float)Math.Tau * circular_velocity * (float)delta;
			Position = new Vector3(
				(float)Math.Cos(_circular_angle) * circular_radius,
				
				Position.Y,
				(float)Math.Sin(_circular_angle) * circular_radius
				);
		}
	}
}
