// WeaponTentaculo.cs — virou um chicote: recolhe, estala no inimigo e volta.
// Cor do linfocito (azul/roxo), com brilho e uma luz na ponta.
using Godot;
using System.Collections.Generic;

// ── Uma chicotada ──
public partial class WhipLash : Node2D
{
	public Vector2 Direction = Vector2.Right;
	public float   Length    = 100f;
	public Enemy   Target;              // se vivo, a ponta persegue ele
	public float   Delay     = 0f;      // pra escalonar varias chicotadas

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	public const  float DURATION   = 0.34f; // duracao total do golpe
	private const float WINDUP_END = 0.30f; // fim do recuo (fracao da duracao)
	private const float STRIKE_END = 0.58f; // fim do estalo
	private const float CURL       = 0.42f; // o quanto o chicote encurva
	private const float BASE_WIDTH = 7.0f;  // grossura na base
	private const int   SEGMENTS   = 22;    // resolucao da curva

	private static readonly Color GLOW  = new Color(0.35f, 0.55f, 1.00f);
	private static readonly Color CORE  = new Color(0.85f, 0.92f, 1.00f);
	private static readonly Color TIP   = new Color(0.70f, 0.60f, 1.00f);
	// ──────────────────────────────────────────────────────────────────────

	private float        _age   = 0f;
	private bool         _dealt = false;
	private PointLight2D _light;

	public override void _Ready()
	{
		ZIndex = 2;

		_light = new PointLight2D
		{
			Color         = TIP,
			Energy        = 0f,
			TextureScale  = 0.8f,
			BlendMode     = Light2D.BlendModeEnum.Add,
			Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres"),
			ShadowEnabled = false
		};
		AddChild(_light);
	}

	public override void _Process(double delta)
	{
		if (Delay > 0f)
		{
			Delay -= (float)delta;
			QueueRedraw();
			return;
		}

		_age += (float)delta;

		// a ponta acompanha o alvo enquanto ele estiver vivo
		if (IsInstanceValid(Target))
		{
			Vector2 to = Target.GlobalPosition - GlobalPosition;
			if (to.LengthSquared() > 1f)
			{
				Direction = to.Normalized();
				Length    = Mathf.Min(to.Length(), Length);
			}
		}

		float t = _age / DURATION;

		// dano no momento do estalo
		if (!_dealt && t >= WINDUP_END)
		{
			_dealt = true;
			AudioManager.Instance?.PlayShoot();
		}

		_light.Position = TipPosition(t);
		_light.Energy   = (t >= WINDUP_END && t <= STRIKE_END) ? 1.3f : 0f;

		QueueRedraw();

		if (_age >= DURATION) QueueFree();
	}

	// Perfil do golpe: recua, estala pra frente passando do alvo, e recolhe.
	private float Reach(float t)
	{
		if (t < WINDUP_END)
		{
			// recolhe encolhendo
			float u = t / WINDUP_END;
			return Mathf.Lerp(0.25f, 0.10f, u);
		}
		if (t < STRIKE_END)
		{
			// estala: ease-out forte, com um leve overshoot
			float u = (t - WINDUP_END) / (STRIKE_END - WINDUP_END);
			float e = 1f - Mathf.Pow(1f - u, 3f);
			return Mathf.Lerp(0.10f, 1.06f, e);
		}
		// recolhe
		float v = (t - STRIKE_END) / (1f - STRIKE_END);
		return Mathf.Lerp(1.06f, 0.15f, v * v);
	}

	// O quanto a curva desvia pro lado (o "S" do chicote).
	private float Curl(float t)
	{
		if (t < WINDUP_END)
		{
			float u = t / WINDUP_END;
			return -CURL * u;                       // enrola pra tras
		}
		if (t < STRIKE_END)
		{
			float u = (t - WINDUP_END) / (STRIKE_END - WINDUP_END);
			return Mathf.Lerp(-CURL, CURL * 0.35f, u); // desenrola passando pelo centro
		}
		float v = (t - STRIKE_END) / (1f - STRIKE_END);
		return Mathf.Lerp(CURL * 0.35f, -CURL * 0.5f, v);
	}

	private Vector2 PointAt(float t, float s)
	{
		Vector2 dir  = Direction;
		Vector2 perp = new Vector2(-dir.Y, dir.X);

		float reach = Reach(t) * Length;
		float curl  = Curl(t)  * Length;

		// bezier quadratica: base -> controle lateral -> ponta
		Vector2 p0 = Vector2.Zero;
		Vector2 p2 = dir * reach;
		Vector2 p1 = dir * reach * 0.45f + perp * curl;

		float u = 1f - s;
		return u * u * p0 + 2f * u * s * p1 + s * s * p2;
	}

	private Vector2 TipPosition(float t) => PointAt(t, 1f);

	public override void _Draw()
	{
		if (Delay > 0f) return;

		float t = Mathf.Clamp(_age / DURATION, 0f, 1f);

		// some no fim
		float alpha = t < STRIKE_END ? 1f : 1f - (t - STRIKE_END) / (1f - STRIKE_END);
		if (alpha <= 0f) return;

		var pts = new Vector2[SEGMENTS + 1];
		for (int i = 0; i <= SEGMENTS; i++)
			pts[i] = PointAt(t, (float)i / SEGMENTS);

		for (int i = 0; i < SEGMENTS; i++)
		{
			float s = (float)i / SEGMENTS;
			// afina da base pra ponta
			float w = BASE_WIDTH * (1f - s * 0.85f);

			DrawLine(pts[i], pts[i + 1],
				new Color(GLOW.R, GLOW.G, GLOW.B, 0.20f * alpha), w * 2.4f);
			DrawLine(pts[i], pts[i + 1],
				new Color(GLOW.R, GLOW.G, GLOW.B, 0.75f * alpha), w);
			DrawLine(pts[i], pts[i + 1],
				new Color(CORE.R, CORE.G, CORE.B, 0.85f * alpha), w * 0.35f);
		}

		// estalo na ponta
		if (t >= WINDUP_END && t <= STRIKE_END)
		{
			float u    = (t - WINDUP_END) / (STRIKE_END - WINDUP_END);
			float snap = Mathf.Sin(u * Mathf.Pi);
			Vector2 tip = pts[SEGMENTS];

			DrawCircle(tip, 14f * snap, new Color(TIP.R, TIP.G, TIP.B, 0.30f * snap));
			DrawCircle(tip, 5f  * snap, new Color(CORE.R, CORE.G, CORE.B, snap));
			DrawArc(tip, 16f * snap, 0f, Mathf.Tau, 18,
				new Color(CORE.R, CORE.G, CORE.B, 0.7f * snap), 1.5f);
		}
	}
}

public partial class WeaponTentaculo : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage   = 12;
	private float _range    = 100f;
	private float _cooldown = 1.2f;
	private float _timer    = 0f;
	private int   _count    = 1;

	private const float STAGGER = 0.07f; // atraso entre uma chicotada e outra

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage += 5;
		_range  += 20f;
		_count++;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_timer >= cd) { _timer = 0f; Attack(); }
	}

	private void Attack()
	{
		var sorted = new List<(float d, Enemy e)>();
		foreach (Enemy e in Enemy.All)
			if (IsInstanceValid(e))
			{
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d <= _range) sorted.Add((d, e));
			}

		sorted.Sort((a, b) => a.d.CompareTo(b.d));

		int hits = Mathf.Min(_count, sorted.Count);
		for (int i = 0; i < hits; i++)
		{
			var e = sorted[i].e;
			e.TakeDamage(_damage, EnemyType.None);

			var lash = new WhipLash
			{
				Direction = (e.GlobalPosition - GlobalPosition).Normalized(),
				Length    = _range,
				Target    = e,
				Delay     = i * STAGGER
			};
			// filho da arma = acompanha o player enquanto chicoteia
			AddChild(lash);
		}
	}

	public override void _Draw()
	{
		DrawArc(Vector2.Zero, _range, 0f, Mathf.Tau, 48,
			new Color(0.45f, 0.6f, 1f, 0.10f), 1f);
	}
}
