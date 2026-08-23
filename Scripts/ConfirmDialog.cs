using Godot;

public partial class ConfirmDialog : CanvasLayer
{
	private Label         _messageLabel;
	private System.Action _onConfirm;
	private System.Action _onCancel;

	public override void _Ready()
	{
		Layer       = 60;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0, 0, 0, 0.8f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		var panel = new PanelContainer();
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.1f, 0.05f, 0.08f, 0.98f);
		style.BorderColor = new Color(0.8f, 0.2f, 0.2f);
		style.BorderWidthLeft = style.BorderWidthRight  = 2;
		style.BorderWidthTop  = style.BorderWidthBottom = 2;
		style.ContentMarginLeft = style.ContentMarginRight  = 24;
		style.ContentMarginTop  = style.ContentMarginBottom = 20;
		panel.AddThemeStyleboxOverride("panel", style);
		AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.CustomMinimumSize = new Vector2(380, 0);
		vbox.AddThemeConstantOverride("separation", 16);
		panel.AddChild(vbox);

		var title = new Label { Text = "ATENCAO", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 14);
		title.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
		vbox.AddChild(title);

		vbox.AddChild(new HSeparator());

		_messageLabel = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Word,
			CustomMinimumSize = new Vector2(0, 60)
		};
		_messageLabel.AddThemeFontSizeOverride("font_size", 10);
		vbox.AddChild(_messageLabel);

		vbox.AddChild(new HSeparator());

		var btnRow = new HBoxContainer();
		btnRow.Alignment = BoxContainer.AlignmentMode.Center;
		btnRow.AddThemeConstantOverride("separation", 20);
		vbox.AddChild(btnRow);

		var confirmBtn = new Button { Text = "CONFIRMAR" };
		confirmBtn.CustomMinimumSize = new Vector2(140, 40);
		confirmBtn.AddThemeFontSizeOverride("font_size", 10);
		var confirmStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.1f, 0.1f) };
		confirmBtn.AddThemeStyleboxOverride("normal", confirmStyle);
		confirmBtn.Pressed += () => { Visible = false; _onConfirm?.Invoke(); };
		btnRow.AddChild(confirmBtn);

		var cancelBtn = new Button { Text = "CANCELAR" };
		cancelBtn.CustomMinimumSize = new Vector2(140, 40);
		cancelBtn.AddThemeFontSizeOverride("font_size", 10);
		cancelBtn.Pressed += () => { Visible = false; _onCancel?.Invoke(); };
		btnRow.AddChild(cancelBtn);
	}

	public void Ask(string message, System.Action onConfirm, System.Action onCancel = null)
	{
		_messageLabel.Text = message;
		_onConfirm = onConfirm;
		_onCancel  = onCancel;
		Visible    = true;
	}
}
