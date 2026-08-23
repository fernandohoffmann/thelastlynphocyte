using Godot;

public partial class Vignette : CanvasLayer
{
	private ColorRect      _rect;
	private ShaderMaterial _mat;

	public override void _Ready()
	{
		Layer = 4;

		_rect = new ColorRect();
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;

		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;
uniform float intensity : hint_range(0.0, 1.0) = 0.97;
uniform float inner_radius : hint_range(0.0, 1.0) = 0.3;
void fragment() {
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv) * 2.0;
    float vignette = smoothstep(inner_radius, 1.4, dist);
    COLOR = vec4(0.0, 0.0, 0.0, vignette * intensity);
}
";
		_mat = new ShaderMaterial();
		_mat.Shader = shader;
		_rect.Material = _mat;

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

		// Quanto menos HP, mais intensa e mais próxima do centro a vinheta
		float intensity   = Mathf.Lerp(0.97f, 0.55f, hpRatio); // 100% hp = 0.55, 0% hp = 0.97
		float innerRadius = Mathf.Lerp(0.05f, 0.35f, hpRatio); // mais perto do centro quando hp baixo

		_mat.SetShaderParameter("intensity", intensity);
		_mat.SetShaderParameter("inner_radius", innerRadius);
	}
}
