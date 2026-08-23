using Godot;
using System.Collections.Generic;

public partial class WeaponProteinaComplemento : Node2D
{
	public int WeaponLevel { get; private set; } = 1;

	private int   _damage     = 6;      // dano menor
	private int   _bounces    = 3;      // saltos
	private float _cooldown   = 2.5f;
	private float _chainRange = 250f;   // range entre saltos
	private float _activationRange = 250f; // range para ativar (do player)
	private float _timer      = 0f;

	// Visual
	private List<Vector2> _chainWorld = new(); // posições globais
	private float         _visualTimer = 0f;

	public void UpgradeLevel()
	{
		WeaponLevel++;
		_damage  += 3;
		_bounces += 1;
		_chainRange += 30f;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		float cd = _cooldown * (Player.Instance?.CooldownReduction ?? 1f);
		_timer += (float)delta;

		if (_visualTimer > 0f)
		{
			_visualTimer -= (float)delta;
			QueueRedraw();
		}
		else if (_chainWorld.Count > 0)
		{
			_chainWorld.Clear();
			QueueRedraw();
		}

		if (_timer >= cd) { _timer = 0f; FireChain(); }
	}

	private void FireChain()
	{
		AudioManager.Instance?.PlayShoot();  // ← NOVO
		var enemies = GetTree().GetNodesInGroup("Enemy");
		if (enemies.Count == 0) return;

		// Primeiro alvo: inimigo mais próximo dentro do range de ativação
		Enemy first = null;
		float best  = float.MaxValue;
		foreach (Node n in enemies)
		{
			if (n is Enemy e)
			{
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d < _activationRange && d < best) { best = d; first = e; }
			}
		}
		if (first == null) return;

		_chainWorld.Clear();
		_chainWorld.Add(GlobalPosition); // começa do player

		var hit = new HashSet<Enemy>();
		Enemy cur = first;

		for (int i = 0; i <= _bounces; i++)
		{
			if (!IsInstanceValid(cur)) break;

			cur.TakeDamage(_damage, EnemyType.None);
			_chainWorld.Add(cur.GlobalPosition);
			hit.Add(cur);

			// Próximo alvo mais próximo dentro do chainRange
			Enemy next = null;
			float nd   = float.MaxValue;
			foreach (Node n in enemies)
			{
				if (n is Enemy e && !hit.Contains(e))
				{
					float d = cur.GlobalPosition.DistanceTo(e.GlobalPosition);
					if (d < _chainRange && d < nd) { nd = d; next = e; }
				}
			}
			if (next == null) break;
			cur = next;
		}

		_visualTimer = 0.35f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		// Desenha range de ativação (círculo sutil ao redor do player)
		DrawArc(Vector2.Zero, _activationRange, 0f, Mathf.Tau, 64,
			new Color(0.4f, 0.8f, 1f, 0.08f), 1.5f);

		if (_visualTimer <= 0f || _chainWorld.Count < 2) return;

		float alpha = Mathf.Clamp(_visualTimer * 3f, 0f, 1f);
		for (int i = 0; i < _chainWorld.Count - 1; i++)
		{
			Vector2 from = ToLocal(_chainWorld[i]);
			Vector2 to   = ToLocal(_chainWorld[i + 1]);
			// Linha grossa externa
			DrawLine(from, to, new Color(0.4f, 0.8f, 1f, alpha * 0.4f), 8f);
			// Linha fina interna
			DrawLine(from, to, new Color(0.8f, 0.95f, 1f, alpha), 2f);
			// Círculo no ponto de impacto
			DrawCircle(to, 6f, new Color(0.4f, 0.8f, 1f, alpha));
		}
	}
}
