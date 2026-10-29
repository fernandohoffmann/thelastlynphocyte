// Vignette.cs — vinheta forte (cantos quase pretos) + escurecimento geral
using Godot;

public partial class Vignette : CanvasLayer
{
	private ColorRect      _rect;
	private ShaderMaterial _mat;

	// ── AJUSTE FINO AQUI ──────────────────────────────────────────────────
	// valores com HP CHEIO  →  valores com HP ZERADO
	private const float INTENSITY_FULL = 0.88f;  private const float INTENSITY_LOW = 1.00f;
	private const float RADIUS_FULL    = 0.26f;  private const float RADIUS_LOW    = 0.05f;
	private const float DARKNESS_FULL  = 0.25f;  private const float DARKNESS_LOW  = 0.45f;
	// ──────────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		Layer = 4;

		_rect = new ColorRect();
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;

		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform float intensity     : hint_range(0.0, 1.0) = 0.88;
uniform float inner_radius  : hint_range(0.0, 1.0) = 0.26;
uniform float base_darkness : hint_range(0.0, 1.0) = 0.25;

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv) * 2.0;
    float vignette = smoothstep(inner_radius, 1.25, dist);
    float alpha = base_darkness + vignette * intensity * (1.0 - base_darkness);
    COLOR = vec4(0.02, 0.0, 0.01, alpha);
}
";
		_mat = new ShaderMaterial();
		_mat.Shader = shader;
		_rect.Material = _mat;

		_mat.SetShaderParameter("intensity",     INTENSITY_FULL);
		_mat.SetShaderParameter("inner_radius",  RADIUS_FULL);
		_mat.SetShaderParameter("base_darkness", DARKNESS_FULL);

		AddChild(_rect);

		CallDeferred(MethodName.ConnectSignals);
	}

	private void ConnectSignals()
	{
		var player = GetTree().GetFirstNodeInGroup("Player") as Player;
		if (player == null) { CallDeferred(MethodName.ConnectSignals); return; }
		player.HpChanged += OnHpChanged;
		OnHpChanged(player.CurrentHp, player.MaxHp);
	}

	private void OnHpChanged(int cur, int max)
	{
		if (max <= 0) return;
		float hpRatio = (float)cur / max;

		float intensity   = Mathf.Lerp(INTENSITY_LOW, INTENSITY_FULL, hpRatio);
		float innerRadius = Mathf.Lerp(RADIUS_LOW,    RADIUS_FULL,    hpRatio);
		float darkness    = Mathf.Lerp(DARKNESS_LOW,  DARKNESS_FULL,  hpRatio);

		_mat.SetShaderParameter("intensity",     intensity);
		_mat.SetShaderParameter("inner_radius",  innerRadius);
		_mat.SetShaderParameter("base_darkness", darkness);
	}
}
