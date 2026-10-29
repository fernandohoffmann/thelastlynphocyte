// WeaponProteinaComplemento.cs — agora o raio VIAJA de alvo em alvo em vez de
// acertar todo mundo de uma vez. Cada salto e um relampago com nucleo branco,
// brilho azul e uma luz que acompanha a ponta.
using Godot;
using System.Collections.Generic;

// ── O raio propriamente dito: um no proprio, com vida curta ──
public partial class ComplementBolt : Node2D
{
	public int   Damage     = 6;
	public int   MaxBounces = 3;
	public float ChainRange = 250f;
	public Enemy FirstTarget;
	public Node2D Origin;               // segue o player enquanto o 1o salto viaja

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float TRAVEL_SPEED  = 1500f; // px/s que a ponta percorre
	private const float PAUSE_AT_HIT  = 0.06f; // respiro entre um salto e outro
	private const float SEG_FADE      = 0.45f; // quanto cada segmento demora pra apagar
	private const float JITTER        = 13f;   // amplitude do zigue-zague
	private const float JITTER_HZ     = 22f;   // trocas de forma por segundo
	private const int   POINTS_PER_100PX = 4;  // resolucao do zigue-zague

	private static readonly Color GLOW_COLOR = new Color(0.35f, 0.70f, 1.00f);
	private static readonly Color CORE_COLOR = new Color(0.92f, 0.98f, 1.00f);
	// ──────────────────────────────────────────────────────────────────────

	private class Segment
	{
		public Vector2 From;
		public Vector2 To;
		public float   Age;
		public int     Seed;
	}

	private class Impact
	{
		public Vector2 Pos;
		public float   Age;
	}

	private readonly List<Segment> _segments = new List<Segment>();
	private readonly List<Impact>  _impacts  = new List<Impact>();
	private readonly HashSet<ulong> _hit     = new HashSet<ulong>();

	private Enemy        _current;
	private Vector2      _segFrom;
	private float        _travelled;
	private float        _pauseTimer;
	private int          _bouncesDone;
	private bool         _finished;
	private PointLight2D _light;
	private float        _shapeTimer;
	private int          _shapeSeed;

	public override void _Ready()
	{
		ZIndex = 3;

		_light = new PointLight2D
		{
			Color         = GLOW_COLOR,
			Energy        = 1.4f,
			TextureScale  = 1.1f,
			BlendMode     = Light2D.BlendModeEnum.Add,
			Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres"),
			ShadowEnabled = false
		};
		AddChild(_light);

		_segFrom   = GetOriginPos();
		_current   = FirstTarget;
		_shapeSeed = (int)(GD.Randi() % 9999);

		if (!IsInstanceValid(_current)) _finished = true;
	}

	private Vector2 GetOriginPos()
	{
		return IsInstanceValid(Origin) ? Origin.GlobalPosition : _segFrom;
	}

	public override void _Process(double delta)
	{
		float d = (float)delta;

		// troca a forma do zigue-zague algumas vezes por segundo (flicker)
		_shapeTimer += d;
		if (_shapeTimer >= 1f / JITTER_HZ)
		{
			_shapeTimer = 0f;
			_shapeSeed  = (int)(GD.Randi() % 9999);
		}

		// envelhece o rastro
		foreach (var s in _segments) s.Age += d;
		foreach (var i in _impacts)  i.Age += d;

		if (!_finished)
		{
			if (_pauseTimer > 0f)
			{
				_pauseTimer -= d;
			}
			else if (IsInstanceValid(_current))
			{
				// o primeiro salto sai do player, que pode estar se movendo
				if (_segments.Count == 0) _segFrom = GetOriginPos();

				Vector2 to    = _current.GlobalPosition;
				float   total = _segFrom.DistanceTo(to);

				_travelled += TRAVEL_SPEED * d;

				if (_travelled >= total)
					ArriveAtTarget(to);
			}
			else
			{
				// alvo morreu no meio do caminho: tenta continuar dali mesmo
				ArriveAtTarget(_segFrom + (Vector2.Right * 0.01f));
			}
		}

		// posiciona a luz na ponta
		_light.GlobalPosition = HeadPosition();
		_light.Energy = _finished
			? Mathf.Max(0f, _light.Energy - d * 4f)
			: 1.4f;

		QueueRedraw();

		if (_finished && OldestAge() > SEG_FADE) QueueFree();
	}

	private float OldestAge()
	{
		float m = 99f;
		foreach (var s in _segments) m = Mathf.Min(m, s.Age);
		return _segments.Count == 0 ? 99f : m;
	}

	private Vector2 HeadPosition()
	{
		if (_finished && _segments.Count > 0) return _segments[_segments.Count - 1].To;
		if (!IsInstanceValid(_current))       return _segFrom;

		Vector2 to    = _current.GlobalPosition;
		float   total = Mathf.Max(1f, _segFrom.DistanceTo(to));
		float   t     = Mathf.Clamp(_travelled / total, 0f, 1f);
		return _segFrom.Lerp(to, t);
	}

	private void ArriveAtTarget(Vector2 pos)
	{
		if (IsInstanceValid(_current))
		{
			pos = _current.GlobalPosition;
			_current.TakeDamage(Damage, EnemyType.None);
			_hit.Add(_current.GetInstanceId());
			AudioManager.Instance?.PlayEnemyHit();
		}

		_segments.Add(new Segment
		{
			From = _segFrom,
			To   = pos,
			Age  = 0f,
			Seed = (int)(GD.Randi() % 9999)
		});
		_impacts.Add(new Impact { Pos = pos, Age = 0f });

		_segFrom   = pos;
		_travelled = 0f;
		_bouncesDone++;

		if (_bouncesDone > MaxBounces) { _finished = true; return; }

		_current = FindNext(pos);
		if (_current == null) { _finished = true; return; }

		_pauseTimer = PAUSE_AT_HIT;
	}

	private Enemy FindNext(Vector2 from)
	{
		Enemy best = null;
		float bd   = float.MaxValue;

		foreach (Enemy e in Enemy.All)
		{
			if (IsInstanceValid(e) && !_hit.Contains(e.GetInstanceId()))
			{
				float d = from.DistanceTo(e.GlobalPosition);
				if (d < ChainRange && d < bd) { bd = d; best = e; }
			}
		}
		return best;
	}

	// Zigue-zague deterministico: mesmo seed = mesma forma no frame inteiro.
	private void BuildLightning(List<Vector2> outPts, Vector2 from, Vector2 to, int seed, float amp)
	{
		outPts.Clear();

		float len  = from.DistanceTo(to);
		int   segs = Mathf.Max(2, Mathf.RoundToInt(len / 100f * POINTS_PER_100PX));

		Vector2 dir  = (to - from).Normalized();
		Vector2 perp = new Vector2(-dir.Y, dir.X);

		outPts.Add(from);
		for (int i = 1; i < segs; i++)
		{
			float t = (float)i / segs;
			// afunila nas pontas: o raio nasce e morre colado nos alvos
			float taper = Mathf.Sin(t * Mathf.Pi);
			float rnd   = PseudoRandom(seed + i) * 2f - 1f;
			outPts.Add(from.Lerp(to, t) + perp * rnd * amp * taper);
		}
		outPts.Add(to);
	}

	private static float PseudoRandom(int n)
	{
		n = (n << 13) ^ n;
		return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
	}

	public override void _Draw()
	{
		var pts = new List<Vector2>();

		// ── segmentos ja concluidos ──────────────────────────────
		foreach (var s in _segments)
		{
			float a = 1f - Mathf.Clamp(s.Age / SEG_FADE, 0f, 1f);
			if (a <= 0f) continue;
			DrawBolt(pts, ToLocal(s.From), ToLocal(s.To), s.Seed, a);
		}

		// ── segmento em viagem (so ate onde a ponta chegou) ──────
		if (!_finished && IsInstanceValid(_current))
		{
			Vector2 head = HeadPosition();
			DrawBolt(pts, ToLocal(_segFrom), ToLocal(head), _shapeSeed, 1f);

			// faisca na ponta
			Vector2 h = ToLocal(head);
			DrawCircle(h, 9f, new Color(GLOW_COLOR.R, GLOW_COLOR.G, GLOW_COLOR.B, 0.35f));
			DrawCircle(h, 3.5f, CORE_COLOR);
		}

		// ── clarao nos pontos de impacto ─────────────────────────
		foreach (var i in _impacts)
		{
			float t = Mathf.Clamp(i.Age / 0.25f, 0f, 1f);
			if (t >= 1f) continue;

			float a = 1f - t;
			Vector2 p = ToLocal(i.Pos);
			DrawCircle(p, 6f + 16f * t, new Color(GLOW_COLOR.R, GLOW_COLOR.G, GLOW_COLOR.B, 0.30f * a));
			DrawArc(p, 6f + 20f * t, 0f, Mathf.Tau, 20,
				new Color(CORE_COLOR.R, CORE_COLOR.G, CORE_COLOR.B, a), 2f);
		}
	}

	// tres passadas: brilho largo, corpo azul e nucleo branco fino
	private void DrawBolt(List<Vector2> pts, Vector2 from, Vector2 to, int seed, float alpha)
	{
		BuildLightning(pts, from, to, seed, JITTER);

		for (int i = 0; i < pts.Count - 1; i++)
		{
			DrawLine(pts[i], pts[i + 1],
				new Color(GLOW_COLOR.R, GLOW_COLOR.G, GLOW_COLOR.B, 0.18f * alpha), 11f);
			DrawLine(pts[i], pts[i + 1],
				new Color(GLOW_COLOR.R, GLOW_COLOR.G, GLOW_COLOR.B, 0.60f * alpha), 4.5f);
			DrawLine(pts[i], pts[i + 1],
				new Color(CORE_COLOR.R, CORE_COLOR.G, CORE_COLOR.B, alpha), 1.6f);
		}
	}
}

public partial class WeaponProteinaComplemento : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage          = 6;
	private int   _bounces         = 3;
	private float _cooldown        = 2.5f;
	private float _chainRange      = 250f;
	private float _activationRange = 250f;
	private float _timer           = 0f;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage     += 3;
		_bounces    += 1;
		_chainRange += 30f;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_timer >= cd) { _timer = 0f; FireChain(); }
	}

	private void FireChain()
	{
		Enemy first = Enemy.FindClosest(GlobalPosition, _activationRange);
		if (first == null) return;

		AudioManager.Instance?.PlayShoot();

		var bolt = new ComplementBolt
		{
			Damage      = _damage,
			MaxBounces  = _bounces,
			ChainRange  = _chainRange,
			FirstTarget = first,
			Origin      = this
		};
		GetTree().CurrentScene.AddChild(bolt);
		bolt.GlobalPosition = GlobalPosition;
	}

	public override void _Draw()
	{
		// range de ativacao (circulo sutil ao redor do player)
		DrawArc(Vector2.Zero, _activationRange, 0f, Mathf.Tau, 64,
			new Color(0.4f, 0.8f, 1f, 0.08f), 1.5f);
	}
}
