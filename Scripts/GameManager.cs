using Godot;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	[Signal] public delegate void GamePausedEventHandler();
	[Signal] public delegate void GameResumedEventHandler();
	[Signal] public delegate void GameOverEventHandler();
	[Signal] public delegate void DnaChangedEventHandler(int totalDna);

	public bool  IsGameRunning       { get; private set; } = false;
	public bool  IsPaused            { get; private set; } = false;
	public float ElapsedTime         { get; private set; } = 0f;
	public float DifficultyMultiplier { get; private set; } = 1f;
	public int   SessionDna          { get; private set; } = 0;

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

	// GameManager.cs — StartGame corrigido
public void StartGame()
{
	IsGameRunning        = true;
	IsPaused             = false;  // ← garante reset do pause
	ElapsedTime          = 0f;
	DifficultyMultiplier = 1f;
	SessionDna           = 0;
	GetTree().Paused     = false;  // ← despausa a árvore se ficou pausada
}

	public void GainDna(int amount)
	{
		// Aplica bônus de Síntese de DNA se o player tiver
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

	public void ReturnToMenu()
	{
		IsGameRunning    = false;
		IsPaused         = false;
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public float GetRemainingTime() => Mathf.Max(0f, MAX_TIME - ElapsedTime);
}
