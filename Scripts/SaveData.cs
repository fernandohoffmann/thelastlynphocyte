using Godot;
using System.Collections.Generic;

public static class SaveData
{
	private const string SAVE_PATH = "user://tll_save.cfg";

	public static int TotalDna { get; private set; } = 0;
	public static HashSet<string> UnlockedItems { get; private set; } = new();
	public static HashSet<string> CodexUnlocked { get; private set; } = new();
	public static HashSet<int>    UnlockedPhases { get; private set; } = new();

	public static float MusicVolume     { get; private set; } = 0.8f;
	public static float SfxVolume       { get; private set; } = 1.0f;
	public static bool  Fullscreen      { get; private set; } = false;
	public static int   ResolutionIndex { get; private set; } = DisplaySettings.DEFAULT_INDEX;

	public static void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SAVE_PATH) != Error.Ok)
		{
			TotalDna        = 0;
			UnlockedItems   = new HashSet<string> { "Anticorpos" };
			CodexUnlocked   = new HashSet<string>();
			UnlockedPhases  = new HashSet<int>();
			MusicVolume     = 0.8f;
			SfxVolume       = 1.0f;
			Fullscreen      = false;
			ResolutionIndex = DisplaySettings.DEFAULT_INDEX;
			return;
		}

		TotalDna = (int)cfg.GetValue("save", "total_dna", 0);

		CodexUnlocked = new HashSet<string>();
		var codex = cfg.GetValue("save", "codex", new Godot.Collections.Array());
		foreach (var item in codex.AsGodotArray())
			CodexUnlocked.Add(item.AsString());

		UnlockedPhases = new HashSet<int>();
		var phases = cfg.GetValue("save", "phases", new Godot.Collections.Array());
		foreach (var item in phases.AsGodotArray())
			UnlockedPhases.Add(item.AsInt32());

		UnlockedItems = new HashSet<string>();
		UnlockedItems.Add("Anticorpos");

		var unlocked = cfg.GetValue("save", "unlocked_items", new Godot.Collections.Array());
		foreach (var item in unlocked.AsGodotArray())
			UnlockedItems.Add(item.AsString());

		MusicVolume     = cfg.GetValue("settings", "music_volume", 0.8f).AsSingle();
		SfxVolume       = cfg.GetValue("settings", "sfx_volume", 1.0f).AsSingle();
		Fullscreen      = cfg.GetValue("settings", "fullscreen", false).AsBool();
		ResolutionIndex = cfg.GetValue("settings", "resolution_index",
							DisplaySettings.DEFAULT_INDEX).AsInt32();
	}

	public static void Save()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("save", "total_dna", TotalDna);

		var arr = new Godot.Collections.Array();
		foreach (var item in UnlockedItems)
			arr.Add(item);
		cfg.SetValue("save", "unlocked_items", arr);

		var codexArr = new Godot.Collections.Array();
		foreach (var item in CodexUnlocked)
			codexArr.Add(item);
		cfg.SetValue("save", "codex", codexArr);

		var phaseArr = new Godot.Collections.Array();
		foreach (var n in UnlockedPhases)
			phaseArr.Add(n);
		cfg.SetValue("save", "phases", phaseArr);

		cfg.SetValue("settings", "music_volume", MusicVolume);
		cfg.SetValue("settings", "sfx_volume", SfxVolume);
		cfg.SetValue("settings", "fullscreen", Fullscreen);
		cfg.SetValue("settings", "resolution_index", ResolutionIndex);

		cfg.Save(SAVE_PATH);
	}

	public static void AddDna(int amount)
	{
		TotalDna += amount;
		Save();
	}

	public static bool SpendDna(int amount)
	{
		if (TotalDna < amount) return false;
		TotalDna -= amount;
		Save();
		return true;
	}

	public static void UnlockPhase(int number)
	{
		UnlockedPhases.Add(number);
		Save();
	}

	public static void UnlockCodex(string codexId)
	{
		CodexUnlocked.Add(codexId);
		Save();
	}

	public static void UnlockItem(string itemId)
	{
		UnlockedItems.Add(itemId);
		Save();
	}

	public static bool IsUnlocked(string itemId)
	{
		return UnlockedItems.Contains(itemId);
	}

	public static void SetMusicVolume(float v)
	{
		MusicVolume = v;
		Save();
	}

	public static void SetSfxVolume(float v)
	{
		SfxVolume = v;
		Save();
	}

	public static void SetFullscreen(bool v)
	{
		Fullscreen = v;
		Save();
	}

	public static void SetResolutionIndex(int i)
	{
		ResolutionIndex = i;
		Save();
	}

	public static void Reset()
	{
		TotalDna      = 0;
		UnlockedItems = new HashSet<string> { "Anticorpos" };
		CodexUnlocked  = new HashSet<string>();
		UnlockedPhases = new HashSet<int>();
		Save();
		UpgradeManager.Instance?.ApplyLoadout(
			new System.Collections.Generic.List<UpgradeId>(),
			new System.Collections.Generic.List<UpgradeId>()
		);
	}
}
