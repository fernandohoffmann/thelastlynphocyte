// Virus.cs — completo, rotação suave
using Godot;

public partial class Virus : Enemy
{
	private Sprite2D _sprite;
	private Player   _player;

	private const float ROTATION_OFFSET_DEGREES = 0f;
	private const float ROTATION_SMOOTH_SPEED   = 8f; // ← quanto maior, mais rápido vira

	public override void _Ready()
	{
		Type       = EnemyType.Virus;
		BaseSpeed  = 55f;
		BaseHp     = 25;
		BaseDamage = 5;
		BaseXp     = 35;
		BodyRadius = 22f;
		UseSprite  = true;

		base._Ready();

		_sprite = new Sprite2D();
		_sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/virus.png");
		_sprite.Scale   = new Vector2(0.8f, 0.8f);
		AddChild(_sprite);

		var light = new PointLight2D();
		light.Color         = new Color(0.6f, 0.2f, 1f);
		light.Energy        = 1.2f;
		light.TextureScale  = 3.5f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);

		_player = GetTree().GetFirstNodeInGroup("Player") as Player;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_player == null)
			_player = GetTree().GetFirstNodeInGroup("Player") as Player;

		if (_player != null && _sprite != null)
		{
			Vector2 dir = (_player.GlobalPosition - GlobalPosition);
			if (dir.LengthSquared() > 0.01f)
			{
				float targetAngle = dir.Angle() + Mathf.DegToRad(ROTATION_OFFSET_DEGREES);
				_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, targetAngle, ROTATION_SMOOTH_SPEED * (float)delta);
			}
		}
	}
}
