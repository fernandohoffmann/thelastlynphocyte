using Godot;
using System.Collections.Generic;

public enum UpgradeId
{
	Anticorpos,
	AnticorposGuiados,
	ProteinaComplemento,
	TentaculoFagocitario,
	TempestandeCitocinas,
	Macrofago,
	SistemaImuneReforcado,
	MetabolismoAcelerado,
	ProducaoAnticorpos,
	CirculacaoAcelerada,
	SinteseDna,
	Antibiotico,
	Antiviral,
	HemoglobinaReativa,
	FatorCrescimento,
	Quimiotaxia,
	HemoglobinaAprimorada,
	Plaquetas,
	Adrenalina,
}

public class UpgradeOption
{
	public UpgradeId Id;
	public string    Name;
	public string    Description;
	public string    LevelDesc;
	public bool      IsWeapon;
	public int       DnaCost;
	public int       CurrentLevel;
	public int       TreeIndex;
}

public partial class UpgradeManager : Node
{
	public static UpgradeManager Instance { get; private set; }

	public const int MAX_LEVEL    = 5;
	public const int MAX_WEAPONS  = 3;
	public const int MAX_PASSIVES = 5;

	private Dictionary<UpgradeId, UpgradeOption> _pool = new();

	public List<UpgradeId> EquippedWeapons    { get; private set; } = new();
	public List<UpgradeId> EquippedPassives   { get; private set; } = new();
	public List<UpgradeId> LoadoutPreferences { get; private set; } = new();

	private bool _firstLevelUp = true;

	public override void _Ready()
	{
		if (Instance != null) { QueueFree(); return; }
		Instance = this;
		InitPool();
	}

	private void InitPool()
	{
		Add(new UpgradeOption { Id = UpgradeId.Anticorpos, Name = "Anticorpos",
			Description = "Proteínas em Y produzidas pelo sistema imune que reconhecem e neutralizam patógenos. Dispara projéteis em direção ao cursor.",
			LevelDesc = "+dano e +velocidade de disparo a cada nível.", IsWeapon = true, DnaCost = 0, TreeIndex = 0 });

		Add(new UpgradeOption { Id = UpgradeId.AnticorposGuiados, Name = "Anticorpos Guiados",
			Description = "Anticorpos monoclonais altamente específicos, projetados para rastrear antígenos com precisão. Projéteis que perseguem inimigos automaticamente.",
			LevelDesc = "+dano e -cooldown a cada nível.", IsWeapon = true, DnaCost = 20, TreeIndex = 1 });

		Add(new UpgradeOption { Id = UpgradeId.ProteinaComplemento, Name = "Proteína de Complemento",
			Description = "Sistema de proteínas que se ativa em cascata, amplificando a resposta imune. Raios que saltam de inimigo em inimigo.",
			LevelDesc = "+dano e +saltos a cada nível.", IsWeapon = true, DnaCost = 50, TreeIndex = 2 });

		Add(new UpgradeOption { Id = UpgradeId.TentaculoFagocitario, Name = "Tentáculo Fagocitário",
			Description = "Pseudópodos usados por fagócitos para englobar e destruir patógenos por contato direto. Golpeia inimigos próximos.",
			LevelDesc = "+tentáculos e +alcance a cada nível.", IsWeapon = true, DnaCost = 90, TreeIndex = 3 });

		Add(new UpgradeOption { Id = UpgradeId.TempestandeCitocinas, Name = "Tempestade de Citocinas",
			Description = "Resposta inflamatória excessiva onde citocinas são liberadas em massa. Nuvem de veneno periódica que causa dano por segundo.",
			LevelDesc = "+frequência e +dano por segundo a cada nível.", IsWeapon = true, DnaCost = 140, TreeIndex = 4 });

		Add(new UpgradeOption { Id = UpgradeId.Macrofago, Name = "Macrófago",
			Description = "Célula do sistema imune que engole e digere patógenos por fagocitose. Aliado lento que engole inimigos — ao engolir 3, explode causando dano em área.",
			LevelDesc = "+vida, +velocidade e +dano da explosão a cada nível.", IsWeapon = true, DnaCost = 200, TreeIndex = 5 });

		Add(new UpgradeOption { Id = UpgradeId.SistemaImuneReforcado, Name = "Sist. Imune Reforçado",
			Description = "Produção elevada de leucócitos e células de defesa, aumentando a resistência geral do organismo. +5 HP máximo por nível.",
			LevelDesc = "+5 HP máximo a cada nível.", IsWeapon = false, DnaCost = 20, TreeIndex = 0 });

		Add(new UpgradeOption { Id = UpgradeId.MetabolismoAcelerado, Name = "Metabolismo Acelerado",
			Description = "Aumento da taxa metabólica celular, acelerando a síntese de proteínas e regeneração de recursos. Reduz cooldowns de todas as armas.",
			LevelDesc = "-15% de cooldown a cada nível.", IsWeapon = false, DnaCost = 40, TreeIndex = 1 });

		Add(new UpgradeOption { Id = UpgradeId.ProducaoAnticorpos, Name = "Produção de Anticorpos",
			Description = "Plasmócitos hiperstimuados produzem anticorpos em ritmo acelerado. Aumenta a velocidade de ataque.",
			LevelDesc = "+15% de velocidade de ataque a cada nível.", IsWeapon = false, DnaCost = 70, TreeIndex = 2 });

		Add(new UpgradeOption { Id = UpgradeId.CirculacaoAcelerada, Name = "Circulação Acelerada",
			Description = "Aumento do débito cardíaco e vasodilatação, transportando oxigênio e nutrientes mais rapidamente para os tecidos. Aumenta a velocidade de movimento.",
			LevelDesc = "+12% de velocidade de movimento a cada nível.", IsWeapon = false, DnaCost = 110, TreeIndex = 3 });

		Add(new UpgradeOption { Id = UpgradeId.SinteseDna, Name = "Síntese de DNA",
			Description = "Replicação acelerada do material genético, aumentando a eficiência de coleta de informação dos patógenos eliminados.",
			LevelDesc = "+0.5 DNA por inimigo a cada nível.", IsWeapon = false, DnaCost = 160, TreeIndex = 4 });

		Add(new UpgradeOption { Id = UpgradeId.Antibiotico, Name = "Antibiótico",
			Description = "Substância que inibe o crescimento ou destrói bactérias ao interferir na síntese da parede celular ou de proteínas.",
			LevelDesc = "+25% de dano vs Bactérias a cada nível.", IsWeapon = false, DnaCost = 220, TreeIndex = 5 });

		Add(new UpgradeOption { Id = UpgradeId.Antiviral, Name = "Antiviral",
			Description = "Composto que impede a replicação viral ao bloquear enzimas essenciais do ciclo de vida do vírus.",
			LevelDesc = "+25% de dano vs Vírus a cada nível.", IsWeapon = false, DnaCost = 290, TreeIndex = 6 });

		Add(new UpgradeOption { Id = UpgradeId.HemoglobinaReativa, Name = "Hemoglobina Reativa",
			Description = "Variante instável da hemoglobina que libera radicais livres ao contato com patógenos. Ao matar um inimigo, explode causando dano em área.",
			LevelDesc = "+dano e +raio da explosão a cada nível.", IsWeapon = false, DnaCost = 370, TreeIndex = 7 });

		Add(new UpgradeOption { Id = UpgradeId.FatorCrescimento, Name = "Fator de Crescimento",
			Description = "Proteínas sinalizadoras que estimulam a proliferação celular, acelerando o desenvolvimento do organismo. +25% de XP ganho.",
			LevelDesc = "+25% de XP ganho a cada nível.", IsWeapon = false, DnaCost = 460, TreeIndex = 8 });

		Add(new UpgradeOption { Id = UpgradeId.Quimiotaxia, Name = "Quimiotaxia",
			Description = "Movimento direcionado de células imunes em resposta a gradientes químicos. Itens no chão são atraídos para você.",
			LevelDesc = "+raio de atração a cada nível.", IsWeapon = false, DnaCost = 560, TreeIndex = 9 });

		Add(new UpgradeOption { Id = UpgradeId.HemoglobinaAprimorada, Name = "Hemoglobina Aprimorada",
			Description = "Hemoglobina com maior afinidade pelo oxigênio, otimizando a recuperação tecidual e regeneração celular.",
			LevelDesc = "+25% de cura dos corações a cada nível.", IsWeapon = false, DnaCost = 670, TreeIndex = 10 });

		Add(new UpgradeOption { Id = UpgradeId.Plaquetas, Name = "Plaquetas",
			Description = "Fragmentos celulares que respondem imediatamente a lesões, formando um tampão temporário que impede dano adicional.",
			LevelDesc = "+0.5s de imunidade a cada nível.", IsWeapon = false, DnaCost = 790, TreeIndex = 11 });

		Add(new UpgradeOption { Id = UpgradeId.Adrenalina, Name = "Adrenalina",
			Description = "Hormônio liberado em situações de estresse que aumenta frequência cardíaca, força muscular e velocidade de reação.",
			LevelDesc = "+dano e +velocidade baseados no HP perdido a cada nível.", IsWeapon = false, DnaCost = 920, TreeIndex = 12 });
	}

	private void Add(UpgradeOption opt) => _pool[opt.Id] = opt;

	public void ResetRunLevels()
	{
		foreach (var kv in _pool)
			kv.Value.CurrentLevel = 0;
		_firstLevelUp = true;
	}

	public void ApplyLoadout(List<UpgradeId> weapons, List<UpgradeId> passives)
	{
		ResetRunLevels();
		LoadoutPreferences.Clear();
		foreach (var id in weapons)  LoadoutPreferences.Add(id);
		foreach (var id in passives) LoadoutPreferences.Add(id);
		EquippedWeapons  = new List<UpgradeId>();
		EquippedPassives = new List<UpgradeId>();
	}

	public List<UpgradeOption> GetLevelUpOptions(int count = 3)
	{
		var result = new List<UpgradeOption>();
		var used   = new HashSet<UpgradeId>();

		// 1. Prioridade: itens do loadout desbloqueados ainda não adquiridos
		foreach (var id in LoadoutPreferences)
		{
			if (result.Count >= count) break;
			if (!_pool.TryGetValue(id, out var opt)) continue;
			if (opt.CurrentLevel >= MAX_LEVEL) continue;
			if (used.Contains(id)) continue;
			if (!SaveData.IsUnlocked(id.ToString())) continue;
			result.Add(opt);
			used.Add(id);
		}

		// 2. Complemento: itens já equipados que podem subir de nível
		if (result.Count < count)
		{
			var upgradeable = new List<UpgradeOption>();
			foreach (var id in EquippedWeapons)
			{
				if (!_pool.TryGetValue(id, out var opt)) continue;
				if (opt.CurrentLevel > 0 && opt.CurrentLevel < MAX_LEVEL && !used.Contains(id))
					upgradeable.Add(opt);
			}
			foreach (var id in EquippedPassives)
			{
				if (!_pool.TryGetValue(id, out var opt)) continue;
				if (opt.CurrentLevel > 0 && opt.CurrentLevel < MAX_LEVEL && !used.Contains(id))
					upgradeable.Add(opt);
			}
			for (int i = upgradeable.Count - 1; i > 0; i--)
			{
				int j = (int)(GD.Randi() % (uint)(i + 1));
				(upgradeable[i], upgradeable[j]) = (upgradeable[j], upgradeable[i]);
			}
			foreach (var opt in upgradeable)
			{
				if (result.Count >= count) break;
				result.Add(opt);
				used.Add(opt.Id);
			}
		}

		// 3. Fallback: apenas itens DESBLOQUEADOS no Arsenal
		if (result.Count < count)
		{
			var all = new List<UpgradeOption>();
			foreach (var opt in _pool.Values)
				if (SaveData.IsUnlocked(opt.Id.ToString()))
					all.Add(opt);

			for (int i = all.Count - 1; i > 0; i--)
			{
				int j = (int)(GD.Randi() % (uint)(i + 1));
				(all[i], all[j]) = (all[j], all[i]);
			}
			foreach (var opt in all)
			{
				if (result.Count >= count) break;
				if (opt.CurrentLevel >= MAX_LEVEL) continue;
				if (used.Contains(opt.Id)) continue;
				result.Add(opt);
				used.Add(opt.Id);
			}
		}

		// ── Garante pelo menos 1 ARMA na primeira escolha da run ──
		if (_firstLevelUp)
		{
			_firstLevelUp = false;

			bool hasWeapon = false;
			foreach (var opt in result)
				if (opt.IsWeapon) { hasWeapon = true; break; }

			if (!hasWeapon && result.Count > 0)
			{
				UpgradeOption weaponToForce = null;

				foreach (var id in LoadoutPreferences)
					if (_pool.TryGetValue(id, out var opt) && opt.IsWeapon
						&& SaveData.IsUnlocked(id.ToString()))
						{ weaponToForce = opt; break; }

				if (weaponToForce == null)
					foreach (var opt in _pool.Values)
						if (opt.IsWeapon && SaveData.IsUnlocked(opt.Id.ToString()))
							{ weaponToForce = opt; break; }

				if (weaponToForce != null)
					result[0] = weaponToForce;
			}
		}

		return result;
	}

	public void ApplyUpgrade(UpgradeId id, bool forceLevel1 = false)
	{
		if (!_pool.TryGetValue(id, out var opt)) return;

		if (forceLevel1)
			opt.CurrentLevel = 1;
		else
			opt.CurrentLevel = Mathf.Min(opt.CurrentLevel + 1, MAX_LEVEL);

		var p = Player.Instance;
		if (p == null) return;

		switch (id)
		{
			case UpgradeId.Anticorpos:
				GetOrAdd<WeaponAnticorpos>(p, "WeaponAnticorpos").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.AnticorposGuiados:
				GetOrAdd<WeaponAnticorposGuiados>(p, "WeaponAnticorposGuiados").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.ProteinaComplemento:
				GetOrAdd<WeaponProteinaComplemento>(p, "WeaponProteinaComplemento").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.TentaculoFagocitario:
				GetOrAdd<WeaponTentaculo>(p, "WeaponTentaculo").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.TempestandeCitocinas:
				GetOrAdd<WeaponCitocinas>(p, "WeaponCitocinas").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.Macrofago:
				GetOrAdd<WeaponMacrofago>(p, "WeaponMacrofago").UpgradeLevel();
				if (!EquippedWeapons.Contains(id)) EquippedWeapons.Add(id);
				GetDisplay()?.RegisterWeapon(id.ToString(), id);
				GetDisplay()?.UpdateWeaponLevel(id.ToString(), opt.CurrentLevel);
				break;

			case UpgradeId.SistemaImuneReforcado:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.AddMaxHp(5);
				break;

			case UpgradeId.MetabolismoAcelerado:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.CooldownReduction = Mathf.Max(0.3f, p.CooldownReduction - 0.15f);
				break;

			case UpgradeId.ProducaoAnticorpos:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.AttackSpeedBonus += 0.15f;
				break;

			case UpgradeId.CirculacaoAcelerada:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.MoveSpeed += p.MoveSpeed * 0.12f;
				break;

			case UpgradeId.SinteseDna:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.DnaBonus += 0.5f;
				break;

			case UpgradeId.Antibiotico:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.BonusDmgVsBacteria += 0.25f;
				break;

			case UpgradeId.Antiviral:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.BonusDmgVsVirus += 0.25f;
				break;

			case UpgradeId.HemoglobinaReativa:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.HasHemoglobinaReativa = true;
				p.HemoglobinDamage     += 5f;
				p.HemoglobinRadius     += 20f;
				break;

			case UpgradeId.FatorCrescimento:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.XpBonus += 0.25f;
				break;

			case UpgradeId.Quimiotaxia:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.MagnetRadius += 100f;
				break;

			case UpgradeId.HemoglobinaAprimorada:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.HealBonus += 0.25f;
				break;

			case UpgradeId.Plaquetas:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.HasPlaquetas     = true;
				p.PlaquetaDuration += 0.5f;
				break;

			case UpgradeId.Adrenalina:
				if (!EquippedPassives.Contains(id)) EquippedPassives.Add(id);
				GetPassiveDisplay()?.RegisterPassive(opt.Name, opt.Description, id);
				p.AdrenalineDamage += 0.1f;
				p.AdrenalineSpeed  += 0.05f;
				break;
		}
	}

	private T GetOrAdd<T>(Node parent, string nodeName) where T : Node2D, new()
	{
		var existing = parent.GetNodeOrNull<T>(nodeName);
		if (existing != null) return existing;
		var node = new T { Name = nodeName };
		parent.AddChild(node);
		return node;
	}

	private WeaponDisplay GetDisplay()
	{
		return Engine.GetMainLoop() is SceneTree tree
			? tree.CurrentScene?.GetNodeOrNull<WeaponDisplay>("WeaponDisplay")
			: null;
	}

	private PassiveDisplay GetPassiveDisplay()
	{
		return Engine.GetMainLoop() is SceneTree tree
			? tree.CurrentScene?.GetNodeOrNull<PassiveDisplay>("PassiveDisplay")
			: null;
	}

	public UpgradeOption GetUpgrade(UpgradeId id) => _pool.TryGetValue(id, out var v) ? v : null;
	public Dictionary<UpgradeId, UpgradeOption> GetPool() => _pool;

	public List<UpgradeOption> GetUnlockedWeapons()
	{
		var result = new List<UpgradeOption>();
		var order  = new[] { UpgradeId.Anticorpos, UpgradeId.AnticorposGuiados,
			UpgradeId.ProteinaComplemento, UpgradeId.TentaculoFagocitario,
			UpgradeId.TempestandeCitocinas, UpgradeId.Macrofago };
		foreach (var id in order)
			if (SaveData.IsUnlocked(id.ToString()) && _pool.TryGetValue(id, out var opt))
				result.Add(opt);
		return result;
	}

	public List<UpgradeOption> GetUnlockedPassives()
	{
		var result = new List<UpgradeOption>();
		var order  = new[] { UpgradeId.SistemaImuneReforcado, UpgradeId.MetabolismoAcelerado,
			UpgradeId.ProducaoAnticorpos, UpgradeId.CirculacaoAcelerada,
			UpgradeId.SinteseDna, UpgradeId.Antibiotico, UpgradeId.Antiviral,
			UpgradeId.HemoglobinaReativa, UpgradeId.FatorCrescimento,
			UpgradeId.Quimiotaxia, UpgradeId.HemoglobinaAprimorada,
			UpgradeId.Plaquetas, UpgradeId.Adrenalina };
		foreach (var id in order)
			if (SaveData.IsUnlocked(id.ToString()) && _pool.TryGetValue(id, out var opt))
				result.Add(opt);
		return result;
	}
}
