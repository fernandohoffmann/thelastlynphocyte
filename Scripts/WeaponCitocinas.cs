using Godot;
using System.Collections.Generic;

// ── Nuvem de veneno que aparece periodicamente ao redor do player ──
public partial class CitokinaCloud : Area2D
{
	public int   Damage   = 3;
	public float Radius   = 80f;
	public float Duration = 3f;

	private float         _age      = 0f;
	private float         _tickTimer = 0f;
	private const float   TICK      = 0.5f;
	private HashSet<ulong> _hit     = new();

	public override void _Ready()
	{
		CollisionLayer = 4;
		CollisionMask  = 2;
		Monitoring     = true;
		Monitorable    = false;

		var col   = new CollisionShape2D();
		col.Shape = new CircleShape2D { Radius = Radius };
		AddChild(col);
	}

	public override void _Process(double delta)
	{
		_age       += (float)delta;
		_tickTimer += (float)delta;

		if (_tickTimer >= TICK)
		{
			_tickTimer = 0f;
			DamageOverlapping();
		}

		// Fade out
		float alpha = 1f - Mathf.Clamp(_age / Duration, 0f, 1f);
		Modulate    = new Color(1, 1, 1, alpha);

		QueueRedraw();

		if (_age >= Duration) QueueFree();
	}

	private void DamageOverlapping()
	{
		foreach (var body in GetOverlappingBodies())
			if (body is Enemy e && IsInstanceValid(e))
				e.TakeDamage(Damage, EnemyType.None);

		foreach (var area in GetOverlappingAreas())
			if (area.GetParent() is Enemy e && IsInstanceValid(e))
				e.TakeDamage(Damage, EnemyType.None);
	}

	public override void _Draw()
	{
		float alpha = 1f - Mathf.Clamp(_age / Duration, 0f, 1f);
		// Círculo externo translúcido
		DrawCircle(Vector2.Zero, Radius,
			new Color(0.6f, 1f, 0.3f, 0.18f * alpha));
		// Borda
		DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 48,
			new Color(0.5f, 1f, 0.2f, 0.55f * alpha), 2f);
		// Partículas visuais simples
		for (int i = 0; i < 8; i++)
		{
			float angle = (Mathf.Tau / 8f) * i + _age * 0.8f;
			float r     = Radius * 0.65f;
			var   pos   = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
			DrawCircle(pos, 4f, new Color(0.4f, 1f, 0.2f, 0.6f * alpha));
		}
	}
}

public partial class WeaponCitocinas : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage   = 3;
	private float _radius   = 80f;
	private float _cooldown = 5f;
	private float _duration = 3f;
	private float _timer    = 0f;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage   += 2;
		_cooldown  = Mathf.Max(2f, _cooldown - 0.5f);
		_radius   += 10f;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_timer >= cd)
		{
			_timer = 0f;
			SpawnCloud();
		}
	}

	private void SpawnCloud()
	{
		var cloud = new CitokinaCloud
		{
			Damage   = _damage,
			Radius   = _radius,
			Duration = _duration
		};
		GetTree().CurrentScene.AddChild(cloud);
		cloud.GlobalPosition = GlobalPosition;
		AudioManager.Instance?.PlayShoot();
	}
}
