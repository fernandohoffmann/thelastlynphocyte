// UiPolish.cs — corrige o tamanho das fontes da UI em todo o jogo.
//
// A PressStart2P e uma fonte de pixel desenhada numa grade de 8px. Em tamanhos
// que nao sao multiplos de 8 (9, 10, 12, 14...) cada letra e reescalada por um
// fator quebrado, entao umas colunas de pixel ficam com 1px e outras com 2px.
// O resultado e exatamente aquela sensacao de texto "esticado"/borrado.
//
// Este autoload arredonda todo font_size da UI pro multiplo de 8 mais proximo,
// sem precisar editar os 12 scripts de tela um por um.
//
// Registrar em: Project > Project Settings > Autoload > adicionar este script.
using Godot;

public partial class UiPolish : Node
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// static readonly (e nao const): com const o compilador resolve o if em
	// tempo de compilacao e acusa o return como codigo inalcancavel
	public static readonly bool ENABLED = true;
	public const int  GRID    = 8;    // grade da fonte de pixel
	public const bool VERBOSE = true; // imprime o que foi alterado
	// ──────────────────────────────────────────────────────────────────────

	private static readonly StringName[] FONT_SIZE_KEYS =
	{
		"font_size",
		"title_font_size",
		"normal_font_size",
		"bold_font_size",
	};

	public override void _Ready()
	{
		if (!ENABLED) return;

		GetTree().NodeAdded += OnNodeAdded;

		// tambem arruma o que ja estava na arvore quando o autoload subiu
		CallDeferred(MethodName.FixExisting);
	}

	public override void _ExitTree()
	{
		if (ENABLED && GetTree() != null)
			GetTree().NodeAdded -= OnNodeAdded;
	}

	private void FixExisting()
	{
		var root = GetTree()?.Root;
		if (root != null) FixRecursive(root);
	}

	private void FixRecursive(Node node)
	{
		if (node is Control c) FixControl(c);
		foreach (var child in node.GetChildren())
			FixRecursive(child);
	}

	private void OnNodeAdded(Node node)
	{
		if (node is Control c)
			CallDeferred(MethodName.FixControl, c); // o override costuma vir depois do AddChild
	}

	private void FixControl(Control c)
	{
		if (!IsInstanceValid(c)) return;

		foreach (var key in FONT_SIZE_KEYS)
		{
			if (!c.HasThemeFontSizeOverride(key)) continue;

			int cur     = c.GetThemeFontSize(key);
			int snapped = Snap(cur);

			if (snapped != cur)
			{
				c.AddThemeFontSizeOverride(key, snapped);
				if (VERBOSE)
					GD.Print($"[UiPolish] {c.GetPath()} : {key} {cur} -> {snapped}");
			}
		}
	}

	// Multiplo de GRID mais proximo. Empate vai pra baixo (nunca estoura layout).
	private static int Snap(int size)
	{
		if (size <= GRID) return GRID;

		int lower = (size / GRID) * GRID;
		int upper = lower + GRID;

		return (size - lower) > (upper - size) ? upper : lower;
	}
}
