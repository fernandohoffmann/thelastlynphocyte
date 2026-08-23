using Godot;

public partial class MainMenu : Control
{
	private LoadoutScreen   _loadoutScreen;
	private ArsenalScreen   _arsenalScreen;
	private PanelContainer  _settingsPanel;
	private IntroScreen     _introScreen;
	private ConfirmDialog   _confirmDialog;

	public override void _Ready()
	{
		SaveData.Load();

		var font = GD.Load<FontFile>("res://Assets/Fonts/PressStart2P-Regular.ttf");
		if (font != null)
		{
			var theme = new Theme();
			theme.DefaultFont = font;
			theme.DefaultFontSize = 8;
			GetTree().Root.Theme = theme;
		}

		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new TextureRect();
		bg.Texture     = GD.Load<Texture2D>("res://Assets/fundo_menu.png");
		bg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		bg.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(bg);

		var overlay = new ColorRect { Color = new Color(0f, 0f, 0f, 0.42f) };
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(overlay);

		// ── Título ────────────────────────────────────────────────
		var titleContainer = new VBoxContainer();
		titleContainer.SetAnchorsPreset(LayoutPreset.TopRight);
		titleContainer.OffsetLeft   = -500f; titleContainer.OffsetTop    = 30f;
		titleContainer.OffsetRight  =  -30f; titleContainer.OffsetBottom = 200f;
		titleContainer.AddThemeConstantOverride("separation", 0);
		AddChild(titleContainer);

		var titleLine1 = new Label { Text = "THE LAST" };
		titleLine1.HorizontalAlignment = HorizontalAlignment.Right;
		titleLine1.AddThemeFontSizeOverride("font_size", 42);
		titleLine1.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.15f));
		titleContainer.AddChild(titleLine1);

		var titleLine2 = new Label { Text = "LYMPHOCYTE" };
		titleLine2.HorizontalAlignment = HorizontalAlignment.Right;
		titleLine2.AddThemeFontSizeOverride("font_size", 42);
		titleLine2.AddThemeColorOverride("font_color", new Color(0.95f, 0.75f, 0.15f));
		titleContainer.AddChild(titleLine2);

		titleContainer.AddChild(new ColorRect { Color = new Color(0.95f, 0.75f, 0.15f, 0.6f), CustomMinimumSize = new Vector2(0, 2) });

		var tagline = new Label { Text = "Defenda o organismo. Evolua ou pereca." };
		tagline.HorizontalAlignment = HorizontalAlignment.Right;
		tagline.AddThemeFontSizeOverride("font_size", 9);
		tagline.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f, 0.8f));
		titleContainer.AddChild(tagline);

		// ── Botões ────────────────────────────────────────────────
		var btnCol = new VBoxContainer();
		btnCol.SetAnchorsPreset(LayoutPreset.BottomRight);
		btnCol.OffsetLeft   = -340f; btnCol.OffsetTop    = -380f;
		btnCol.OffsetRight  =  -30f; btnCol.OffsetBottom =  -30f;
		btnCol.AddThemeConstantOverride("separation", 10);
		AddChild(btnCol);

		MakeButton(btnCol, "NOVO JOGO",        new Color(0.7f, 0.15f, 0.15f),  () => _loadoutScreen?.Show());
		MakeButton(btnCol, "ARSENAL GENETICO", new Color(0.15f, 0.35f, 0.6f),  () => _arsenalScreen?.Show());
		MakeButton(btnCol, "CONFIGURACOES",    new Color(0.2f, 0.2f, 0.2f),    () => _settingsPanel.Visible = !_settingsPanel.Visible);
		MakeButton(btnCol, "SAIR DO JOGO",     new Color(0.15f, 0.15f, 0.15f), () => GetTree().Quit());

		// ── DNA ───────────────────────────────────────────────────
		var dnaContainer = new HBoxContainer();
		dnaContainer.SetAnchorsPreset(LayoutPreset.BottomLeft);
		dnaContainer.OffsetLeft   =  20f; dnaContainer.OffsetTop    = -60f;
		dnaContainer.OffsetRight  = 400f; dnaContainer.OffsetBottom = -20f;
		AddChild(dnaContainer);

		var dnaDot = new ColorRect { Color = new Color(0.3f, 1f, 0.4f), CustomMinimumSize = new Vector2(10, 10) };
		dnaDot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		dnaContainer.AddChild(dnaDot);

		var dnaLabel = new Label { Text = $"  DNA: {SaveData.TotalDna}" };
		dnaLabel.AddThemeFontSizeOverride("font_size", 10);
		dnaLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		dnaContainer.AddChild(dnaLabel);

		// ── Versão ────────────────────────────────────────────────
		var versionLabel = new Label { Text = "v0.8 Early Access" };
		versionLabel.SetAnchorsPreset(LayoutPreset.BottomRight);
		versionLabel.OffsetLeft = -220f; versionLabel.OffsetTop    = -28f;
		versionLabel.OffsetRight = -8f;  versionLabel.OffsetBottom =  -4f;
		versionLabel.HorizontalAlignment = HorizontalAlignment.Right;
		versionLabel.AddThemeFontSizeOverride("font_size", 8);
		versionLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
		AddChild(versionLabel);

		// ── Painel de Configurações ───────────────────────────────
		_settingsPanel         = new PanelContainer();
		_settingsPanel.Visible = false;
		_settingsPanel.SetAnchorsPreset(LayoutPreset.Center);

		var settingsStyle = new StyleBoxFlat();
		settingsStyle.BgColor           = new Color(0.08f, 0.08f, 0.10f, 0.95f);
		settingsStyle.BorderColor       = new Color(0.4f, 0.4f, 0.5f, 0.8f);
		settingsStyle.BorderWidthLeft   = settingsStyle.BorderWidthRight  = 2;
		settingsStyle.BorderWidthTop    = settingsStyle.BorderWidthBottom = 2;
		settingsStyle.ContentMarginLeft = settingsStyle.ContentMarginRight  = 24;
		settingsStyle.ContentMarginTop  = settingsStyle.ContentMarginBottom = 20;
		_settingsPanel.AddThemeStyleboxOverride("panel", settingsStyle);
		AddChild(_settingsPanel);

		var settingsVbox = new VBoxContainer();
		settingsVbox.CustomMinimumSize = new Vector2(380, 0);
		settingsVbox.AddThemeConstantOverride("separation", 14);
		_settingsPanel.AddChild(settingsVbox);

		var settingsTitle = new Label { Text = "CONFIGURACOES", HorizontalAlignment = HorizontalAlignment.Center };
		settingsTitle.AddThemeFontSizeOverride("font_size", 14);
		settingsVbox.AddChild(settingsTitle);
		settingsVbox.AddChild(new HSeparator());

		// Volume música
		var volRow = new HBoxContainer();
		volRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(volRow);
		var volLabel = new Label { Text = "Musica:", CustomMinimumSize = new Vector2(80, 0) };
		volRow.AddChild(volLabel);
		var volSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume * 100 : 80, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		volSlider.ValueChanged += (double v) => AudioManager.Instance?.SetMusicVolume((float)(v / 100.0));
		volRow.AddChild(volSlider);

		// Volume SFX
		var sfxRow = new HBoxContainer();
		sfxRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(sfxRow);
		var sfxLabel = new Label { Text = "Efeitos:", CustomMinimumSize = new Vector2(80, 0) };
		sfxRow.AddChild(sfxLabel);
		var sfxSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume * 100 : 100, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		sfxSlider.ValueChanged += (double v) => AudioManager.Instance?.SetSfxVolume((float)(v / 100.0));
		sfxRow.AddChild(sfxSlider);

		settingsVbox.AddChild(new HSeparator());

		// Tela cheia
		var fullscreenRow = new HBoxContainer();
		fullscreenRow.AddThemeConstantOverride("separation", 10);
		settingsVbox.AddChild(fullscreenRow);
		var fullscreenLabel = new Label { Text = "Tela cheia:", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		fullscreenRow.AddChild(fullscreenLabel);
		var fullscreenCheck = new CheckBox { ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen };
		fullscreenCheck.Toggled += (bool on) => DisplayServer.WindowSetMode(on ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		fullscreenRow.AddChild(fullscreenCheck);

		settingsVbox.AddChild(new HSeparator());

		// ── RESETAR PROGRESSO ─────────────────────────────────────
		var resetBtn = new Button { Text = "RESETAR PROGRESSO" };
		resetBtn.CustomMinimumSize = new Vector2(0, 40);
		resetBtn.AddThemeFontSizeOverride("font_size", 10);
		var resetStyle = new StyleBoxFlat { BgColor = new Color(0.5f, 0.08f, 0.08f) };
		resetBtn.AddThemeStyleboxOverride("normal", resetStyle);
		resetBtn.Pressed += () =>
		{
			_confirmDialog?.Ask(
				"Isso vai apagar TODO o seu progresso:\nDNA, habilidades desbloqueadas e saves.\n\nEssa acao nao pode ser desfeita!",
				() =>
				{
					SaveData.Reset();
					dnaLabel.Text = $"  DNA: {SaveData.TotalDna}";
				}
			);
		};
		settingsVbox.AddChild(resetBtn);

		settingsVbox.AddChild(new HSeparator());

		var closeBtn = new Button { Text = "Fechar" };
		closeBtn.CustomMinimumSize = new Vector2(0, 40);
		closeBtn.Pressed += () => _settingsPanel.Visible = false;
		settingsVbox.AddChild(closeBtn);

		CallDeferred(MethodName.InitScreens);
	}

	private void InitScreens()
	{
		if (AudioManager.Instance == null)
		{
			var am = new AudioManager { Name = "AudioManager" };
			GetTree().Root.AddChild(am);
		}
		if (UpgradeManager.Instance == null)
		{
			var um = new UpgradeManager { Name = "UpgradeManager" };
			GetTree().Root.AddChild(um);
		}
		if (GameManager.Instance == null)
		{
			var gm = new GameManager { Name = "GameManager" };
			GetTree().Root.AddChild(gm);
		}

		_introScreen = new IntroScreen { Name = "IntroScreen" };
		GetTree().Root.AddChild(_introScreen);

		_confirmDialog = new ConfirmDialog { Name = "ConfirmDialog" };
		GetTree().Root.AddChild(_confirmDialog);

		_loadoutScreen = new LoadoutScreen { Name = "LoadoutScreen" };
		GetTree().Root.AddChild(_loadoutScreen);

		_arsenalScreen = new ArsenalScreen { Name = "ArsenalScreen" };
		GetTree().Root.AddChild(_arsenalScreen);

		// Expõe IntroScreen e ConfirmDialog para LoadoutScreen
		LoadoutScreen.IntroScreenRef   = _introScreen;
		LoadoutScreen.ConfirmDialogRef = _confirmDialog;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_loadoutScreen)) _loadoutScreen.QueueFree();
		if (IsInstanceValid(_arsenalScreen)) _arsenalScreen.QueueFree();
		if (IsInstanceValid(_introScreen))   _introScreen.QueueFree();
		if (IsInstanceValid(_confirmDialog)) _confirmDialog.QueueFree();
	}

	private void MakeButton(VBoxContainer parent, string text, Color bgColor, System.Action onPressed)
	{
		var container = new PanelContainer();
		container.CustomMinimumSize = new Vector2(300, 54);
		var style = new StyleBoxFlat();
		style.BgColor = bgColor;
		style.BorderColor = new Color(bgColor.R + 0.2f, bgColor.G + 0.2f, bgColor.B + 0.2f, 0.8f);
		style.BorderWidthLeft = 3; style.BorderWidthTop = 1;
		style.BorderWidthRight = 1; style.BorderWidthBottom = 3;
		style.CornerRadiusTopLeft = style.CornerRadiusTopRight = 4;
		style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 4;
		container.AddThemeStyleboxOverride("panel", style);

		var btn = new Button { Text = text };
		btn.SetAnchorsPreset(LayoutPreset.FullRect);
		btn.AddThemeFontSizeOverride("font_size", 13);
		btn.AddThemeColorOverride("font_color", Colors.White);
		var btnStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
		btn.AddThemeStyleboxOverride("normal",  btnStyle);
		btn.AddThemeStyleboxOverride("hover",   MakeHoverStyle(bgColor));
		btn.AddThemeStyleboxOverride("pressed", MakePressedStyle(bgColor));
		btn.AddThemeStyleboxOverride("focus",   btnStyle);
		btn.Pressed      += () => { AudioManager.Instance?.PlayUiClick(); onPressed(); };
		btn.MouseEntered += () => AudioManager.Instance?.PlayUiHover();
		container.AddChild(btn);
		parent.AddChild(container);
	}

	private StyleBoxFlat MakeHoverStyle(Color c)
	{
		var s = new StyleBoxFlat();
		s.BgColor = new Color(Mathf.Min(c.R + 0.15f, 1f), Mathf.Min(c.G + 0.15f, 1f), Mathf.Min(c.B + 0.15f, 1f), 0.9f);
		s.CornerRadiusTopLeft = s.CornerRadiusTopRight = s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = 4;
		return s;
	}

	private StyleBoxFlat MakePressedStyle(Color c)
	{
		var s = new StyleBoxFlat();
		s.BgColor = new Color(Mathf.Max(c.R - 0.1f, 0f), Mathf.Max(c.G - 0.1f, 0f), Mathf.Max(c.B - 0.1f, 0f), 1f);
		s.CornerRadiusTopLeft = s.CornerRadiusTopRight = s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = 4;
		return s;
	}
}
