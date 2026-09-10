using Godot;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	[Signal] public delegate void GamePausedEventHandler();
	[Signal] public delegate void GameResumedEventHandler();
	[Signal] public delegate void GameOverEventHandler();
	[Signal] public delegate void DnaChangedEventHandler(int totalDna);

	public bool  IsGameRunning        { get; private set; } = false;
	public bool  IsPaused             { get; private set; } = false;
	public float ElapsedTime          { get; private set; } = 0f;
	public float DifficultyMultiplier { get; private set; } = 1f;
	public int   SessionDna           { get; private set; } = 0;

	public Rect2 PlayableArea { get; private set; } = new Rect2(-1000, -1000, 2000, 2000);
	public event System.Action PlayableAreaChanged;

	// Multiplicador do intervalo de spawn — colônias destruídas aumentam isso (spawn mais lento)
	public float SpawnIntervalMultiplier { get; private set; } = 1f;

	private const float MAX_TIME = 1200f;

	public override void _Ready()
	{
		if (Instance != null) { QueueFree(); return; }
		Instance = this;
		SaveData.Load();
	}

	public override void _Process(double delta)
	{
		if (!IsGameRunning || IsPaused) return;
		ElapsedTime += (float)delta;
		float t = Mathf.Clamp(ElapsedTime / MAX_TIME, 0f, 1f);
		DifficultyMultiplier = 1f + (t * t * 7f);
		AudioManager.Instance?.UpdateHeartPitch(t);
	}

	public void StartGame()
	{
		IsGameRunning            = true;
		IsPaused                 = false;
		ElapsedTime              = 0f;
		DifficultyMultiplier     = 1f;
		SessionDna               = 0;
		SpawnIntervalMultiplier  = 1f;
		GetTree().Paused         = false;
	}

	public void SetPlayableArea(Area2D areaNode)
	{
		if (areaNode == null) return;

		var col = areaNode.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col == null || col.Shape is not RectangleShape2D rectShape)
		{
			GD.PrintErr("[GameManager] PlayableArea precisa de um CollisionShape2D com RectangleShape2D.");
			return;
		}

		Vector2 size    = rectShape.Size;
		Vector2 center  = areaNode.GlobalPosition + col.Position;
		Vector2 topLeft = center - size / 2f;

		PlayableArea = new Rect2(topLeft, size);
		PlayableAreaChanged?.Invoke();
	}

	public Vector2 ClampToPlayableArea(Vector2 pos, float margin = 0f)
	{
		float minX = PlayableArea.Position.X + margin;
		float minY = PlayableArea.Position.Y + margin;
		float maxX = PlayableArea.End.X - margin;
		float maxY = PlayableArea.End.Y - margin;

		return new Vector2(
			Mathf.Clamp(pos.X, minX, maxX),
			Mathf.Clamp(pos.Y, minY, maxY)
		);
	}

	public void ReduceSpawnRate(float amount)
	{
		SpawnIntervalMultiplier += amount;
	}

	public void GainDna(int amount)
	{
		float bonus = Player.Instance?.DnaBonus ?? 1f;
		int total   = Mathf.RoundToInt(amount * bonus);

		SessionDna += total;
		SaveData.AddDna(total);
		EmitSignal(SignalName.DnaChanged, SaveData.TotalDna);
	}

	public void PauseGame()
	{
		if (IsPaused) return;
		IsPaused         = true;
		GetTree().Paused = true;
		AudioManager.Instance?.PauseHeartLoop(true);
		EmitSignal(SignalName.GamePaused);
	}

	public void ResumeGame()
	{
		if (!IsPaused) return;
		IsPaused         = false;
		GetTree().Paused = false;
		AudioManager.Instance?.PauseHeartLoop(false);
		EmitSignal(SignalName.GameResumed);
	}

	public void EndGame()
	{
		IsGameRunning    = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.GameOver);
	}

	// Vitória — todas as missões concluídas (não mostra a tela de Game Over)
	public void EndGameVictory()
	{
		IsGameRunning    = false;
		GetTree().Paused = false;
	}

	public void ReturnToMenu()
	{
		IsGameRunning    = false;
		IsPaused         = false;
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public float GetRemainingTime() => Mathf.Max(0f, MAX_TIME - ElapsedTime);
}
