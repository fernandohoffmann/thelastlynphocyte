// HemoglobinaSmoke.cs
using Godot;

public partial class HemoglobinaSmoke : Node2D
{
	public float Radius = 80f;

	private float _age      = 0f;
	private const float DURATION = 0.7f;

	private struct Particle
	{
		public Vector2 Pos;
		public Vector2 Vel;
		public float   Size;
		public float   Alpha;
	}

	private Particle[] _particles;

	public override void _Ready()
	{
		_particles = new Particle[16];
		for (int i = 0; i < _particles.Length; i++)
		{
			float angle = GD.Randf() * Mathf.Tau;
			float speed = GD.Randf() * Radius * 0.6f + 20f;
			_particles[i] = new Particle
			{
				Pos   = Vector2.Zero,
				Vel   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
				Size  = GD.Randf() * 12f + 6f,
				Alpha = GD.Randf() * 0.5f + 0.4f
			};
		}
	}

	public override void _Process(double delta)
	{
		_age += (float)delta;
		for (int i = 0; i < _particles.Length; i++)
		{
			_particles[i].Pos  += _particles[i].Vel * (float)delta;
			_particles[i].Vel  *= 0.93f;
		}
		QueueRedraw();
		if (_age >= DURATION) QueueFree();
	}

	public override void _Draw()
	{
		float t = _age / DURATION;
		foreach (var p in _particles)
		{
			float a    = p.Alpha * (1f - t);
			float size = p.Size  * (1f + t * 1.5f);
			// Cor vermelha/laranja de hemoglobina
			DrawCircle(p.Pos, size, new Color(0.9f, 0.15f, 0.15f, a * 0.5f));
			DrawCircle(p.Pos, size * 0.5f, new Color(1f, 0.4f, 0.1f, a));
		}
	}
}
