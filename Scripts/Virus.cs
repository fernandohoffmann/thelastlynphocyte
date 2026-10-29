// Virus.cs — completo, com pulsação leve
using Godot;

public partial class Virus : Enemy
{
	private Sprite2D _sprite;
	private Player   _player;

	private const float ROTATION_OFFSET_DEGREES = 0f;
	private const float ROTATION_SMOOTH_SPEED   = 8f;

	// Pulsação sutil — ajuste PULSE_AMOUNT pra mais/menos intensidade
	private const float PULSE_AMOUNT = 0.025f; // 2.5% de variação de tamanho
	private const float PULSE_SPEED  = 1.5f;   // ciclos por segundo (mais baixo = mais lento/suave)

	private float _pulsePhase; // fase aleatória por inimigo, pra não pulsarem todos em sincronia

	private const float BASE_SCALE = 0.08f;

	public override void _Ready()
	{
		Type       = EnemyType.Virus;
		BaseSpeed  = 55f;
		BaseHp     = 40;
		BaseDamage = 7;
		BaseXp     = 35;
		BodyRadius = 22f;
		UseSprite  = true;

		base._Ready();

		_pulsePhase = (float)GD.RandRange(0.0, Mathf.Tau);

		_sprite = new Sprite2D();
		_sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/virus.png");
		_sprite.Scale   = new Vector2(BASE_SCALE, BASE_SCALE);
		AddChild(_sprite);

		var light = new PointLight2D();
		light.Color         = new Color(0.6f, 0.2f, 1f);
		light.Energy        = 0.7f;
		light.TextureScale  = 2.2f;
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

		if (_sprite != null)
		{
			float t     = Time.GetTicksMsec() / 1000f;
			float pulse = 1f + (float)Mathf.Sin((t * PULSE_SPEED + _pulsePhase) * Mathf.Tau) * PULSE_AMOUNT;
			_sprite.Scale = new Vector2(BASE_SCALE * pulse, BASE_SCALE * pulse);
		}
	}
}
