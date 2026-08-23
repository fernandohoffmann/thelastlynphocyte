using Godot;

public static class IconAtlas
{
	private static Texture2D Load(string name)
	{
		string path = $"res://Assets/Icones/{name}.png";
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		GD.PrintErr($"[IconAtlas] Ícone não encontrado: {path}");
		return null;
	}

	public static Texture2D Get(UpgradeId id) => id switch
	{
		UpgradeId.Anticorpos            => Load("anticorpos"),
		UpgradeId.AnticorposGuiados     => Load("anticorposguiados"),
		UpgradeId.ProteinaComplemento   => Load("proteinacomplemento"),
		UpgradeId.TentaculoFagocitario  => Load("tentaculo"),
		UpgradeId.TempestandeCitocinas  => Load("citocinas"),
		UpgradeId.Macrofago             => Load("macrofago"),
		UpgradeId.SistemaImuneReforcado => Load("sistemimune"),
		UpgradeId.MetabolismoAcelerado  => Load("metabolismoacelerado"),
		UpgradeId.ProducaoAnticorpos    => Load("producaoanticorpos"),
		UpgradeId.CirculacaoAcelerada   => Load("circulacaoacelerada"),
		UpgradeId.SinteseDna            => Load("sintesedna"),
		UpgradeId.Antibiotico           => Load("antibiotico"),
		UpgradeId.Antiviral             => Load("antiviral"),
		UpgradeId.HemoglobinaReativa    => Load("hemoglobinareativa"),
		UpgradeId.FatorCrescimento      => Load("fatorcrescimento"),
		UpgradeId.Quimiotaxia           => Load("quimiotaxia"),
		UpgradeId.HemoglobinaAprimorada => Load("hemoglobinaaprimorada"),
		UpgradeId.Plaquetas             => Load("plaquetas"),
		UpgradeId.Adrenalina            => Load("adrenalina"),
		_                               => null
	};
}
