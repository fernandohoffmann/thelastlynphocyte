// CodexToast.cs — aviso de nova descoberta no canto da tela.
// Enfileira: se duas descobertas caem juntas, a segunda espera a primeira sair.
using Godot;
using System.Collections.Generic;

public partial class CodexToast : CanvasLayer
{
	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	private const float SHOW_TIME = 3.2f;   // tempo com o aviso parado na tela
	private const float SLIDE     = 0.35f;  // entrada e saida
	private const float WIDTH     = 420f;
	private const float HEIGHT    = 84f;
	private const float MARGIN    = 28f;
	// ──────────────────────────────────────────────────────────────────────

	private readonly Queue<CodexEntry> _queue = new();
	private PanelContainer _panel;
	private Label          _title;
	private Label          _name;

	private float _timer  = 0f;
	private int   _phase  = 0;   // 0 parado, 1 entrando, 2 mostrando, 3 saindo

	public override void _Ready()
	{
		Layer = 6; // acima do HUD, abaixo das telas cheias

		_panel = new PanelContainer();
		_panel.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(14));
		_panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_panel.Size    = new Vector2(WIDTH, HEIGHT);
		_panel.Visible = false;
		AddChild(_panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 6);
		_panel.AddChild(box);

		_title = new Label { Text = "NOVA DESCOBERTA" };
		_title.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_SMALL);
		_title.AddThemeColorOverride("font_color", MenuStyle.ACCENT);
		box.AddChild(_title);

		_name = new Label { Text = "" };
		_name.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_BODY);
		_name.AddThemeColorOverride("font_color", MenuStyle.TEXT);
		box.AddChild(_name);

		Codex.Discovered += OnDiscovered;
	}

	public override void _ExitTree()
	{
		Codex.Discovered -= OnDiscovered;
	}

	private void OnDiscovered(CodexEntry entry) => _queue.Enqueue(entry);

	public override void _Process(double delta)
	{
		float d = (float)delta;

		if (_phase == 0)
		{
			if (_queue.Count == 0) return;

			var entry = _queue.Dequeue();
			_name.Text     = entry.Name.ToUpper();
			_panel.Visible = true;
			_phase = 1;
			_timer = 0f;
			AudioManager.Instance?.PlayUiUnlock();
		}

		_timer += d;

		float restX = GetViewport().GetVisibleRect().Size.X - WIDTH - MARGIN;
		float hidX  = GetViewport().GetVisibleRect().Size.X + 10f;

		switch (_phase)
		{
			case 1:
			{
				float t = Mathf.Clamp(_timer / SLIDE, 0f, 1f);
				float e = 1f - Mathf.Pow(1f - t, 3f);           // desacelera na chegada
				_panel.Position = new Vector2(Mathf.Lerp(hidX, restX, e), MARGIN);
				if (t >= 1f) { _phase = 2; _timer = 0f; }
				break;
			}
			case 2:
				_panel.Position = new Vector2(restX, MARGIN);
				if (_timer >= SHOW_TIME) { _phase = 3; _timer = 0f; }
				break;

			case 3:
			{
				float t = Mathf.Clamp(_timer / SLIDE, 0f, 1f);
				_panel.Position = new Vector2(Mathf.Lerp(restX, hidX, t * t), MARGIN);
				if (t >= 1f)
				{
					_phase = 0;
					_panel.Visible = false;
				}
				break;
			}
		}
	}
}
