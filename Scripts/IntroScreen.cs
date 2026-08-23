using Godot;

public partial class IntroScreen : CanvasLayer
{
	private VideoStreamPlayer _video;
	private ColorRect         _topBar;
	private ColorRect         _botBar;
	private ColorRect         _fadeRect;
	private Label             _skipLabel;
	private ColorRect         _skipBarBg;
	private ColorRect         _skipBarFill;

	private float _fadeTimer    = 0f;
	private bool  _fadingOut    = false;
	private bool  _skipHolding  = false;
	private float _skipProgress = 0f;

	// Estado da intro
	private enum State { BlackIn, Hold, Playing, FadeOut }
	private State _state = State.BlackIn;

	private const float BLACK_IN_DURATION = 0.6f;  // fade para preto
	private const float BLACK_HOLD        = 3.0f;  // segundos de tela preta
	private const float FADE_IN_DURATION  = 0.8f;  // fade do preto para o video
	private const float FADE_OUT_DURATION = 1.2f;
	private const float SKIP_HOLD_TIME   = 1.5f;

	private System.Action _onFinished;

	public override void _Ready()
	{
		Layer       = 50;
		Visible     = false;
		ProcessMode = ProcessModeEnum.Always;

		var bg = new ColorRect { Color = Colors.Black };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(bg);

		_video = new VideoStreamPlayer();
		_video.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_video.Expand     = true;
		_video.AudioTrack = 0;
		_video.Finished  += OnVideoFinished;
		_video.Visible    = false;
		AddChild(_video);

		// Barras cinematic
		float barH = 40f;
		_topBar = new ColorRect { Color = Colors.Black };
		_topBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_topBar.OffsetBottom = barH;
		AddChild(_topBar);

		_botBar = new ColorRect { Color = Colors.Black };
		_botBar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_botBar.OffsetTop = -barH;
		AddChild(_botBar);

		// Hint de skip
		_skipLabel = new Label
		{
			Text                = "Segure ESPACO para pular",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_skipLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_skipLabel.OffsetTop    = -barH + 6f;
		_skipLabel.OffsetBottom = -barH + 22f;
		_skipLabel.AddThemeFontSizeOverride("font_size", 8);
		_skipLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f, 0.8f));
		_skipLabel.Visible = false;
		AddChild(_skipLabel);

		// Barra de progresso de skip
		_skipBarBg = new ColorRect { Color = new Color(0.2f, 0.2f, 0.2f, 0.7f) };
		_skipBarBg.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_skipBarBg.OffsetLeft   =  200f;
		_skipBarBg.OffsetRight  = -200f;
		_skipBarBg.OffsetTop    = -barH + 24f;
		_skipBarBg.OffsetBottom = -barH + 34f;
		_skipBarBg.Visible      = false;
		AddChild(_skipBarBg);

		_skipBarFill = new ColorRect { Color = new Color(0.9f, 0.9f, 0.9f, 0.9f) };
		_skipBarFill.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_skipBarFill.AnchorRight = 0f;
		_skipBarFill.OffsetRight = 0f;
		_skipBarBg.AddChild(_skipBarFill);

		// Overlay de fade — começa transparente (a tela já é preta)
		_fadeRect = new ColorRect { Color = new Color(0, 0, 0, 0) };
		_fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_fadeRect);
	}

	public void Play(System.Action onFinished)
	{
		_onFinished   = onFinished;
		Visible       = true;
		_fadingOut    = false;
		_fadeTimer    = 0f;
		_skipProgress = 0f;
		_skipHolding  = false;
		_state        = State.BlackIn;

		// Começa com overlay preto visível
		_fadeRect.Color    = new Color(0, 0, 0, 1);
		_video.Visible     = false;
		_skipLabel.Visible = false;
		_skipBarBg.Visible = false;

		var stream = GD.Load<VideoStream>("res://Assets/Videos/intro.ogv");
		if (stream == null)
		{
			GD.PrintErr("[IntroScreen] Video nao encontrado (intro.ogv)");
			Finish();
			return;
		}
		_video.Stream = stream;
	}

	public override void _Process(double delta)
	{
		if (!Visible) return;

		switch (_state)
		{
			case State.BlackIn:
				// Fade de qualquer coisa para preto (já começa preto, mas garante)
				_fadeTimer += (float)delta;
				_fadeRect.Color = new Color(0, 0, 0, 1f);
				if (_fadeTimer >= BLACK_IN_DURATION)
				{
					_fadeTimer = 0f;
					_state     = State.Hold;
				}
				break;

			case State.Hold:
				// Tela preta por 3 segundos
				_fadeTimer += (float)delta;
				if (_fadeTimer >= BLACK_HOLD)
				{
					_fadeTimer     = 0f;
					_state         = State.Playing;
					_video.Visible = true;
					_video.Play();
				}
				break;

			case State.Playing:
	// Fade do preto para o vídeo
	_fadeTimer += (float)delta;
	float fadeInAlpha = Mathf.Clamp(1f - _fadeTimer / FADE_IN_DURATION, 0f, 1f);
	_fadeRect.Color = new Color(0, 0, 0, fadeInAlpha);

	// Hint aparece após o fade in, some após 3s, reaparece ao segurar
	if (_fadeTimer > FADE_IN_DURATION)
	{
		float timeSinceFadeIn = _fadeTimer - FADE_IN_DURATION;

		bool holding = Input.IsKeyPressed(Key.Space);

		if (holding)
		{
			// Aparece gradualmente ao segurar
			_skipProgress += (float)delta;
			float t        = Mathf.Clamp(_skipProgress / SKIP_HOLD_TIME, 0f, 1f);
			_skipBarFill.AnchorRight = t;

			// Fade in do hint ao começar a segurar
			float hintAlpha = Mathf.Clamp(_skipProgress / 0.3f, 0f, 1f);
			_skipLabel.Modulate  = new Color(1f, 1f, 1f, hintAlpha);
			_skipBarBg.Modulate  = new Color(1f, 1f, 1f, hintAlpha);
			_skipLabel.Visible   = true;
			_skipBarBg.Visible   = true;
			_skipHolding         = true;

			if (_skipProgress >= SKIP_HOLD_TIME) StartFadeOut();
		}
		else
		{
			// Decai ao soltar
			if (_skipHolding)
			{
				_skipProgress = Mathf.Max(0f, _skipProgress - (float)delta * 2f);
				float t       = Mathf.Clamp(_skipProgress / SKIP_HOLD_TIME, 0f, 1f);
				_skipBarFill.AnchorRight = t;

				// Fade out rápido ao soltar
				float hintAlpha = Mathf.Clamp(_skipProgress / 0.2f, 0f, 1f);
				_skipLabel.Modulate = new Color(1f, 1f, 1f, hintAlpha);
				_skipBarBg.Modulate = new Color(1f, 1f, 1f, hintAlpha);

				if (_skipProgress <= 0f)
				{
					_skipHolding       = false;
					_skipLabel.Visible = false;
					_skipBarBg.Visible = false;
				}
			}
			else
			{
				// Pisca por 3s logo após o fade in, depois some
				if (timeSinceFadeIn <= 3f)
				{
					_skipLabel.Visible  = true;
					_skipBarBg.Visible  = true;
					// Fade out suave nos últimos 0.5s
					float disappear = Mathf.Clamp((3f - timeSinceFadeIn) / 0.5f, 0f, 1f);
					_skipLabel.Modulate = new Color(1f, 1f, 1f, disappear);
					_skipBarBg.Modulate = new Color(1f, 1f, 1f, disappear);
				}
				else
				{
					_skipLabel.Visible = false;
					_skipBarBg.Visible = false;
				}
			}
		}
	}
	break;

			case State.FadeOut:
				_fadeTimer += (float)delta;
				float outAlpha = Mathf.Clamp(_fadeTimer / FADE_OUT_DURATION, 0f, 1f);
				_fadeRect.Color = new Color(0, 0, 0, outAlpha);
				if (_fadeTimer >= FADE_OUT_DURATION) Finish();
				break;
		}
	}

	private void OnVideoFinished() => StartFadeOut();

	private void StartFadeOut()
	{
		if (_state == State.FadeOut) return;
		_state     = State.FadeOut;
		_fadeTimer = 0f;
		_video.Stop();
		_skipLabel.Visible = false;
		_skipBarBg.Visible = false;
	}

	private void Finish()
	{
		Visible = false;
		_onFinished?.Invoke();
	}
}
