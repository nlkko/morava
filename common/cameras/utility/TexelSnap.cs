using Godot;
using System.Collections.Generic;

public partial class TexelSnap : Camera3D
{
	[Export]
	public bool snap = true;

	[Export]
	public bool snap_objects = true;

	private float _texel_size = 0.0f;
	public Vector2 TexelError { get; private set; } = Vector2.Zero;

	Vector3 _previous_rotation;
	Transform3D _snap_space;

	private List<Node> _snap_nodes;
	private Vector3[] _pre_snapped_positions;

	public override void _Ready()
	{
		_previous_rotation = GlobalRotation;
		_snap_space = GlobalTransform;


		RenderingServer.FramePostDraw += _SnapObjectsRevert;
	}

	public override void _Process(double delta)
	{

		// rotation changes snap space
		if (GlobalRotation != _previous_rotation)
		{
			_previous_rotation = GlobalRotation;
			_snap_space = GlobalTransform;
		}

		_texel_size = Size / GetViewport().GetVisibleRect().Size.Y;

		// camera position at snap space
		Vector3 snap_space_position = GlobalPosition * _snap_space;

		// snap!
		Vector3 snapped_snap_space_position = snap_space_position.Snapped(Vector3.One * _texel_size);

		// how much camera snapped in snap space
		Vector3 snap_error = snapped_snap_space_position - snap_space_position;

		if (snap)
		{
			HOffset = snap_error.X;
			VOffset = snap_error.Y;

			TexelError = new Vector2(snap_error.X, snap_error.Y);
			if (snap_objects)
			{
				_SnapObjects();
			}
		}
		else
		{
			TexelError = Vector2.Zero;
		}

	}

	private void _SnapObjects()
	{
		_snap_nodes = new List<Node>(GetTree().GetNodesInGroup("snap"));
		_pre_snapped_positions = new Vector3[_snap_nodes.Count];

		for (int i = 0; i < _snap_nodes.Count; i++)
		{
			Node3D node = _snap_nodes[i] as Node3D;
			Vector3 position = node.GlobalPosition;
			_pre_snapped_positions[i] = position;
			Vector3 snap_space_position = position * _snap_space;
			Vector3 snapped_snap_space = snap_space_position.Snapped(new Vector3(_texel_size, _texel_size, 0.0f));
			node.GlobalPosition = _snap_space * snapped_snap_space;
		}

	}

	private void _SnapObjectsRevert ()
	{
		for (int i = 0; i < _snap_nodes.Count; i++)
		{
			(_snap_nodes[i] as Node3D).GlobalPosition = _pre_snapped_positions[i];
		}
		_snap_nodes.Clear();
	}
}
