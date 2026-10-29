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

			// O molde de Line2D e o tilemap sao cenario da fase 1: escondidos
			// SEMPRE aqui fora, senao aparecem no meio do pulmao.
			var painted = GetNodeOrNull<Node2D>("World/VeinPaint");
			if (painted != null) painted.Visible = false;

			var tileMap = GetNodeOrNull<CanvasItem>("World/TileMapLayer");
			if (tileMap != null) tileMap.Visible = GamePhase.Data.ShowTileMap;

			if (GamePhase.Data.HasVeinWalls)
			{
				var veins = new VeinMap { Name = "VeinMap" };
				GetNode("World").AddChild(veins);
				veins.Build(GameManager.Instance.PlayableArea, painted);

				// Marker2D opcional "PlayerSpawn" define onde o player nasce
				var spawnMarker = GetNodeOrNull<Marker2D>("World/PlayerSpawn");
				player.GlobalPosition = spawnMarker != null
					? spawnMarker.GlobalPosition
					: veins.SpawnPoint;
			}
			else if (GamePhase.Data.HasLungWalls)
			{
				// A silhueta do pulmao vira parede; as ramificacoes bronquicas
				// sao so cenario, sem colisao nenhuma.
				var lungMap = new VeinMap { Name = "VeinMap" };
				GetNode("World").AddChild(lungMap);
				lungMap.BuildLung(GameManager.Instance.PlayableArea);

				// a respiracao precisa existir ANTES do cenario, que le dela
				AddChild(new LungBreath { Name = "LungBreath" });
				GetNode("World").AddChild(new LungBackground { Name = "LungBackground" });
				player.GlobalPosition = lungMap.SpawnPoint;
			}
			else
			{
				player.GlobalPosition = GameManager.Instance.PlayableArea.GetCenter();
			}
		}
		else
		{
			GD.PrintErr("[GameScene] No 'World/PlayableArea' nao encontrado.");
		}

		AudioManager.Instance?.PlayHeartLoop();

		// ── Codex ────────────────────────────────────────────────
		Codex.Hook();                       // liga nos eventos que o jogo ja tem
		AddChild(new CodexToast { Name = "CodexToast" });
		Codex.Discover(
			CodexId.Veia, CodexId.CorrenteSanguinea, CodexId.Plasma, CodexId.Tecido,
			CodexId.Linfocito, CodexId.Hemacia, CodexId.Circulacao);

		// ── Sistema de missões ──────────────────────────────────
		// A fase 2 ainda nao tem missoes: roda so contra o relogio.
		if (GamePhase.Data.HasMissions)
		{
			_missionManager = new MissionManager { Name = "MissionManager" };
			AddChild(_missionManager);
			_missionManager.AllMissionsCompleted += OnAllMissionsCompleted;

			_missionCompleteScreen = new MissionCompleteScreen { Name = "MissionCompleteScreen" };
			AddChild(_missionCompleteScreen);
		}

		var options = UpgradeManager.Instance?.GetLevelUpOptions(3);
		if (options != null && options.Count > 0)
			_upgradeScreen?.ShowOptions(options);

		// ── Efeitos visuais ──────────────────────────────────────
		if (DebugPanel.ENABLED)
			AddChild(new DebugPanel { Name = "DebugPanel" });

		SetupGlowEnvironment();
		if (GamePhase.Data.HasVeinOverlay)
			AddChild(new VeinOverlay { Name = "VeinOverlay" });
		AddChild(new AmbientShading { Name = "AmbientShading" });
		AddChild(new VisionOverlay  { Name = "VisionOverlay"  });

		// globulos vermelhos boiando pela corrente sanguinea
		if (GamePhase.Data.HasBloodCells)
			GetNode("World").AddChild(new BloodCellField { Name = "BloodCellField" });
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

	// Um level-up de cada vez. Subir varios niveis de uma vez (debug, ou dois
	// drops grandes juntos) chamaria ShowOptions em sequencia e so a ultima
	// chamada apareceria — as escolhas anteriores se perderiam.
	private void OnLevelUp(int newLevel)
	{
		DebugPanel.PendingLevelUps++;
		TryShowNextUpgrade();
	}

	private void TryShowNextUpgrade()
	{
		if (DebugPanel.PendingLevelUps <= 0) return;
		if (_upgradeScreen == null || _upgradeScreen.Visible) return;

		var options = UpgradeManager.Instance?.GetLevelUpOptions(3);
		if (options == null || options.Count == 0)
		{
			DebugPanel.PendingLevelUps = 0;   // nada pra oferecer: limpa a fila
			return;
		}

		DebugPanel.PendingLevelUps--;
		_upgradeScreen.ShowOptions(options);
	}

	public override void _Process(double delta)
	{
		// a tela de upgrade nao avisa quando fecha, entao a fila e drenada aqui
		TryShowNextUpgrade();
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

	private void SetupGlowEnvironment()
	{
		GetViewport().UseHdr2D = true; // sem isso, o glow nao aparece no 2D

		var env = new Godot.Environment();
		env.BackgroundMode      = Godot.Environment.BGMode.Canvas;
		env.GlowEnabled         = true;
		env.GlowIntensity       = 0.9f;
		env.GlowStrength        = 1.1f;
		env.GlowBloom           = 0.05f;
		env.GlowBlendMode       = Godot.Environment.GlowBlendModeEnum.Softlight;
		env.GlowHdrThreshold    = 0.9f; // só o que é "brilhante" (luzes aditivas) gera glow

		var worldEnv = new WorldEnvironment { Environment = env };
		AddChild(worldEnv);
	}
}
