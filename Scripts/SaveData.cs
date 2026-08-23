using Godot;
using System.Collections.Generic;

public static class SaveData
{
	private const string SAVE_PATH = "user://tll_save.cfg";

	// DNA acumulado entre runs
	public static int TotalDna { get; private set; } = 0;

	// Quais itens estão desbloqueados na árvore
	// Key: UpgradeId.ToString(), Value: true = desbloqueado
	public static HashSet<string> UnlockedItems { get; private set; } = new();

	public static void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SAVE_PATH) != Error.Ok)
		{
			// Arquivo não existe ainda — valores padrão
			TotalDna = 0;
			UnlockedItems = new HashSet<string> { "Anticorpos" }; // começa desbloqueado
			return;
		}

		TotalDna = (int)cfg.GetValue("save", "total_dna", 0);
		UnlockedItems = new HashSet<string>();

		// Anticorpos sempre desbloqueado
		UnlockedItems.Add("Anticorpos");

		var unlocked = cfg.GetValue("save", "unlocked_items", new Godot.Collections.Array());
		foreach (var item in unlocked.AsGodotArray())
			UnlockedItems.Add(item.AsString());
	}

	public static void Save()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("save", "total_dna", TotalDna);

		var arr = new Godot.Collections.Array();
		foreach (var item in UnlockedItems)
			arr.Add(item);
		cfg.SetValue("save", "unlocked_items", arr);

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

	public static void UnlockItem(string itemId)
	{
		UnlockedItems.Add(itemId);
		Save();
	}

	public static bool IsUnlocked(string itemId)
	{
		return UnlockedItems.Contains(itemId);
	}
	public static void Reset()
{
	TotalDna      = 0;
	UnlockedItems = new HashSet<string> { "Anticorpos" };
	Save();
	// Limpa preferências do loadout para não aparecer itens inválidos
	UpgradeManager.Instance?.ApplyLoadout(
		new System.Collections.Generic.List<UpgradeId>(),
		new System.Collections.Generic.List<UpgradeId>()
	);
}
}
