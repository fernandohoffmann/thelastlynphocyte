// Codex.cs — registro das descobertas. Fase 1 (sistema circulatorio) apenas.
//
// Regra do documento: o jogador PRIMEIRO vive o fenomeno, depois recebe a
// explicacao. Por isso cada entrada tem um gatilho ligado a algo que realmente
// acontece no jogo — nada aqui desbloqueia "de graca".
using Godot;
using System.Collections.Generic;

public enum CodexId
{
	// Estruturas
	Veia, CorrenteSanguinea, Plasma, Tecido,
	// Celulas
	Linfocito, Hemacia, Plaqueta, Macrofago,
	// Defesa
	Anticorpo, AnticorpoGuiado, Complemento, Citocinas, Tentaculo, HemoglobinaReativa,
	// Ameacas
	Bacteria, Virus, Colonia, TrypanosomaCruzi,
	// Processos
	Circulacao, Fagocitose, Inflamacao, RespostaImune, Coagulacao,
	// Doencas
	DoencaChagas,
}

public enum CodexCategory { Estruturas, Celulas, Defesa, Ameacas, Processos, Doencas }

public class CodexEntry
{
	public CodexId       Id;
	public CodexCategory Category;
	public string        Name;
	public string        Trigger;   // o que o jogador faz pra desbloquear
	public string        Text;      // conteudo cientifico
}

public static class Codex
{
	/// Disparado quando algo novo e descoberto (usado pelo aviso na tela).
	public static event System.Action<CodexEntry> Discovered;

	private static readonly Dictionary<CodexId, CodexEntry> _entries = new();
	private static bool _hooked = false;

	public static IReadOnlyDictionary<CodexId, CodexEntry> Entries
	{
		get { EnsureBuilt(); return _entries; }
	}

	public static bool IsKnown(CodexId id) => SaveData.CodexUnlocked.Contains(id.ToString());

	/// Marca uma descoberta. Repetir nao faz nada — o aviso so aparece na 1a vez.
	public static void Discover(CodexId id)
	{
		EnsureBuilt();
		if (IsKnown(id)) return;
		if (!_entries.TryGetValue(id, out var entry)) return;

		SaveData.UnlockCodex(id.ToString());
		GD.Print($"[Codex] nova descoberta: {entry.Name}");
		Discovered?.Invoke(entry);
	}

	public static void Discover(params CodexId[] ids)
	{
		foreach (var id in ids) Discover(id);
	}

	public static List<CodexEntry> ByCategory(CodexCategory cat)
	{
		EnsureBuilt();
		var list = new List<CodexEntry>();
		foreach (var e in _entries.Values)
			if (e.Category == cat) list.Add(e);
		return list;
	}

	public static void Progress(CodexCategory cat, out int known, out int total)
	{
		known = 0; total = 0;
		foreach (var e in ByCategory(cat))
		{
			total++;
			if (IsKnown(e.Id)) known++;
		}
	}

	public static void ProgressTotal(out int known, out int total)
	{
		EnsureBuilt();
		known = 0; total = _entries.Count;
		foreach (var e in _entries.Values)
			if (IsKnown(e.Id)) known++;
	}

	// ══════════════════════════════════════════════════════════════════════
	// Gatilhos automaticos: o Codex se inscreve nos eventos que o jogo JA tem,
	// entao ameacas e nivel nao precisam de nenhuma linha espalhada por ai.
	// ══════════════════════════════════════════════════════════════════════
	public static void Hook()
	{
		if (_hooked) return;
		_hooked = true;

		Enemy.EnemyDied += OnEnemyDied;
	}

	private static void OnEnemyDied(Enemy e)
	{
		if (e is ChagasBoss) Discover(CodexId.TrypanosomaCruzi, CodexId.DoencaChagas);
		else if (e is Colony) Discover(CodexId.Colonia);
		else if (e.Type == EnemyType.Virus) Discover(CodexId.Virus);
		else Discover(CodexId.Bacteria);
	}

	/// Mapeia as armas/passivas do loadout para as entradas correspondentes.
	/// Chamado pelo UpgradeManager quando o item entra em jogo.
	public static void OnUpgradeEquipped(UpgradeId id)
	{
		switch (id)
		{
			case UpgradeId.Anticorpos:            Discover(CodexId.Anticorpo); break;
			case UpgradeId.AnticorposGuiados:     Discover(CodexId.AnticorpoGuiado); break;
			case UpgradeId.ProteinaComplemento:   Discover(CodexId.Complemento); break;
			case UpgradeId.TempestandeCitocinas:  Discover(CodexId.Citocinas, CodexId.Inflamacao); break;
			case UpgradeId.TentaculoFagocitario:  Discover(CodexId.Tentaculo); break;
			case UpgradeId.Macrofago:             Discover(CodexId.Macrofago); break;
			case UpgradeId.Plaquetas:             Discover(CodexId.Plaqueta, CodexId.Coagulacao); break;
			case UpgradeId.HemoglobinaReativa:
			case UpgradeId.HemoglobinaAprimorada: Discover(CodexId.HemoglobinaReativa); break;
		}
	}

	// ══════════════════════════════════════════════════════════════════════
	private static void EnsureBuilt()
	{
		if (_entries.Count > 0) return;

		Add(CodexId.Veia, CodexCategory.Estruturas, "Veia",
			"Entrar em uma fase do sistema circulatorio.",
			"Veias sao vasos que levam o sangue de volta ao coracao. Diferente das " +
			"arterias, trabalham sob pressao baixa e muitas delas possuem valvulas " +
			"internas que impedem o sangue de voltar no sentido errado.\n\n" +
			"No jogo, as paredes do mapa sao as paredes da propria veia: voce se " +
			"move dentro do vaso, nao em volta dele.");

		Add(CodexId.CorrenteSanguinea, CodexCategory.Estruturas, "Corrente sanguinea",
			"Entrar em uma fase do sistema circulatorio.",
			"O sangue circula continuamente por uma rede fechada de vasos. Essa " +
			"corrente e o que distribui oxigenio e nutrientes e, ao mesmo tempo, o " +
			"caminho pelo qual as celulas de defesa alcancam qualquer parte do corpo.\n\n" +
			"E tambem a via que muitos agentes infecciosos usam para se espalhar.");

		Add(CodexId.Plasma, CodexCategory.Estruturas, "Plasma",
			"Entrar em uma fase do sistema circulatorio.",
			"Parte liquida do sangue, composta em sua maior parte por agua. Nele " +
			"estao dissolvidos nutrientes, hormonios, proteinas de defesa e " +
			"residuos do metabolismo.\n\n" +
			"E o plasma que carrega os anticorpos ate o alvo.");

		Add(CodexId.Tecido, CodexCategory.Estruturas, "Tecido vascular",
			"Entrar em uma fase do sistema circulatorio.",
			"A parede de um vaso nao e uma casca simples: tem camadas de celulas, " +
			"fibras elasticas e musculo liso. Essa elasticidade e o que permite ao " +
			"vaso acompanhar as variacoes de pressao a cada batimento.");

		Add(CodexId.Linfocito, CodexCategory.Celulas, "Linfocito",
			"Comecar uma partida.",
			"Tipo de leucocito responsavel pela resposta imunologica especifica. " +
			"Diferente das defesas gerais, o linfocito reconhece alvos determinados " +
			"e guarda memoria deles.\n\n" +
			"E o personagem que voce controla.");

		Add(CodexId.Hemacia, CodexCategory.Celulas, "Hemacia",
			"Comecar uma partida.",
			"Tambem chamada de globulo vermelho ou eritrocito. Transporta oxigenio " +
			"gracas a hemoglobina, a proteina que da ao sangue a cor vermelha.\n\n" +
			"Tem formato de disco achatado no centro, o que aumenta a area de troca. " +
			"A hemacia humana madura perde o nucleo, o que abre mais espaco para " +
			"hemoglobina.");

		Add(CodexId.Plaqueta, CodexCategory.Celulas, "Plaqueta",
			"Equipar a passiva Plaquetas.",
			"Fragmentos celulares que circulam no sangue. Quando um vaso se rompe, " +
			"elas se acumulam no local e formam um tampao que reduz a perda de " +
			"sangue enquanto o reparo acontece.");

		Add(CodexId.Macrofago, CodexCategory.Celulas, "Macrofago",
			"Equipar a arma Macrofago.",
			"Celula de defesa de grande porte que engloba e digere particulas, " +
			"restos celulares e microrganismos. Alem de destruir o invasor, " +
			"apresenta pedacos dele a outras celulas do sistema imune.");

		Add(CodexId.Anticorpo, CodexCategory.Defesa, "Anticorpo",
			"Equipar a arma Anticorpos.",
			"Proteina produzida em resposta a um agente estranho. Liga-se a alvos " +
			"especificos, neutralizando-os ou marcando-os para que outras celulas " +
			"de defesa os destruam.");

		Add(CodexId.AnticorpoGuiado, CodexCategory.Defesa, "Reconhecimento imunologico",
			"Equipar a arma Anticorpos Guiados.",
			"O anticorpo nao acerta qualquer coisa: ele se encaixa em uma regiao " +
			"especifica do alvo, chamada antigeno, como uma chave numa fechadura.\n\n" +
			"E essa especificidade que a versao guiada representa.");

		Add(CodexId.Complemento, CodexCategory.Defesa, "Sistema complemento",
			"Equipar a arma Proteina do Complemento.",
			"Conjunto de proteinas do plasma que age em cascata: uma ativa a " +
			"seguinte. O resultado pode ser perfurar a membrana do invasor, marca-lo " +
			"para ser engolido ou atrair mais celulas de defesa ao local.");

		Add(CodexId.Citocinas, CodexCategory.Defesa, "Citocinas",
			"Equipar a arma Tempestade de Citocinas.",
			"Moleculas que as celulas usam para se comunicar, coordenando a resposta " +
			"imune. Em excesso, porem, provocam uma reacao inflamatoria " +
			"desproporcional que danifica o proprio organismo.");

		Add(CodexId.Tentaculo, CodexCategory.Defesa, "Pseudopode",
			"Equipar a arma Tentaculo Fagocitario.",
			"Projecao da membrana que algumas celulas de defesa emitem para se mover " +
			"e para envolver aquilo que vao engolir. E o primeiro passo da fagocitose.");

		Add(CodexId.HemoglobinaReativa, CodexCategory.Defesa, "Hemoglobina",
			"Equipar uma passiva de Hemoglobina.",
			"Proteina das hemacias que se liga ao oxigenio nos pulmoes e o libera " +
			"nos tecidos. Contem ferro, e e a ele que o sangue deve a cor.\n\n" +
			"A versao reativa do jogo e uma licenca ficcional.");

		Add(CodexId.Bacteria, CodexCategory.Ameacas, "Bacteria",
			"Derrotar uma bacteria.",
			"Organismo unicelular sem nucleo definido. Tem estrutura propria e se " +
			"reproduz sozinha, por divisao celular.\n\n" +
			"Por serem celulas completas, sao vulneraveis a antibioticos — " +
			"substancias que atacam estruturas que so elas possuem.");

		Add(CodexId.Virus, CodexCategory.Ameacas, "Virus",
			"Derrotar um virus.",
			"Nao e uma celula: e material genetico dentro de uma capsula de " +
			"proteina. Sozinho nao produz energia nem se reproduz.\n\n" +
			"Precisa invadir uma celula hospedeira e usar a maquinaria dela para se " +
			"multiplicar. Por isso antibioticos nao funcionam contra virus.");

		Add(CodexId.Colonia, CodexCategory.Ameacas, "Colonia bacteriana",
			"Destruir uma colonia.",
			"Agrupamento de bacterias que se multiplicaram a partir de poucas " +
			"celulas iniciais. Reunidas, resistem melhor as defesas do organismo " +
			"e liberam mais toxinas no local.");

		Add(CodexId.TrypanosomaCruzi, CodexCategory.Ameacas, "Trypanosoma cruzi",
			"Derrotar o chefe da fase.",
			"Parasita unicelular, nem bacteria nem virus. Move-se pelo sangue com um " +
			"flagelo e consegue invadir celulas do hospedeiro, onde se multiplica.\n\n" +
			"E o agente causador da Doenca de Chagas. A forma monstruosa que aparece " +
			"no jogo e ficcional.");

		Add(CodexId.Circulacao, CodexCategory.Processos, "Circulacao sanguinea",
			"Comecar uma partida.",
			"O coracao bombeia o sangue por um circuito fechado. Ele sai carregado de " +
			"oxigenio, entrega esse oxigenio aos tecidos, recolhe gas carbonico e " +
			"retorna pelas veias para ser oxigenado de novo.");

		Add(CodexId.Fagocitose, CodexCategory.Processos, "Fagocitose",
			"Ver um macrofago engolir um inimigo.",
			"Processo em que uma celula envolve uma particula com sua membrana, " +
			"formando uma bolsa interna, e a digere com enzimas.\n\n" +
			"E como o organismo remove tanto invasores quanto restos das proprias " +
			"celulas.");

		Add(CodexId.Inflamacao, CodexCategory.Processos, "Inflamacao",
			"Equipar a arma Tempestade de Citocinas.",
			"Resposta do organismo a uma agressao. Os vasos da regiao se dilatam e " +
			"ficam mais permeaveis, o que traz mais sangue e mais celulas de defesa " +
			"ao local — e explica o calor, o inchaco e a vermelhidao.");

		Add(CodexId.RespostaImune, CodexCategory.Processos, "Resposta imunologica",
			"Subir de nivel durante uma partida.",
			"A defesa acontece em camadas: primeiro a resposta geral, rapida e " +
			"pouco seletiva; depois a resposta especifica, mais lenta porem " +
			"dirigida ao invasor exato.\n\n" +
			"A segunda deixa memoria, e e por isso que o corpo reage mais rapido " +
			"num proximo encontro.");

		Add(CodexId.Coagulacao, CodexCategory.Processos, "Coagulacao",
			"Equipar a passiva Plaquetas.",
			"Sequencia de reacoes que transforma o sangue liquido em um coagulo " +
			"solido. As plaquetas formam o tampao inicial e uma rede de fibrina o " +
			"reforca, selando a lesao do vaso.");

		Add(CodexId.DoencaChagas, CodexCategory.Doencas, "Doenca de Chagas",
			"Derrotar o chefe da fase.",
			"Doenca causada pelo protozoario Trypanosoma cruzi. A transmissao " +
			"classica ocorre pelas fezes do inseto conhecido como barbeiro, que " +
			"entram em contato com a pele lesionada ou com mucosas.\n\n" +
			"Tem uma fase aguda, muitas vezes discreta, e pode evoluir anos depois " +
			"para uma fase cronica que afeta principalmente o coracao e o sistema " +
			"digestorio.\n\n" +
			"A prevencao passa pelo controle do inseto vetor e pela melhoria das " +
			"condicoes de moradia.");
	}

	private static void Add(CodexId id, CodexCategory cat, string name, string trigger, string text)
	{
		_entries[id] = new CodexEntry
		{
			Id = id, Category = cat, Name = name, Trigger = trigger, Text = text
		};
	}
}
