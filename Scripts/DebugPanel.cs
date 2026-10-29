// DebugPanel.cs — painel de teste. Abre e fecha com F1.
//
// Ponha ENABLED em false (ou nao adicione o no no GameScene) pra tirar da build.
using Godot;

public partial class DebugPanel : CanvasLayer
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	public static readonly bool ENABLED = true;
	private const Key TOGGLE_KEY = Key.F1;
	// ──────────────────────────────────────────────────────────────────────

	/// Quantos level-ups ainda faltam mostrar. O GameScene consome esta fila.
	public static int PendingLevelUps = 0;

	private PanelContainer _panel;
	private SpinBox        _levelBox;
	private Label          _status;

	public override void _Ready()
	{
		Layer   = 10;   // acima de tudo
		Visible = ENABLED;

		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(16));
		_panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_panel.Position = new Vector2(24, 120);
		_panel.Visible  = false;
		AddChild(_panel);

		var box = new VBoxContainer();
		box.CustomMinimumSize = new Vector2(360, 0);
		box.AddThemeConstantOverride("separation", 10);
		_panel.AddChild(box);

		var title = MenuStyle.Body("DEBUG  [F1]", MenuStyle.FONT_BODY);
		title.AddThemeColorOverride("font_color", MenuStyle.ACCENT);
		box.AddChild(title);
		box.AddChild(new HSeparator());

		// ── Nivel ────────────────────────────────────────────────
		box.AddChild(MenuStyle.Body("IR PARA O NIVEL", MenuStyle.FONT_SMALL));

		var levelRow = new HBoxContainer();
		levelRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(levelRow);

		_levelBox = new SpinBox
		{
			MinValue = 1,
			MaxValue = 60,
			Value    = 20,
			Step     = 1,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		levelRow.AddChild(_levelBox);

		var goBtn = MenuStyle.MakeButton("APLICAR", 40f, MenuStyle.FONT_SMALL);
		goBtn.CustomMinimumSize = new Vector2(140, 40);
		goBtn.Pressed += () => JumpToLevel((int)_levelBox.Value);
		levelRow.AddChild(goBtn);

		var oneRow = new HBoxContainer();
		oneRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(oneRow);
		AddSmall(oneRow, "+1 NIVEL",  () => GrantLevels(1));
		AddSmall(oneRow, "+5 NIVEIS", () => GrantLevels(5));

		box.AddChild(new HSeparator());

		// ── Tempo ────────────────────────────────────────────────
		box.AddChild(MenuStyle.Body("TEMPO", MenuStyle.FONT_SMALL));

		var timeRow = new HBoxContainer();
		timeRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(timeRow);
		AddSmall(timeRow, "+1 MIN",  () => SkipTime(60f));
		AddSmall(timeRow, "+5 MIN",  () => SkipTime(300f));
		AddSmall(timeRow, "-1 MIN",  () => SkipTime(-60f));

		box.AddChild(new HSeparator());

		// ── Atalhos uteis ────────────────────────────────────────
		var miscRow = new HBoxContainer();
		miscRow.AddThemeConstantOverride("separation", 8);
		box.AddChild(miscRow);
		AddSmall(miscRow, "MATAR TODOS", KillAll);
		AddSmall(miscRow, "+500 DNA",    () => { GameManager.Instance?.GainDna(500); Report("+500 DNA"); });
		AddSmall(miscRow, "CURAR",       () => { Player.Instance?.Heal(999); Report("vida cheia"); });

		_status = MenuStyle.Body("", MenuStyle.FONT_SMALL);
		_status.AddThemeColorOverride("font_color", MenuStyle.TEXT_DIM);
		box.AddChild(_status);
	}

	private void AddSmall(HBoxContainer row, string text, System.Action action)
	{
		var b = MenuStyle.MakeButton(text, 38f, MenuStyle.FONT_SMALL);
		b.CustomMinimumSize = new Vector2(110, 38);
		b.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		b.Pressed += action;
		row.AddChild(b);
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!ENABLED) return;

		if (e is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == TOGGLE_KEY)
		{
			_panel.Visible = !_panel.Visible;
			GetViewport().SetInputAsHandled();
		}
	}

	// ══════════════════════════════════════════════════════════════════════
	private void JumpToLevel(int target)
	{
		var p = Player.Instance;
		if (p == null) return;

		int missing = target - p.Level;
		if (missing <= 0) { Report($"ja esta no nivel {p.Level}"); return; }

		GrantLevels(missing);
	}

	// Enfileira os level-ups. O GameScene mostra a tela de upgrade um por vez,
	// senao as 20 chamadas se sobrescreveriam e so a ultima apareceria.
	private void GrantLevels(int count)
	{
		var p = Player.Instance;
		if (p == null) return;

		for (int i = 0; i < count; i++)
			p.GainXp(p.XpForNext);

		Report($"nivel {p.Level} — {PendingLevelUps} escolhas na fila");
	}

	private void SkipTime(float seconds)
	{
		GameManager.Instance?.DebugAddTime(seconds);
		Report($"tempo: {GameManager.Instance?.ElapsedTime:F0}s");
	}

	private void KillAll()
	{
		int n = 0;
		// copia a lista: matar altera Enemy.All no fim do frame
		var all = new System.Collections.Generic.List<Enemy>(Enemy.All);
		foreach (var e in all)
		{
			if (!IsInstanceValid(e)) continue;
			e.TakeDamageNoHemoglobina(999999);
			n++;
		}
		Report($"{n} inimigos removidos");
	}

	private void Report(string msg)
	{
		_status.Text = msg;
		GD.Print($"[Debug] {msg}");
	}
}
