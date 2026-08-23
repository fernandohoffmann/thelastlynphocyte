using Godot;

public partial class GameScene : Node2D
{
	private UpgradeScreen _upgradeScreen;

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

		var navLayer = GetNodeOrNull<TileMapLayer>("World/NavLayer");
		if (navLayer != null) navLayer.Visible = false;

		AudioManager.Instance?.PlayHeartLoop();

		var options = UpgradeManager.Instance?.GetLevelUpOptions(3);
		if (options != null && options.Count > 0)
			_upgradeScreen?.ShowOptions(options);
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
}
