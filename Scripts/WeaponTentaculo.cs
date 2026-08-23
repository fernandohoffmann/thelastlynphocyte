using Godot;
using System.Collections.Generic;

public partial class WeaponTentaculo : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage   = 12;
	private float _range    = 100f;
	private float _cooldown = 1.2f;
	private float _timer    = 0f;
	private int   _count    = 1;

	private bool          _attacking = false;
	private float         _atkTimer  = 0f;
	private List<Vector2> _atkDirs   = new();

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

		if (_attacking)
		{
			_atkTimer -= (float)delta;
			if (_atkTimer <= 0f) { _attacking = false; QueueRedraw(); }
		}

		if (_timer >= cd) { _timer = 0f; Attack(); }
	}

	private void Attack()
	{
		_atkDirs.Clear();

		var sorted = new List<(float d, Enemy e)>();
		foreach (Node n in GetTree().GetNodesInGroup("Enemy"))
			if (n is Enemy e)
			{
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d <= _range) sorted.Add((d, e));
			}

		sorted.Sort((a, b) => a.d.CompareTo(b.d));

		int hits = Mathf.Min(_count, sorted.Count);
		for (int i = 0; i < hits; i++)
		{
			sorted[i].e.TakeDamage(_damage, EnemyType.None);
			_atkDirs.Add((sorted[i].e.GlobalPosition - GlobalPosition).Normalized());
		}

		if (hits > 0)
		{
			_attacking = true;
			_atkTimer  = 0.2f;
			QueueRedraw();
			AudioManager.Instance?.PlayShoot(); // ← NOVO
		}
	}

	public override void _Draw()
	{
		DrawArc(Vector2.Zero, _range, 0, Mathf.Tau, 32, new Color(1, 0.5f, 0, 0.12f), 1f);
		if (!_attacking) return;

		foreach (var dir in _atkDirs)
		{
			DrawLine(Vector2.Zero, dir * _range, new Color(1f, 0.4f, 0f, 0.85f), 4f);
			DrawCircle(dir * _range, 10f, new Color(1f, 0.3f, 0f));
		}
	}
}
