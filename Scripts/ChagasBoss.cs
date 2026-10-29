using Godot;

// ── Projetil cuspido pela Doenca de Chagas ──
public partial class ChagasProjectile : Area2D
{
	public int     Damage    = 12;
	public float   Speed     = 340f;
	public Vector2 Direction = Vector2.Right;

	private const float LIFETIME = 5f;
	private const float RADIUS   = 11f;

	private float _age   = 0f;
	private float _pulse = 0f;

	public override void _Ready()
	{
		ZIndex         = 3;
		CollisionLayer = 0;
		CollisionMask  = 1;   // so o player
		Monitoring     = true;
		Monitorable    = false;

		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = RADIUS } });
		BodyEntered += OnBodyEntered;

		var light = new PointLight2D
		{
			Color         = new Color(0.75f, 0.25f, 0.95f),
			Energy        = 0.9f,
			TextureScale  = 0.6f,
			BlendMode     = Light2D.BlendModeEnum.Add,
			Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres"),
			ShadowEnabled = false
		};
		AddChild(light);
	}

	public override void _PhysicsProcess(double delta)
	{
		float d = (float)delta;

		_age   += d;
		_pulse += d * 9f;
		if (_age >= LIFETIME) { QueueFree(); return; }

		Vector2 next = GlobalPosition + Direction * Speed * d;

		// some ao bater na parede de veia (layer 8)
		var space = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, next, 8);
		if (space.IntersectRay(query).Count > 0) { QueueFree(); return; }

		GlobalPosition = next;
		QueueRedraw();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			p.TakeDamage(Damage);
			QueueFree();
		}
	}

	public override void _Draw()
	{
		float pulse = 0.85f + 0.15f * Mathf.Sin(_pulse);

		DrawCircle(Vector2.Zero, RADIUS * 2.1f * pulse, new Color(0.65f, 0.15f, 0.85f, 0.25f));
		DrawCircle(Vector2.Zero, RADIUS * pulse,        new Color(0.55f, 0.10f, 0.70f, 0.95f));
		DrawCircle(Vector2.Zero, RADIUS * 0.45f,        new Color(0.95f, 0.75f, 1.00f, 1f));
	}
}

public partial class ChagasBoss : Enemy
{
	public static ChagasBoss Instance { get; private set; }

	private const float FEVER_DURATION    = 5f;   // segundos de febre
	private const int   FEVER_DAMAGE_TICK = 2;    // dano por segundo enquanto febril

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const string SPRITE_PATH  = "res://Assets/Sprites/chagas.png";
	private const int    SHEET_COLS   = 3;
	private const int    SHEET_ROWS   = 3;
	private const float  SPRITE_SCALE = 0.95f;  // era 0.55
	private const float  BODY_RADIUS  = 120f;   // era 70

	// Raio do CORPO FISICO, separado do BODY_RADIUS. Com 120 de colisao ele nao
	// passa nos corredores e fica raspando na parede sem conseguir avancar.
	private const float  PHYS_RADIUS  = 68f;

	private const bool   DEBUG_CHASE  = true;  // imprime o estado da perseguicao

	// A folha ja esta na ordem da mordida: quadro 0 fechado, 4 escancarado,
	// 8 fechando de novo. Uma passada direta de 0 a 8 e a mordida inteira.
	private const int   FRAME_CLOSED = 0;
	private const float BITE_TIME    = 0.45f;

	private const float TURN_SPEED = 8f; // suavidade com que a cabeca acompanha

	// ── Ataque a distancia ──
	private const float SHOT_COOLDOWN = 3.0f;  // segundos entre cuspidas
	private const float SHOT_RANGE    = 950f;  // alcance do ataque
	private const int   SHOT_DAMAGE   = 12;
	private const float SHOT_SPEED    = 340f;
	private const float MUZZLE_DIST   = 1.15f; // multiplo do BodyRadius: ponta da boca
	// ──────────────────────────────────────────────────────────────────────

	private Sprite2D _sprite;
	private float    _biteTimer = 0f;

	private float   _shotTimer  = 0f;
	private float   _shotDelay  = -1f;      // < 0 = nenhum tiro engatilhado
	private float   _debugTimer = 0f;
	private Vector2 _shotDir    = Vector2.Right;

	public override void _Ready()
	{
		Type       = EnemyType.Virus;
		BaseSpeed  = 120f;  // era 48: o player anda a 220, nunca era alcancado
		BaseHp     = 2600;
		BaseDamage = 22;
		BaseXp     = 800;
		BodyRadius = BODY_RADIUS;
		BodyColor  = new Color(0.35f, 0.05f, 0.45f);
		UseSprite  = true;

		base._Ready();
		Instance = this;

		ShrinkPhysicsBody();
		BuildSprite();

		var light = new PointLight2D();
		light.Color         = new Color(0.6f, 0.1f, 0.6f);
		light.Energy        = 1.0f;
		light.TextureScale  = 5f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);
	}

	// O Enemy cria a colisao a partir do BodyRadius. Aqui ela e reduzida, porque
	// o BodyRadius grande ainda serve pro alcance de contato e pra boca.
	private void ShrinkPhysicsBody()
	{
		foreach (var child in GetChildren())
		{
			if (child is CollisionShape2D cs && cs.Shape is CircleShape2D circle)
			{
				circle.Radius = PHYS_RADIUS;
				return;
			}
		}
	}

	private void BuildSprite()
	{
		var tex = GD.Load<Texture2D>(SPRITE_PATH);
		if (tex == null)
		{
			GD.PrintErr($"[ChagasBoss] sprite nao encontrado: {SPRITE_PATH}");
			UseSprite = false; // volta pro circulo roxo de placeholder
			return;
		}

		_sprite = new Sprite2D
		{
			Name    = "ChagasSprite",
			Texture = tex,
			Hframes = SHEET_COLS,
			Vframes = SHEET_ROWS,
			Frame   = FRAME_CLOSED,
			Scale   = new Vector2(SPRITE_SCALE, SPRITE_SCALE)
		};
		AddChild(_sprite);
		// o SetupSceneBlend do Enemy roda deferred e pega este sprite sozinho
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_sprite == null || _player == null) return;

		float d = (float)delta;

		FacePlayer(d);
		UpdateBite(d);
		UpdateRangedAttack(d);

		if (DEBUG_CHASE)
		{
			_debugTimer -= d;
			if (_debugTimer <= 0f)
			{
				_debugTimer = 1f;
				float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
				bool  los  = VeinMap.Instance == null ||
							 VeinMap.Instance.HasLineOfSight(GlobalPosition, _player.GlobalPosition);
				Vector2 flow = VeinMap.Instance?.GetFlowDirection(GlobalPosition) ?? Vector2.Zero;
				GD.Print($"[ChagasBoss] dist={dist:F0} vel={Velocity.Length():F0} " +
						 $"visao={los} flow={flow}");
			}
		}
	}

	// A arte aponta para a ESQUERDA. Em vez de rotacionar 180 graus quando o
	// alvo esta a direita (o que deixaria o bicho de cabeca pra baixo), espelha
	// na horizontal e rotaciona a partir dai.
	private void FacePlayer(float delta)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.LengthSquared() < 1f) return;

		float angle = toPlayer.Angle();
		bool  right = Mathf.Cos(angle) >= 0f;

		_sprite.FlipH = right;

		float target = right ? angle : angle - Mathf.Pi;
		_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, target, TURN_SPEED * delta);
	}

	private void UpdateBite(float delta)
	{
		if (_biteTimer <= 0f)
		{
			_sprite.Frame = FRAME_CLOSED;
			return;
		}

		_biteTimer -= delta;

		int   last = SHEET_COLS * SHEET_ROWS - 1;          // 8
		float t    = 1f - Mathf.Clamp(_biteTimer / BITE_TIME, 0f, 1f);

		_sprite.Frame = Mathf.Clamp(Mathf.RoundToInt(t * last), 0, last);
	}

	// Cospe em linha reta. A boca abre primeiro e o projetil sai no quadro em
	// que ela esta escancarada, nao no instante em que o ataque e decidido.
	private void UpdateRangedAttack(float delta)
	{
		// tiro ja engatilhado esperando a boca abrir
		if (_shotDelay >= 0f)
		{
			_shotDelay -= delta;
			if (_shotDelay <= 0f)
			{
				_shotDelay = -1f;
				FireProjectile();
			}
			return;
		}

		_shotTimer -= delta;
		if (_shotTimer > 0f) return;

		float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
		if (dist > SHOT_RANGE) return;

		// nao atira atraves de parede
		if (VeinMap.Instance != null &&
			!VeinMap.Instance.HasLineOfSight(GlobalPosition, _player.GlobalPosition))
			return;

		_shotTimer = SHOT_COOLDOWN;
		_shotDir   = (_player.GlobalPosition - GlobalPosition).Normalized();

		_biteTimer = BITE_TIME;
		// quadro 4 de 8 = metade da animacao = boca no maximo
		_shotDelay = BITE_TIME * 0.5f;
	}

	private void FireProjectile()
	{
		// a boca esta sempre voltada pro player, entao a ponta dela fica na
		// direcao dele, a um raio de corpo de distancia
		Vector2 dir = (_player != null && IsInstanceValid(_player))
			? (_player.GlobalPosition - GlobalPosition).Normalized()
			: _shotDir;

		Vector2 muzzle = GlobalPosition + dir * (BodyRadius * MUZZLE_DIST);

		var shot = new ChagasProjectile
		{
			Damage    = SHOT_DAMAGE,
			Speed     = SHOT_SPEED,
			Direction = dir
		};
		GetTree().CurrentScene.AddChild(shot);
		shot.GlobalPosition = muzzle;

		AudioManager.Instance?.PlayEnemyHit();
	}

	// Além do dano normal de contato, contamina o jogador com febre
	protected override void OnContactHit()
	{
		base.OnContactHit();
		Player.Instance?.ApplyFever(FEVER_DURATION, FEVER_DAMAGE_TICK);

		_biteTimer = BITE_TIME; // a boca so abre quando ele morde de verdade
	}

	public override void _ExitTree()
	{
		base._ExitTree(); // mantem o registro Enemy.All em dia
		if (Instance == this) Instance = null;
	}
}
