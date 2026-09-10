using Godot;

public partial class GameScene : Node2D
{
	private UpgradeScreen         _upgradeScreen;
	private MissionManager        _missionManager;
	private MissionCompleteScreen _missionCompleteScreen;

	public override void _Ready()
	{
		GameManager.Instance?.StartGame();
		_upgradeScreen = GetNodeOrNull<UpgradeScreen>("UpgradeScreen");
		CallDeferred(MethodName.Init);
	}

	private void Init()
	{
		var player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player == null) return;

		player.LevelUp    += OnLevelUp;
		player.PlayerDied += OnPlayerDied;

		var playableAreaNode = GetNodeOrNull<Area2D>("World/PlayableArea");
		if (playableAreaNode != null)
		{
			GameManager.Instance?.SetPlayableArea(playableAreaNode);
			CreateBoundaryWalls(GameManager.Instance.PlayableArea);
		}
		else
		{
			GD.PrintErr("[GameScene] No 'World/PlayableArea' nao encontrado.");
		}

		AudioManager.Instance?.PlayHeartLoop();

		// ── Sistema de missões ──────────────────────────────────
		_missionManager = new MissionManager { Name = "MissionManager" };
		AddChild(_missionManager);
		_missionManager.AllMissionsCompleted += OnAllMissionsCompleted;

		_missionCompleteScreen = new MissionCompleteScreen { Name = "MissionCompleteScreen" };
		AddChild(_missionCompleteScreen);

		var options = UpgradeManager.Instance?.GetLevelUpOptions(3);
		if (options != null && options.Count > 0)
			_upgradeScreen?.ShowOptions(options);
	}

	// Cria quatro paredes físicas (StaticBody2D) ao redor da PlayableArea.
	// Layer 8 é a mesma que os raios de "parede" já usados por Enemy/EnemySpawner
	// e que o Player já escuta (collision_mask = 8 no player.tscn).
	private void CreateBoundaryWalls(Rect2 area)
	{
		var walls = GetNodeOrNull<Node2D>("World/Walls");
		if (walls != null) return; // já criadas (ex: recarregou a cena)

		walls = new Node2D { Name = "Walls" };
		GetNode("World").AddChild(walls);

		float t = 300f; // espessura da parede

		AddWall(walls,
			new Vector2(area.Position.X - t / 2f, area.Position.Y + area.Size.Y / 2f),
			new Vector2(t, area.Size.Y + t * 2f)); // esquerda

		AddWall(walls,
			new Vector2(area.End.X + t / 2f, area.Position.Y + area.Size.Y / 2f),
			new Vector2(t, area.Size.Y + t * 2f)); // direita

		AddWall(walls,
			new Vector2(area.Position.X + area.Size.X / 2f, area.Position.Y - t / 2f),
			new Vector2(area.Size.X + t * 2f, t)); // topo

		AddWall(walls,
			new Vector2(area.Position.X + area.Size.X / 2f, area.End.Y + t / 2f),
			new Vector2(area.Size.X + t * 2f, t)); // baixo
	}

	private void AddWall(Node2D parent, Vector2 center, Vector2 size)
	{
		var body = new StaticBody2D
		{
			CollisionLayer = 8,
			CollisionMask  = 0,
			Position       = center
		};
		var col = new CollisionShape2D { Shape = new RectangleShape2D { Size = size } };
		body.AddChild(col);
		parent.AddChild(body);
	}

	private void OnLevelUp(int newLevel)
	{
		var options = UpgradeManager.Instance?.GetLevelUpOptions(3);
		if (options != null && options.Count > 0)
			_upgradeScreen?.ShowOptions(options);
	}

	private void OnPlayerDied()
	{
		AudioManager.Instance?.StopRockMusic();
	}

	private void OnAllMissionsCompleted()
	{
		GameManager.Instance?.EndGameVictory();
		_missionCompleteScreen?.ShowScreen();
	}
}
