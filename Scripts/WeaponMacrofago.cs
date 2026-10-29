// WeaponMacrofago.cs — a classe ExplosionEffect foi movida para ExplosionEffect.cs.
// macrofago agora cresce a cada inimigo engolido, se deforma
// como o linfocito (mesmo shader) e faz uma investida ao comer. Antes de estourar
// ele incha por um instante.
using Godot;
using System.Collections.Generic;

public partial class MacrofagoAlly : CharacterBody2D
{
	public int   SwallowDamage   = 30;
	public int   ExplosionDamage = 25;
	public float ExplosionRadius = 100f;
	public int   MaxSwallowed    = 3;
	public float Speed           = 60f;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float BASE_SPRITE_SCALE = 0.12f;
	private const float BASE_RADIUS       = 24f;  // colisao do corpo
	private const float SWALLOW_RADIUS    = 30f;  // area que engole
	private const float GROW_PER_SWALLOW  = 0.22f;// +22% de tamanho por inimigo comido
	private const float GROW_SMOOTH       = 6f;   // suavidade do crescimento

	private const float BITE_TIME    = 0.35f; // duracao da animacao de comer
	private const float BITE_STRETCH = 0.38f; // o quanto estica na direcao da presa
	private const float BITE_LUNGE   = 12f;   // avanco em pixels
	private const float BITE_WOBBLE  = 0.07f; // pico extra de tremor na membrana

	private const float POP_TIME  = 0.22f; // inchada antes de explodir
	private const float POP_SCALE = 0.55f; // +55% de tamanho no pico
	// ──────────────────────────────────────────────────────────────────────

	private int         _swallowed   = 0;
	private Enemy       _target      = null;
	private float       _searchTimer = 0f;
	private float       _age         = 0f;
	private const float MAX_AGE      = 30f;
	private float       _glowPulse   = 0f;

	private Node2D         _deform;      // aplica crescimento e deformacao direcional
	private Sprite2D       _sprite;
	private Sprite2D       _shadow;
	private Vector2        _shadowBaseScale = Vector2.One;
	private ShaderMaterial _blendMat;

	private CircleShape2D _bodyShape;
	private CircleShape2D _swallowShape;

	private float   _curSize   = 1f;   // tamanho atual (interpolado)
	private float   _biteTimer = 0f;
	private Vector2 _biteDir   = Vector2.Right;
	private bool    _exploding = false;
	private float   _popTimer  = 0f;

	public static int Count { get; private set; } = 0;

	public override void _Ready()
	{
		AddToGroup("Ally");
		Count++;

		CollisionLayer = 1;
		CollisionMask  = 10;

		_bodyShape = new CircleShape2D { Radius = BASE_RADIUS };
		var col = new CollisionShape2D { Shape = _bodyShape };
		AddChild(col);

		// sombra de contato (fica por baixo de tudo)
		_shadow          = SceneBlend.MakeContactShadow(BASE_RADIUS, 0.40f);
		_shadowBaseScale = _shadow.Scale;
		AddChild(_shadow);

		// no de deformacao: e ele que cresce e estica, o sprite fica "limpo" dentro
		_deform = new Node2D { Name = "Deform" };
		AddChild(_deform);

		_sprite = new Sprite2D();
		_sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/macrofago.png");
		// o shader encolhe a imagem pra ter margem de deformacao — compensa aqui
		_sprite.Scale = new Vector2(BASE_SPRITE_SCALE, BASE_SPRITE_SCALE)
					  * (1f + SceneBlend.PLAYER_PAD);
		_deform.AddChild(_sprite);

		// mesmo shader do linfocito: wobble de membrana + luz ambiente do tecido
		_blendMat = SceneBlend.MakePlayerMaterial(_sprite.Scale.X);
		SceneBlend.ApplyRegionUniforms(_blendMat, _sprite.Texture);
		_sprite.Material = _blendMat;

		var swallowArea = new Area2D { Name = "SwallowArea" };
		swallowArea.CollisionLayer = 0;
		swallowArea.CollisionMask  = 2;
		swallowArea.Monitoring     = true;
		swallowArea.Monitorable    = false;

		_swallowShape = new CircleShape2D { Radius = SWALLOW_RADIUS };
		swallowArea.AddChild(new CollisionShape2D { Shape = _swallowShape });
		swallowArea.BodyEntered += OnBodyEntered;
		AddChild(swallowArea);
	}

	public override void _PhysicsProcess(double delta)
	{
		float d = (float)delta;

		// ── Estourando: para tudo, incha e explode ───────────────
		if (_exploding)
		{
			_popTimer += d;
			UpdateDeform(d);
			if (_popTimer >= POP_TIME) DoExplode();
			return;
		}

		_age += d;
		if (_age >= MAX_AGE) { QueueFree(); return; }

		_searchTimer -= d;
		if (_searchTimer <= 0f || !IsInstanceValid(_target))
		{
			_searchTimer = 0.5f;
			FindTarget();
		}

		if (IsInstanceValid(_target))
		{
			Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
			Velocity    = dir * Speed;

			if (dir.X != 0)
				_sprite.FlipH = dir.X < 0;
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		MoveAndSlide();

		UpdateDeform(d);

		// os pontos orbitais so existem depois de engolir algo
		if (_swallowed > 0)
		{
			_glowPulse += d * 3f;
			QueueRedraw();
		}
	}

	// Crescimento + investida ao comer + inchada antes de estourar.
	private void UpdateDeform(float delta)
	{
		// tamanho alvo cresce a cada engolido
		float targetSize = 1f + GROW_PER_SWALLOW * _swallowed;
		_curSize = Mathf.Lerp(_curSize, targetSize, GROW_SMOOTH * delta);

		// ── investida ────────────────────────────────────────────
		float stretch = 0f;
		float lunge   = 0f;

		if (_biteTimer > 0f)
		{
			_biteTimer -= delta;
			float t = 1f - Mathf.Clamp(_biteTimer / BITE_TIME, 0f, 1f); // 0 -> 1

			// seno completo: estica pra frente, depois recua achatado
			float s = Mathf.Sin(t * Mathf.Tau);
			stretch = BITE_STRETCH * s;
			lunge   = BITE_LUNGE * Mathf.Sin(t * Mathf.Pi);

			// a membrana treme mais forte durante a mordida
			_blendMat?.SetShaderParameter("wobble_amount",
				SceneBlend.WOBBLE_AMOUNT + BITE_WOBBLE * Mathf.Abs(s));
		}
		else
		{
			_blendMat?.SetShaderParameter("wobble_amount", SceneBlend.WOBBLE_AMOUNT);
		}

		// ── inchada do estouro ───────────────────────────────────
		float pop = 1f;
		if (_exploding)
		{
			float t = Mathf.Clamp(_popTimer / POP_TIME, 0f, 1f);
			pop = 1f + POP_SCALE * t * t;
			_blendMat?.SetShaderParameter("wobble_amount",
				SceneBlend.WOBBLE_AMOUNT + BITE_WOBBLE * 2f * t);
		}

		float size = _curSize * pop;

		// R(a) * S(sx,sy) * R(-a) = estica no eixo da presa mantendo o sprite em pe
		float angle = _biteDir.Angle();
		_deform.Rotation = angle;
		_deform.Scale    = new Vector2(size * (1f + stretch), size * (1f - stretch * 0.7f));
		_deform.Position = _biteDir * lunge;
		_sprite.Rotation = -angle;

		// colisoes e sombra acompanham o tamanho
		if (_bodyShape    != null) _bodyShape.Radius    = BASE_RADIUS    * _curSize;
		if (_swallowShape != null) _swallowShape.Radius = SWALLOW_RADIUS * _curSize;
		if (_shadow       != null) _shadow.Scale        = _shadowBaseScale * _curSize;
	}

	private void FindTarget()
	{
		_target = Enemy.FindClosest(GlobalPosition);
	}

	private void OnBodyEntered(Node body)
	{
		if (_exploding) return;

		if (body is Enemy e && IsInstanceValid(e))
		{
			// dispara a animacao de comer na direcao da presa
			Vector2 dir = e.GlobalPosition - GlobalPosition;
			if (dir.LengthSquared() > 0.01f) _biteDir = dir.Normalized();
			_biteTimer = BITE_TIME;

			e.TakeDamage(SwallowDamage, EnemyType.None);
			_swallowed++;
			QueueRedraw();

			if (_swallowed >= MaxSwallowed)
				StartExplode();
		}
	}

	private void StartExplode()
	{
		_exploding = true;
		_popTimer  = 0f;
		Velocity   = Vector2.Zero;
	}

	public override void _ExitTree()
	{
		Count--;
	}

	private void DoExplode()
	{
		// o raio acompanha o tamanho que o macrofago alcancou
		float radius = ExplosionRadius * _curSize;

		foreach (Enemy e in Enemy.All)
		{
			if (IsInstanceValid(e) && GlobalPosition.DistanceTo(e.GlobalPosition) <= radius)
				e.TakeDamage(ExplosionDamage, EnemyType.None);
		}

		var exp = new ExplosionEffect();
		exp.Radius = radius;
		GetTree().CurrentScene.AddChild(exp);
		exp.GlobalPosition = GlobalPosition;

		QueueFree();
	}

	public override void _Draw()
	{
		float pulse = (Mathf.Sin(_glowPulse) + 1f) * 0.5f;

		for (int i = 0; i < _swallowed; i++)
		{
			float   angle = (Mathf.Tau / MaxSwallowed) * i + _age * 0.5f;
			Vector2 pos   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f * _curSize;
			float   r     = 4.5f * _curSize;

			DrawCircle(pos, (7f + pulse * 2f) * _curSize, new Color(1f, 0.85f, 0.3f, 0.3f + pulse * 0.2f));
			DrawCircle(pos, r, new Color(1f, 0.95f, 0.6f, 1f));
			DrawArc(pos, r, 0f, Mathf.Tau, 12, new Color(1f, 1f, 0.8f, 0.7f), 1.2f);
		}
	}
}

public partial class WeaponMacrofago : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _swallowDamage   = 30;
	private int   _explosionDamage = 25;
	private float _explosionRadius = 100f;
	private float _allySpeed       = 60f;
	private float _cooldown        = 8f;
	private float _timer           = 0f;
	private int   _allyCount       = 0;
	private const int MAX_ALLIES   = 2;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_swallowDamage   += 10;
		_explosionDamage += 8;
		_explosionRadius += 15f;
		_allySpeed       += 8f;
		_cooldown         = Mathf.Max(4f, _cooldown - 0.8f);
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		_allyCount = MacrofagoAlly.Count;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_timer >= cd && _allyCount < MAX_ALLIES)
		{
			_timer = 0f;
			SpawnAlly();
		}
	}

	private void SpawnAlly()
	{
		var ally = new MacrofagoAlly
		{
			SwallowDamage   = _swallowDamage,
			ExplosionDamage = _explosionDamage,
			ExplosionRadius = _explosionRadius,
			Speed           = _allySpeed
		};
		GetTree().CurrentScene.AddChild(ally);
		ally.GlobalPosition = GlobalPosition + new Vector2(
			(float)GD.RandRange(-40, 40),
			(float)GD.RandRange(-40, 40)
		);
		AudioManager.Instance?.PlayEnemyHit();
	}
}
