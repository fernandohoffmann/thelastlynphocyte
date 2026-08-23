using Godot;
using System.Collections.Generic;

public partial class MacrofagoAlly : CharacterBody2D
{
	public int   SwallowDamage   = 30;
	public int   ExplosionDamage = 25;
	public float ExplosionRadius = 100f;
	public int   MaxSwallowed    = 3;
	public float Speed           = 60f;

	private int       _swallowed   = 0;
	private Enemy      _target     = null;
	private float      _searchTimer = 0f;
	private float      _age         = 0f;
	private const float MAX_AGE     = 30f;
	private Sprite2D   _sprite;
	private float      _glowPulse   = 0f;

	public override void _Ready()
	{
		AddToGroup("Ally");

		CollisionLayer = 1;
		CollisionMask  = 10;

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = 24f };
		AddChild(col);

		// Sprite do macrófago
		_sprite = new Sprite2D();
		_sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/macrofago.png");
		_sprite.Scale   = new Vector2(0.12f, 0.12f);
		AddChild(_sprite);

		var swallowArea = new Area2D { Name = "SwallowArea" };
		swallowArea.CollisionLayer = 0;
		swallowArea.CollisionMask  = 2;
		swallowArea.Monitoring     = true;
		swallowArea.Monitorable    = false;

		var swallowCol   = new CollisionShape2D();
		swallowCol.Shape = new CircleShape2D { Radius = 30f };
		swallowArea.AddChild(swallowCol);
		swallowArea.BodyEntered += OnBodyEntered;
		AddChild(swallowArea);
	}

	public override void _PhysicsProcess(double delta)
	{
		_age += (float)delta;
		if (_age >= MAX_AGE) { QueueFree(); return; }

		_searchTimer -= (float)delta;
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

		_glowPulse += (float)delta * 3f;
		QueueRedraw();
	}

	private void FindTarget()
	{
		float best = float.MaxValue;
		_target    = null;

		foreach (Node n in GetTree().GetNodesInGroup("Enemy"))
		{
			if (n is Enemy e)
			{
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d < best) { best = d; _target = e; }
			}
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Enemy e && IsInstanceValid(e))
		{
			e.TakeDamage(SwallowDamage, EnemyType.None);
			_swallowed++;
			QueueRedraw();

			if (_swallowed >= MaxSwallowed)
				Explode();
		}
	}

	private void Explode()
	{
		foreach (Node n in GetTree().GetNodesInGroup("Enemy"))
		{
			if (n is Enemy e && IsInstanceValid(e))
			{
				if (GlobalPosition.DistanceTo(e.GlobalPosition) <= ExplosionRadius)
					e.TakeDamage(ExplosionDamage, EnemyType.None);
			}
		}

		var exp = new ExplosionEffect();
		exp.Radius = ExplosionRadius;
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
			Vector2 pos   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10f;

			DrawCircle(pos, 7f + pulse * 2f, new Color(1f, 0.85f, 0.3f, 0.3f + pulse * 0.2f));
			DrawCircle(pos, 4.5f, new Color(1f, 0.95f, 0.6f, 1f));
			DrawArc(pos, 4.5f, 0f, Mathf.Tau, 12, new Color(1f, 1f, 0.8f, 0.7f), 1.2f);
		}
	}
}

public partial class ExplosionEffect : Node2D
{
	public float Radius = 100f;
	private float _age  = 0f;
	private const float DURATION = 0.35f;

	public override void _Process(double delta)
	{
		_age += (float)delta;
		QueueRedraw();
		if (_age >= DURATION) QueueFree();
	}

	public override void _Draw()
	{
		float t     = _age / DURATION;
		float alpha = 1f - t;
		float r     = Radius * t;
		DrawCircle(Vector2.Zero, r, new Color(1f, 0.5f, 0.1f, alpha * 0.5f));
		DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 48,
			new Color(1f, 0.8f, 0.2f, alpha), 3f);
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

		_allyCount = GetTree().GetNodesInGroup("Ally").Count;

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
