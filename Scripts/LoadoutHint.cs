// LoadoutHint.cs — dicas da primeira vez na tela de Loadout.
//
// Destaca uma area da tela com uma moldura pulsando e mostra um balao
// ("AQUI VOCE SELECIONA SUAS ARMAS"). O balao tem PROXIMO e PULAR; o resto da
// tela continua clicavel por baixo (so o balao em si captura o mouse).
using Godot;
using System.Collections.Generic;

public partial class LoadoutHint : Control
{
	public class HintStep
	{
		public System.Func<Control> Target;   // area a destacar (resolvida a cada quadro)
		public string Title;
		public string Body;
		public bool   Above;                  // balao ACIMA do alvo (senao, dentro dele, no topo)
	}

	private List<HintStep> _steps = new();
	private int            _i;
	private bool           _active;
	private System.Action  _onDone;

	private PanelContainer _callout;
	private Label          _title;
	private Label          _body;
	private Label          _counter;
	private Button         _next;

	private Rect2 _targetRect;
	private bool  _hasTarget;
	private float _time;

	public bool IsActive => _active;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		ZIndex = 60;   // acima do tooltip (50)

		_callout = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
		_callout.AddThemeStyleboxOverride("panel", MenuStyle.PanelFrame(24));
		AddChild(_callout);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		_callout.AddChild(vbox);

		_title = new Label { CustomMinimumSize = new Vector2(600, 0), AutowrapMode = TextServer.AutowrapMode.Word };
		_title.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_TITLE);
		_title.AddThemeColorOverride("font_color", MenuStyle.ACCENT);
		vbox.AddChild(_title);

		_body = new Label { CustomMinimumSize = new Vector2(600, 0), AutowrapMode = TextServer.AutowrapMode.Word };
		_body.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_BODY);
		_body.AddThemeColorOverride("font_color", MenuStyle.TEXT);
		vbox.AddChild(_body);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(row);

		_counter = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_counter.AddThemeFontSizeOverride("font_size", MenuStyle.FONT_SMALL);
		_counter.AddThemeColorOverride("font_color", MenuStyle.TEXT_DIM);
		row.AddChild(_counter);

		var skip = MenuStyle.MakeButton("PULAR", 44f, MenuStyle.FONT_SMALL);
		skip.CustomMinimumSize = new Vector2(120, 44);
		skip.Pressed += () => Finish();
		row.AddChild(skip);

		_next = MenuStyle.MakeButton("PROXIMO", 44f, MenuStyle.FONT_BODY);
		_next.CustomMinimumSize = new Vector2(200, 44);
		_next.Pressed += OnNext;
		row.AddChild(_next);

		_callout.Visible = false;
		Visible          = false;
		SetProcess(false);
	}

	public void Begin(List<HintStep> steps, System.Action onDone)
	{
		if (steps == null || steps.Count == 0) { onDone?.Invoke(); return; }

		_steps  = steps;
		_onDone = onDone;
		_i      = 0;
		_active = true;
		Visible = true;
		SetProcess(true);
		ShowStep();
	}

	private void ShowStep()
	{
		var s = _steps[_i];
		_title.Text   = s.Title;
		_body.Text    = s.Body;
		_counter.Text = $"{_i + 1} / {_steps.Count}";
		_next.Text    = _i >= _steps.Count - 1 ? "ENTENDI" : "PROXIMO";
		_callout.Visible = false;   // reaparece quando o alvo tiver tamanho
	}

	private void OnNext()
	{
		_i++;
		if (_i >= _steps.Count) { Finish(); return; }
		ShowStep();
	}

	private void Finish()
	{
		if (!_active) return;
		_active = false;
		SetProcess(false);
		Visible = false;
		_callout.Visible = false;
		_onDone?.Invoke();
	}

	/// Encerra sem avisar ninguem (ex.: o jogador iniciou a partida direto).
	public void Dismiss()
	{
		if (!_active) return;
		Finish();
	}

	public override void _Process(double delta)
	{
		if (!_active) return;
		_time += (float)delta;

		var target = _steps[_i].Target?.Invoke();
		_hasTarget = target != null && target.IsVisibleInTree() && target.Size.X > 4f;
		if (!_hasTarget) { _callout.Visible = false; QueueRedraw(); return; }

		_targetRect = target.GetGlobalRect();
		var vp = GetViewportRect().Size;

		_callout.Size = _callout.GetCombinedMinimumSize();
		var   sz = _callout.Size;
		float x  = Mathf.Clamp(_targetRect.GetCenter().X - sz.X * 0.5f, 20f, Mathf.Max(20f, vp.X - sz.X - 20f));
		float y  = _steps[_i].Above
			? _targetRect.Position.Y - sz.Y - 34f
			: _targetRect.Position.Y + 90f;
		y = Mathf.Clamp(y, 10f, Mathf.Max(10f, vp.Y - sz.Y - 10f));

		_callout.Position = new Vector2(x, y);
		_callout.Visible  = true;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_active || !_hasTarget) return;

		// moldura pulsando em volta da area explicada
		float pulse = 0.55f + 0.45f * Mathf.Sin(_time * 4.5f);
		var   acc   = MenuStyle.ACCENT;
		var   frame = MenuStyle.Frame(new Color(acc.R, acc.G, acc.B, 0.06f * pulse),
									  new Color(acc.R, acc.G, acc.B, pulse), 3, 18);
		DrawStyleBox(frame, _targetRect.Grow(10f));

		// seta balancando quando o balao fica acima do alvo
		if (_steps[_i].Above)
		{
			float bob = Mathf.Sin(_time * 5f) * 7f;
			var   tip = new Vector2(_targetRect.GetCenter().X, _targetRect.Position.Y - 12f + bob);
			DrawColoredPolygon(new[]
			{
				tip,
				tip + new Vector2(-18f, -22f),
				tip + new Vector2( 18f, -22f),
			}, acc);
		}
	}
}
