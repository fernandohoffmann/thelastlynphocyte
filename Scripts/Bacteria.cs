// Bacteria.cs — na fase 2 fica roxa e pode se dividir ao encostar no jogador.
using Godot;

public partial class Bacteria : Enemy
{
	private AnimatedSprite2D _animSprite;

	private const float ROTATION_OFFSET_DEGREES = 45f;
	private const float ROTATION_SMOOTH_SPEED   = 8f;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float SPLIT_CHANCE  = 0.20f; // chance de se dividir ao dar dano
	private const float SPLIT_TIME    = 0.55f; // duracao da animacao de divisao
	private const float SPLIT_STRETCH = 0.55f; // o quanto estica antes de partir
	private const float SPLIT_PUSH    = 46f;   // distancia entre as duas filhas
	private const int   SPLIT_MAX_GEN = 2;     // limite de geracoes (evita explosao)

	private static readonly Color PURPLE = new Color(0.72f, 0.35f, 1.00f);
	// ──────────────────────────────────────────────────────────────────────

	/// Quantas vezes esta linhagem ja se dividiu.
	public int Generation = 0;

	private float   _splitTimer = 0f;
	private Vector2 _splitAxis  = Vector2.Right;
	private Vector2 _spriteBaseScale = Vector2.One;

	private static bool IsPurple => GamePhase.Current >= 2;

	public override void _Ready()
	{
		Type       = EnemyType.Bacteria;
		BaseSpeed  = 75f;
		BaseHp     = 14;
		BaseDamage = 3;
		BaseXp     = 10;
		BodyRadius = 18f;
		UseSprite  = true;

		base._Ready();

		_animSprite = BuildAnimatedSprite();
		AddChild(_animSprite);
		_spriteBaseScale = _animSprite.Scale;

		var light = new PointLight2D();
		light.Color         = IsPurple ? new Color(0.65f, 0.25f, 1f)
									   : new Color(0.20f, 1.00f, 0.30f);
		light.Energy        = 0.6f;
		light.TextureScale  = 2.0f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);

		// o SetupSceneBlend do Enemy roda deferred: a recoloracao precisa vir depois
		if (IsPurple) CallDeferred(MethodName.ApplyPurple);
	}

	private void ApplyPurple()
	{
		if (_animSprite?.Material is ShaderMaterial mat)
		{
			mat.SetShaderParameter("recolor",        PURPLE);
			mat.SetShaderParameter("recolor_amount", 0.85f);
		}
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
				frames.AddFrame("default", atlas, 0.51f);
			}
		}

		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Scale        = new Vector2(0.35f, 0.35f)
		};
		sprite.Play("default");
		return sprite;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_player != null && _animSprite != null && _splitTimer <= 0f)
		{
			Vector2 dir = _player.GlobalPosition - GlobalPosition;
			if (dir.LengthSquared() > 0.01f)
			{
				float targetAngle = dir.Angle() + Mathf.DegToRad(ROTATION_OFFSET_DEGREES);
				_animSprite.Rotation = Mathf.LerpAngle(_animSprite.Rotation, targetAngle,
													   ROTATION_SMOOTH_SPEED * (float)delta);
			}
		}

		UpdateSplit((float)delta);
	}

	// Ao encostar no jogador, sorteia a divisao.
	protected override void OnContactHit()
	{
		base.OnContactHit();

		if (!IsPurple) return;                       // so na fase 2
		if (Generation >= SPLIT_MAX_GEN) return;     // corta a arvore genealogica
		if (_splitTimer > 0f) return;
		if (GD.Randf() >= SPLIT_CHANCE) return;

		StartSplit();
	}

	private void StartSplit()
	{
		_splitTimer = SPLIT_TIME;

		// eixo da divisao: perpendicular ao jogador, pra ela abrir "de lado"
		Vector2 toPlayer = _player != null
			? (_player.GlobalPosition - GlobalPosition).Normalized()
			: Vector2.Right;

		_splitAxis = new Vector2(-toPlayer.Y, toPlayer.X);
	}

	// Estica num eixo e afina no outro: a celula vira um "8" antes de partir.
	private void UpdateSplit(float delta)
	{
		if (_splitTimer <= 0f) return;

		_splitTimer -= delta;
		float t = 1f - Mathf.Clamp(_splitTimer / SPLIT_TIME, 0f, 1f);  // 0 -> 1

		if (_animSprite != null)
		{
			float stretch = Mathf.Sin(t * Mathf.Pi) * SPLIT_STRETCH;
			_animSprite.Scale = new Vector2(
				_spriteBaseScale.X * (1f + stretch),
				_spriteBaseScale.Y * (1f - stretch * 0.45f));
		}

		if (_splitTimer <= 0f) FinishSplit();
	}

	private void FinishSplit()
	{
		if (_animSprite != null) _animSprite.Scale = _spriteBaseScale;

		var child = new Bacteria { Generation = Generation + 1 };
		GetTree().CurrentScene.AddChild(child);
		child.GlobalPosition = GlobalPosition + _splitAxis * SPLIT_PUSH;
		child.ApplyDifficultyScale(GameManager.Instance?.DifficultyMultiplier ?? 1f);

		// a mae se afasta pro outro lado, pras duas nao nascerem sobrepostas
		GlobalPosition -= _splitAxis * SPLIT_PUSH * 0.5f;
		Generation++;

		AudioManager.Instance?.PlayEnemyHit();
	}
}
