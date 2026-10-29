// Colony.cs — completo, agora também gera bactérias periodicamente
using Godot;

public partial class Colony : Enemy
{
	private float _spawnTimer = 0f;
	private const float SPAWN_INTERVAL = 6f;  // a cada 6s nasce uma bacteria perto
	private const float SPAWN_RADIUS   = 90f;

	public override void _Ready()
	{
		Type       = EnemyType.Bacteria;
		BaseSpeed  = 0f;
		BaseHp     = 90;
		BaseDamage = 0;
		BaseXp     = 40;
		BodyRadius = 34f;
		BodyColor  = new Color(0.55f, 0.05f, 0.05f);
		UseSprite  = false;

		base._Ready();
		AddToGroup("Colony");

		_spawnTimer = SPAWN_INTERVAL;
	}

	public override void _PhysicsProcess(double delta)
	{
		float pulse = 1f + (float)Mathf.Sin(Time.GetTicksMsec() / 300.0) * 0.06f;
		Scale = new Vector2(pulse, pulse);

		_spawnTimer -= (float)delta;
		if (_spawnTimer <= 0f)
		{
			_spawnTimer = SPAWN_INTERVAL;
			SpawnNearbyBacteria();
		}
	}

	private void SpawnNearbyBacteria()
	{
		if (!IsInstanceValid(this)) return;

		float angle = (float)GD.RandRange(0.0, Mathf.Tau);
		Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SPAWN_RADIUS;
		Vector2 pos    = GlobalPosition + offset;

		if (GameManager.Instance != null)
			pos = GameManager.Instance.ClampToPlayableArea(pos, 40f);

		var bacteria = new Bacteria();
		GetTree().CurrentScene.AddChild(bacteria);
		bacteria.GlobalPosition = pos;

		if (GameManager.Instance != null)
			bacteria.ApplyDifficultyScale(GameManager.Instance.DifficultyMultiplier);
	}
}
