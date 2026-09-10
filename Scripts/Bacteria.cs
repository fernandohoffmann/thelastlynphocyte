// Bacteria.cs — completo, escala maior e duração de frame igual ao bacteria.tscn
using Godot;

public partial class Bacteria : Enemy
{
	private AnimatedSprite2D _animSprite;
	private Player           _player;

	private const float ROTATION_OFFSET_DEGREES = 45f;
	private const float ROTATION_SMOOTH_SPEED   = 8f;

	public override void _Ready()
	{
		Type       = EnemyType.Bacteria;
		BaseSpeed  = 75f;
		BaseHp     = 8;
		BaseDamage = 2;
		BaseXp     = 10;
		BodyRadius = 18f;
		UseSprite  = true;

		base._Ready();

		_animSprite = BuildAnimatedSprite();
		AddChild(_animSprite);

		var light = new PointLight2D();
		light.Color         = new Color(0.2f, 1f, 0.3f);
		light.Energy        = 0.6f;
		light.TextureScale  = 2.0f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);

		_player = GetTree().GetFirstNodeInGroup("Player") as Player;
	}

	private AnimatedSprite2D BuildAnimatedSprite()
	{
		var tex = GD.Load<Texture2D>("res://Assets/Sprites/bacteriasa.png");

		var frames = new SpriteFrames();
		frames.AddAnimation("default");
		frames.SetAnimationLoop("default", true);
		frames.SetAnimationSpeed("default", 5.0);

		const int frameSize = 256;
		const int cols = 4;
		const int rows = 4;

		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < cols; x++)
			{
				var atlas = new AtlasTexture
				{
					Atlas  = tex,
					Region = new Rect2(x * frameSize, y * frameSize, frameSize, frameSize)
				};
				// duração 0.51 por frame, igual ao que você configurou no bacteria.tscn
				frames.AddFrame("default", atlas, 0.51f);
			}
		}

		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			// Aumentado — se ainda estiver pequena/grande demais, ajuste aqui
			Scale = new Vector2(0.35f, 0.35f)
		};
		sprite.Play("default");
		return sprite;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_player == null)
			_player = GetTree().GetFirstNodeInGroup("Player") as Player;

		if (_player != null && _animSprite != null)
		{
			Vector2 dir = (_player.GlobalPosition - GlobalPosition);
			if (dir.LengthSquared() > 0.01f)
			{
				float targetAngle = dir.Angle() + Mathf.DegToRad(ROTATION_OFFSET_DEGREES);
				_animSprite.Rotation = Mathf.LerpAngle(_animSprite.Rotation, targetAngle, ROTATION_SMOOTH_SPEED * (float)delta);
			}
		}
	}
}
