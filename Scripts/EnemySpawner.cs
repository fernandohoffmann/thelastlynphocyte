// EnemySpawner.cs — completo, usando PlayableArea em vez de NavLayer
using Godot;

public partial class EnemySpawner : Node2D
{
	private float  _spawnTimer   = 0f;
	private float  _baseInterval = 1.2f;
	private Node2D _player;

	private const float SCREEN_HALF_W    = 1060f;
	private const float SCREEN_HALF_H    =  640f;
	private const int   MAX_TRIES        = 50;
	private const int   MAX_ENEMIES      = 60;
	private const float DESPAWN_DIST     = 2200f;
	private const float DESPAWN_INTERVAL = 2f;
	private const float EDGE_MARGIN      = 60f; // distância mínima da borda da PlayableArea

	private float _despawnTimer = 0f;

	public override void _Ready()
	{
		CallDeferred(MethodName.FindNodes);
	}

	private void FindNodes()
	{
		_player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

		_despawnTimer += (float)delta;
		if (_despawnTimer >= DESPAWN_INTERVAL)
		{
			_despawnTimer = 0f;
			DespawnFarEnemies();
		}

		_spawnTimer += (float)delta;

		float diff     = GameManager.Instance.DifficultyMultiplier;
			float interval = Mathf.Max(0.2f, (_baseInterval / diff) * GameManager.Instance.SpawnIntervalMultiplier);
		if (_spawnTimer >= interval)
		{
			_spawnTimer = 0f;
			SpawnWave(diff);
		}
	}

	private void DespawnFarEnemies()
	{
		if (_player == null) return;

		var enemies = GetTree().GetNodesInGroup("Enemy");
		foreach (Node n in enemies)
		{
			if (n is Enemy e && IsInstanceValid(e))
			{
				float dist = e.GlobalPosition.DistanceTo(_player.GlobalPosition);
				if (dist > DESPAWN_DIST)
					e.QueueFree();
			}
		}
	}

	private void SpawnWave(float diff)
	{
		if (_player == null) { FindNodes(); return; }

		int currentCount = GetTree().GetNodesInGroup("Enemy").Count;
		if (currentCount >= MAX_ENEMIES) return;

		int count = Mathf.Min(1 + (int)((diff - 1f) * 1.2f), 10);
		count = Mathf.Min(count, MAX_ENEMIES - currentCount);

		for (int i = 0; i < count; i++)
			TrySpawnEnemy(diff);
	}

	private void TrySpawnEnemy(float diff)
	{
		for (int attempt = 0; attempt < MAX_TRIES; attempt++)
		{
			Vector2 pos = GetSpawnPosOutsideScreen();
			if (IsValidSpawn(pos))
			{
				SpawnAt(pos, diff);
				return;
			}
		}
	}

	private Vector2 GetSpawnPosOutsideScreen()
	{
		if (_player == null) return Vector2.Zero;

		int   side = (int)(GD.Randi() % 4);
		float x, y;

		switch (side)
		{
			case 0:
				x = (float)GD.RandRange(-SCREEN_HALF_W, SCREEN_HALF_W);
				y = -SCREEN_HALF_H - (float)GD.RandRange(50, 300);
				break;
			case 1:
				x = (float)GD.RandRange(-SCREEN_HALF_W, SCREEN_HALF_W);
				y =  SCREEN_HALF_H + (float)GD.RandRange(50, 300);
				break;
			case 2:
				x = -SCREEN_HALF_W - (float)GD.RandRange(50, 300);
				y = (float)GD.RandRange(-SCREEN_HALF_H, SCREEN_HALF_H);
				break;
			default:
				x =  SCREEN_HALF_W + (float)GD.RandRange(50, 300);
				y = (float)GD.RandRange(-SCREEN_HALF_H, SCREEN_HALF_H);
				break;
		}

		Vector2 pos = _player.GlobalPosition + new Vector2(x, y);

		// Restringe para dentro da PlayableArea, respeitando a margem
		if (GameManager.Instance != null)
			pos = GameManager.Instance.ClampToPlayableArea(pos, EDGE_MARGIN);

		return pos;
	}

	private bool IsValidSpawn(Vector2 worldPos)
	{
		// Confere se a posição está dentro da área jogável
		if (GameManager.Instance != null)
		{
			var area = GameManager.Instance.PlayableArea;
			if (!area.HasPoint(worldPos))
				return false;
		}

		// Confere se não tem parede/obstáculo físico ali perto
		var space = GetWorld2D().DirectSpaceState;
		float checkDist = 48f;
		Vector2[] dirs =
		{
			Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down,
			new Vector2( 1, 1).Normalized(), new Vector2(-1, 1).Normalized(),
			new Vector2( 1,-1).Normalized(), new Vector2(-1,-1).Normalized()
		};

		foreach (var dir in dirs)
		{
			var query = PhysicsRayQueryParameters2D.Create(
				worldPos, worldPos + dir * checkDist, 8);
			if (space.IntersectRay(query).Count > 0)
				return false;
		}

		return true;
	}

	private void SpawnAt(Vector2 pos, float diff)
	{
		bool  isVirus = GD.Randf() < 0.20f;
		Enemy enemy   = isVirus ? new Virus() : new Bacteria();
		GetTree().CurrentScene.AddChild(enemy);
		enemy.GlobalPosition = pos;
		enemy.ApplyDifficultyScale(diff);
	}
}
