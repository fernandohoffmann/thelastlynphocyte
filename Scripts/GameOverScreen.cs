// GameOverScreen.cs — completo, com a assinatura desfeita corretamente
using Godot;

public partial class GameOverScreen : CanvasLayer
{
	private Label _dnaRunLabel;
	private Label _dnaTotalLabel;
	private Label _timeLabel;

	public override void _Ready()
	{
		Layer       = 20;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.82f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		var panel = new Panel();
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.CustomMinimumSize = new Vector2(420, 320);
		panel.Position          = new Vector2(-210, -160);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vbox.OffsetLeft   =  28;
		vbox.OffsetTop    =  24;
		vbox.OffsetRight  = -28;
		vbox.OffsetBottom = -24;
		vbox.Alignment    = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = new Label
		{
			Text                = "GAME OVER",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
		vbox.AddChild(title);

		var sub = new Label
		{
			Text                = "O paciente não resistiu...",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		sub.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(sub);

		vbox.AddChild(new HSeparator());

		_timeLabel = new Label
		{
			Text                = "Tempo: 00:00",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_timeLabel.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(_timeLabel);

		_dnaRunLabel = new Label
		{
			Text                = "🧬 DNA coletado: 0",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_dnaRunLabel.AddThemeFontSizeOverride("font_size", 16);
		_dnaRunLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.5f));
		vbox.AddChild(_dnaRunLabel);

		_dnaTotalLabel = new Label
		{
			Text                = "🧬 DNA total: 0",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_dnaTotalLabel.AddThemeFontSizeOverride("font_size", 13);
		_dnaTotalLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.8f, 0.4f));
		vbox.AddChild(_dnaTotalLabel);

		vbox.AddChild(new HSeparator());

		var retryBtn = new Button { Text = "Tentar Novamente" };
		retryBtn.CustomMinimumSize = new Vector2(0, 44);
		retryBtn.AddThemeFontSizeOverride("font_size", 15);
		retryBtn.Pressed += () =>
		{
			Visible = false;
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		};
		vbox.AddChild(retryBtn);

		var menuBtn = new Button { Text = "Menu Principal" };
		menuBtn.CustomMinimumSize = new Vector2(0, 44);
		menuBtn.AddThemeFontSizeOverride("font_size", 15);
		menuBtn.Pressed += () => GameManager.Instance.ReturnToMenu();
		vbox.AddChild(menuBtn);

		GameManager.Instance.GameOver += OnGameOver;
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.GameOver -= OnGameOver;
	}

	private void OnGameOver()
	{
		AudioManager.Instance?.StopHeartLoop();
		float elapsed = GameManager.Instance.ElapsedTime;
		int   min     = (int)(elapsed / 60);
		int   sec     = (int)(elapsed % 60);
		_timeLabel.Text = $"Tempo sobrevivido: {min:00}:{sec:00}";

		_dnaRunLabel.Text   = $"🧬 DNA coletado: +{GameManager.Instance.SessionDna}";
		_dnaTotalLabel.Text = $"🧬 DNA total: {SaveData.TotalDna}";

		Visible          = true;
		GetTree().Paused = false;
	}
}
