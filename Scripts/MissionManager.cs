using Godot;
using System.Collections.Generic;

public enum MissionType { SurviveTime, KillBacteria, DestroyColonies, KillTimed, DefeatBoss }

public class MissionData
{
	public string Title;
	public MissionType Type;
	public int Target;
	public float TimeLimit;
}

public partial class MissionManager : Node
{
	public static MissionManager Instance { get; private set; }

	[Signal] public delegate void MissionChangedEventHandler(string title, string progressText);
	[Signal] public delegate void AllMissionsCompletedEventHandler();

	private List<MissionData> _missions;
	private int   _currentIndex = -1;
	private int   _progress     = 0;
	private float _subTimer     = 0f;

	private int _coloniesDestroyed = 0;
	private const int COLONIES_TOTAL = 3;

	// Boss deve nascer bem longe do jogador
	private const int   BOSS_SPAWN_SAMPLES = 24;
	private const float BOSS_SPAWN_MARGIN  = 150f;

	public override void _Ready()
	{
		Instance = this;

		_missions = new List<MissionData>
		{
			new MissionData { Title = "Resistir ao primeiro ataque",             Type = MissionType.SurviveTime,     Target = 300 },
			new MissionData { Title = "Conter a infeccao na corrente sanguinea", Type = MissionType.KillBacteria,    Target = 30  },
			new MissionData { Title = "Localizar o foco da infeccao",           Type = MissionType.DestroyColonies, Target = COLONIES_TOTAL },
			new MissionData { Title = "Purificar a corrente sanguinea",          Type = MissionType.KillTimed,       Target = 50, TimeLimit = 180f },
			new MissionData { Title = "Eliminar a Doenca de Chagas",             Type = MissionType.DefeatBoss,      Target = 1   },
		};

		Enemy.EnemyDied += OnEnemyDied;
		StartNextMission();
	}

	public override void _ExitTree()
	{
		Enemy.EnemyDied -= OnEnemyDied;
		if (Instance == this) Instance = null;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;
		if (_currentIndex < 0 || _currentIndex >= _missions.Count) return;

		var m = _missions[_currentIndex];

		if (m.Type == MissionType.SurviveTime)
		{
			_progress = (int)Mathf.Min(GameManager.Instance.ElapsedTime, m.Target);
			EmitProgress();
			if (_progress >= m.Target) CompleteCurrentMission();
		}
		else if (m.Type == MissionType.KillTimed)
		{
			_subTimer += (float)delta;
			EmitProgress();

			if (_progress >= m.Target)
			{
				CompleteCurrentMission();
			}
			else if (_subTimer >= m.TimeLimit)
			{
				_progress = 0;
				_subTimer = 0f;
				EmitProgress();
			}
		}
		else if (m.Type == MissionType.DefeatBoss)
		{
			// Atualiza o HP do boss em tempo real no painel de missão
			EmitProgress();
		}
	}

	private void StartNextMission()
	{
		_currentIndex++;
		_progress = 0;
		_subTimer = 0f;

		if (_currentIndex >= _missions.Count)
		{
			EmitSignal(SignalName.AllMissionsCompleted);
			return;
		}

		var m = _missions[_currentIndex];

		if (m.Type == MissionType.DestroyColonies)
			SpawnColonies(COLONIES_TOTAL);

		if (m.Type == MissionType.DefeatBoss)
			SpawnBoss();

		EmitProgress();
	}

	private void CompleteCurrentMission() => StartNextMission();

	private void EmitProgress()
	{
		if (_currentIndex < 0 || _currentIndex >= _missions.Count) return;
		var m = _missions[_currentIndex];

		string text = m.Type switch
		{
			MissionType.SurviveTime     => $"{_progress}/{m.Target}s",
			MissionType.KillBacteria    => $"{_progress}/{m.Target}",
			MissionType.DestroyColonies => $"{_coloniesDestroyed}/{m.Target}",
			MissionType.KillTimed       => $"{_progress}/{m.Target} ({Mathf.Max(0, m.TimeLimit - _subTimer):0}s)",
			MissionType.DefeatBoss      => ChagasBoss.Instance != null && IsInstanceValid(ChagasBoss.Instance)
				? $"{ChagasBoss.Instance.Hp}/{ChagasBoss.Instance.MaxHp} HP"
				: "Boss derrotado!",
			_ => ""
		};

		EmitSignal(SignalName.MissionChanged, m.Title, text);
	}

	private void OnEnemyDied(Enemy enemy)
	{
		if (_currentIndex < 0 || _currentIndex >= _missions.Count) return;
		var m = _missions[_currentIndex];

		if (enemy is Colony)
		{
			_coloniesDestroyed++;
			GameManager.Instance?.ReduceSpawnRate(0.15f);

			if (m.Type == MissionType.DestroyColonies)
			{
				_progress = _coloniesDestroyed;
				EmitProgress();
				if (_coloniesDestroyed >= m.Target) CompleteCurrentMission();
			}
			return;
		}

		if (enemy is ChagasBoss)
		{
			if (m.Type == MissionType.DefeatBoss)
			{
				EmitProgress(); // manda o "Boss derrotado!" antes de trocar de missão
				CompleteCurrentMission();
			}
			return;
		}

		if (m.Type == MissionType.KillBacteria && enemy.Type == EnemyType.Bacteria)
		{
			_progress++;
			EmitProgress();
			if (_progress >= m.Target) CompleteCurrentMission();
		}
		else if (m.Type == MissionType.KillTimed)
		{
			_progress++;
			EmitProgress();
			if (_progress >= m.Target) CompleteCurrentMission();
		}
	}

	// folga minima da parede pra colonia/boss (o corpo deles e grande)
	private const float COLONY_WALL_MARGIN = 70f;

	private void SpawnColonies(int count)
	{
		if (GameManager.Instance == null) return;
		var area = GameManager.Instance.PlayableArea;

		for (int i = 0; i < count; i++)
		{
			Vector2 pos = Vector2.Zero;

			// Sorteia ate cair num lugar livre. Sem isso a colonia pode
			// nascer dentro de uma veia e ficar inalcancavel.
			for (int tries = 0; tries < 300; tries++)
			{
				pos = new Vector2(
					(float)GD.RandRange(area.Position.X + 200, area.End.X - 200),
					(float)GD.RandRange(area.Position.Y + 200, area.End.Y - 200)
				);

				if (VeinMap.Instance == null || VeinMap.Instance.IsWalkable(pos, COLONY_WALL_MARGIN))
					break;
			}

			var colony = new Colony();
			GetTree().CurrentScene.AddChild(colony);
			colony.GlobalPosition = pos;
		}
	}

	// Nasce PERTO do jogador, numa faixa de distancia: longe o bastante pra nao
	// aparecer em cima dele, perto o bastante pra encontrar na hora.
	private const float BOSS_NEAR_MIN = 420f;
	private const float BOSS_NEAR_MAX = 800f;

	private void SpawnBoss()
	{
		if (GameManager.Instance == null || Player.Instance == null) return;

		var     area      = GameManager.Instance.PlayableArea;
		Vector2 playerPos = Player.Instance.GlobalPosition;
		Vector2 bestPos   = playerPos;
		bool    found     = false;

		for (int i = 0; i < BOSS_SPAWN_SAMPLES * 4; i++)
		{
			float   ang  = (float)GD.RandRange(0.0, Mathf.Tau);
			float   dist = (float)GD.RandRange(BOSS_NEAR_MIN, BOSS_NEAR_MAX);
			Vector2 cand = playerPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

			// dentro do mapa
			if (!area.HasPoint(cand)) continue;

			if (VeinMap.Instance != null)
			{
				// fora da parede...
				if (!VeinMap.Instance.IsWalkable(cand, COLONY_WALL_MARGIN)) continue;
				// ...e do mesmo lado das veias que o jogador, senao ele nasce
				// atras de uma parede e demora pra dar a volta
				if (!VeinMap.Instance.HasLineOfSight(cand, playerPos, 10f)) continue;
			}

			bestPos = cand;
			found   = true;
			break;
		}

		if (!found && VeinMap.Instance != null)
			bestPos = VeinMap.Instance.RandomWalkablePoint(COLONY_WALL_MARGIN);

		var boss = new ChagasBoss();
		GetTree().CurrentScene.AddChild(boss);
		boss.GlobalPosition = bestPos;

		GD.Print($"[MissionManager] boss nasceu a {bestPos.DistanceTo(playerPos):F0}px do player");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F9)
		{
			GD.Print("[DEBUG] Pulando missao atual: ", _currentIndex);
			CompleteCurrentMission();
		}
	}
}
