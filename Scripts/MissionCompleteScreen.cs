using Godot;

public partial class MissionCompleteScreen : CanvasLayer
{
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
		panel.CustomMinimumSize = new Vector2(420, 260);
		panel.Position          = new Vector2(-210, -130);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vbox.OffsetLeft   = 28;
		vbox.OffsetTop    = 24;
		vbox.OffsetRight  = -28;
		vbox.OffsetBottom = -24;
		vbox.Alignment    = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = new Label { Text = "MISSAO CONCLUIDA", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 28);
		title.AddThemeColorOverride("font_color", new Color(0.4f, 1f, 0.6f));
		vbox.AddChild(title);

		var sub = new Label
		{
			Text = "A infeccao foi controlada. Novas fases em breve.",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		sub.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(sub);

		vbox.AddChild(new HSeparator());

		var btn = new Button { Text = "Voltar ao Menu" };
		btn.Pressed += () => GameManager.Instance?.ReturnToMenu();
		vbox.AddChild(btn);
	}

	public void ShowScreen()
	{
		Visible          = true;
		GetTree().Paused = true;
	}
}
