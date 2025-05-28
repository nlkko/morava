using Godot;

public partial class Player : CharacterBody3D
{

	[Export]
    public int InputVelocity { get; set; } = 5;
    // The downward acceleration when in the air, in meters per second squared.
    [Export]
    public int FallAcceleration { get; set; } = 75;

    private Vector3 _target_velocity = Vector3.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

    public override void _Process(double delta)
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
public override void _PhysicsProcess(double delta)
    {
        var direction = Vector3.Zero;

        if (Input.IsActionPressed("move_right"))
        {
            direction.X += 1.0f;
        }
        if (Input.IsActionPressed("move_left"))
        {
            direction.X -= 1.0f;
        }
        if (Input.IsActionPressed("move_backward"))
        {
            direction.Z += 1.0f;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            direction.Z -= 1.0f;
        }

        if (direction != Vector3.Zero)
        {
            direction = direction.Normalized();
            // Setting the basis property will affect the rotation of the node.
            GetNode<Node3D>("Pivot").Basis = Basis.LookingAt(direction);
        }

        // Ground velocity
        _target_velocity.X = direction.X * InputVelocity;
        _target_velocity.Z = direction.Z * InputVelocity;

        // Vertical velocity
        if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
        {
            _target_velocity.Y -= FallAcceleration * (float)delta;
        }

        // Moving the character
        Velocity = _target_velocity;
        MoveAndSlide();
    }
}
