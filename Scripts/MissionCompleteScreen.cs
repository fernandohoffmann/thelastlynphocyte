using Godot;

public partial class MissionCompleteScreen : CanvasLayer
{
	private Label _sub;

	public override void _Ready()
	{
		Layer       = 20;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.94f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		// O painel precisa de Size explicito. CustomMinimumSize so vale dentro
		// de um container: solto, o Panel ficava com tamanho ZERO e o VBox
		// ancorado como FullRect dentro dele saia com tamanho negativo — era
		// isso que espalhava o conteudo pro canto da tela.
		var size  = new Vector2(620, 320);

		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(28));
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.Size     = size;
		panel.Position = -size * 0.5f;
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 18);
		panel.AddChild(vbox);

		var title = new Label { Text = "MISSAO CONCLUIDA", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_TITLE);
		title.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.6f));
		vbox.AddChild(title);

		_sub = new Label
		{
			Text = "A infeccao foi controlada.",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		_sub.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_SMALL);
		_sub.AddThemeColorOverride("font_color", MenuStyle.TEXT);
		vbox.AddChild(_sub);

		vbox.AddChild(new HSeparator());

		var btn = MenuStyle.MakeButton("VOLTAR AO MENU", 52f);
		btn.Pressed += () => GameManager.Instance?.ReturnToMenu();
		vbox.AddChild(btn);
	}

	public void ShowScreen()
	{
		Visible          = true;
		GetTree().Paused = true;

		// Avisa qual fase acabou de ser liberada
		int next = GamePhase.Current + 1;
		_sub.Text = (next <= GamePhase.MAX_PHASE && GamePhase.IsUnlocked(next))
			? $"A infeccao foi controlada.\nFASE {next} - {GamePhase.Get(next).Name} liberada!"
			: "A infeccao foi controlada. Novas fases em breve.";

		// Silencia tudo imediatamente, não só ao clicar no botão
		AudioManager.Instance?.StopRockMusic();
		AudioManager.Instance?.StopHeartLoop();

		// Esconde o HUD e o minimapa pra não ficarem "congelados" atrás do overlay
		var scene = GetTree().CurrentScene;
		var hud     = scene?.GetNodeOrNull<Hud>("HUD");
		if (hud != null) hud.Visible = false;

		var minimap = scene?.GetNodeOrNull<Minimap>("Minimap");
		if (minimap != null) minimap.Visible = false;
	}
}
