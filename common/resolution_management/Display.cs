using Godot;

public partial class Display : Control
{
	[Export] public SubViewport Viewport { get; set; }
	[Export] public bool PixelMovement { get; set; } = true;
	[Export] public bool SubPixelMovementAtIntegerScale { get; set; } = true;

	private Sprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _Process(double delta)
	{
		Vector2 screen_size = GetWindow().Size;

		// viewport without padding
		Vector2 game_size = Viewport.Size - new Vector2(2, 2);

		Vector2 display_scale = screen_size / game_size;

		// maintain aspect ratio
		float display_scale_min = Mathf.Min(display_scale.X, display_scale.Y);

		_sprite.Scale = new Vector2(display_scale_min, display_scale_min);

		// scale and center control node
		Size = (_sprite.Scale * game_size).Round();
		Position = ((screen_size - Size) /2).Round();

		// smoothing
		if (PixelMovement)
		{
			TexelSnap camera = Viewport.GetCamera3D() as TexelSnap;

			if (camera != null)
			{
				Vector2 pixel_error = camera.TexelError * _sprite.Scale;
				_sprite.Position = -_sprite.Scale + pixel_error;

				bool is_integer_scale = display_scale == display_scale.Floor();
				if (is_integer_scale)
				{
					_sprite.Position = _sprite.Position.Round();
				}
			}
		}
	}
}
