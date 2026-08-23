using Godot;

public partial class PauseMenu : CanvasLayer
{
	private Label _dnaLabel;
	private HSlider _volSlider;
	private HSlider _sfxSlider;

	public override void _Ready()
	{
		Layer = 18;
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;

		// Root
		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		// Fundo escuro
		var bg = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.65f)
		};
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bg.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(bg);

		// ==========================
		// CENTER CONTAINER
		// ==========================
		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		center.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(center);

		// Painel
		var panel = new PanelContainer();

		var panelStyle = new StyleBoxFlat();
		panelStyle.BgColor = new Color(0.08f, 0.06f, 0.10f, 0.97f);
		panelStyle.BorderColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
		panelStyle.BorderWidthLeft = 2;
		panelStyle.BorderWidthRight = 2;
		panelStyle.BorderWidthTop = 2;
		panelStyle.BorderWidthBottom = 2;
		panelStyle.ContentMarginLeft = 20;
		panelStyle.ContentMarginRight = 20;
		panelStyle.ContentMarginTop = 20;
		panelStyle.ContentMarginBottom = 20;

		panel.AddThemeStyleboxOverride("panel", panelStyle);

		center.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.CustomMinimumSize = new Vector2(360, 0);
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = new Label
		{
			Text = "-- PAUSADO --",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 16);
		vbox.AddChild(title);

		vbox.AddChild(new HSeparator());

		_dnaLabel = new Label
		{
			Text = "DNA nesta run: 0",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_dnaLabel.AddThemeFontSizeOverride("font_size", 11);
		_dnaLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		vbox.AddChild(_dnaLabel);

		vbox.AddChild(new HSeparator());

		var cfgTitle = new Label
		{
			Text = "CONFIGURACOES",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		cfgTitle.AddThemeFontSizeOverride("font_size", 10);
		cfgTitle.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.5f));
		vbox.AddChild(cfgTitle);

		// Música
		var volRow = new HBoxContainer();
		volRow.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(volRow);

		var volLbl = new Label
		{
			Text = "Musica:",
			CustomMinimumSize = new Vector2(70, 0)
		};
		volRow.AddChild(volLbl);

		_volSlider = new HSlider
		{
			MinValue = 0,
			MaxValue = 100,
			Value = 80,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		_volSlider.ValueChanged += (double v) =>
			AudioManager.Instance?.SetMusicVolume((float)(v / 100.0));

		volRow.AddChild(_volSlider);

		// Efeitos
		var sfxRow = new HBoxContainer();
		sfxRow.AddThemeConstantOverride("separation", 8);
		vbox.AddChild(sfxRow);

		var sfxLbl = new Label
		{
			Text = "Efeitos:",
			CustomMinimumSize = new Vector2(70, 0)
		};
		sfxRow.AddChild(sfxLbl);

		_sfxSlider = new HSlider
		{
			MinValue = 0,
			MaxValue = 100,
			Value = 100,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		_sfxSlider.ValueChanged += (double v) =>
			AudioManager.Instance?.SetSfxVolume((float)(v / 100.0));

		sfxRow.AddChild(_sfxSlider);

		vbox.AddChild(new HSeparator());

		var resumeBtn = new Button
		{
			Text = "Voltar ao Jogo",
			CustomMinimumSize = new Vector2(0, 42)
		};
		resumeBtn.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(resumeBtn);

		var menuBtn = new Button
		{
			Text = "Menu Principal",
			CustomMinimumSize = new Vector2(0, 42)
		};
		menuBtn.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(menuBtn);

		resumeBtn.Pressed += () =>
		{
			Visible = false;
			GameManager.Instance.ResumeGame();
		};

		menuBtn.Pressed += () =>
		{
			Visible = false;
			AudioManager.Instance?.StopRockMusic();
			AudioManager.Instance?.StopHeartLoop();
			GameManager.Instance.ReturnToMenu();
		};
	}

	private void OpenMenu()
	{
		if (AudioManager.Instance != null)
		{
			_volSlider.SetValueNoSignal(AudioManager.Instance.MusicVolume * 100);
			_sfxSlider.SetValueNoSignal(AudioManager.Instance.SfxVolume * 100);
		}

		_dnaLabel.Text = $"DNA nesta run: {GameManager.Instance.SessionDna}";
		Visible = true;
		GameManager.Instance.PauseGame();
	}

	private void CloseMenu()
	{
		Visible = false;
		GameManager.Instance.ResumeGame();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Keycode != Key.Escape)
			return;

		if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
			return;

		var upgradeScreen = GetTree().CurrentScene?.GetNodeOrNull<UpgradeScreen>("UpgradeScreen");
		if (upgradeScreen != null && upgradeScreen.Visible)
			return;

		if (Visible)
			CloseMenu();
		else
			OpenMenu();

		GetViewport().SetInputAsHandled();
	}
}
