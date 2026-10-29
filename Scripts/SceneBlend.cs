// SceneBlend.cs — integra inimigos E player ao cenario
// (luz ambiente, rim light, sombra de contato e wobble de membrana pro player)
using Godot;

public static class SceneBlend
{
	// ── INIMIGOS ──────────────────────────────────────────────────────────
	public static readonly Color AMBIENT_COLOR = new Color(0.85f, 0.30f, 0.24f);
	public const float AMBIENT_AMOUNT = 0.35f;
	public const float DARKEN         = 0.22f;
	public const float DESATURATE     = 0.25f;
	public const float HAZE           = 0.10f;
	public const float RIM_STRENGTH   = 0.90f;
	public const float RIM_PIXELS     = 3.0f;

	// ── PLAYER (dose menor, pra continuar legivel) ────────────────────────
	public const float PLAYER_AMBIENT  = 0.16f; // quanto o vermelho do tecido tinge
	public const float PLAYER_DARKEN   = 0.08f;
	public const float PLAYER_HAZE     = 0.05f;
	public const float PLAYER_RIM      = 0.55f;
	public const float PLAYER_PAD      = 0.14f; // margem pro wobble nao cortar a borda
	public const float WOBBLE_AMOUNT   = 0.045f;// intensidade da deformacao da membrana
	public const float WOBBLE_SPEED    = 1.6f;  // velocidade da deformacao

	// ── SOMBRA DE CONTATO ─────────────────────────────────────────────────
	public const float SHADOW_ALPHA   = 0.55f;
	public const float SHADOW_SIZE    = 3.0f;
	public static readonly Vector2 SHADOW_OFFSET = new Vector2(5f, 7f);
	// ──────────────────────────────────────────────────────────────────────

	private static Shader    _enemyShader;
	private static Shader    _playerShader;
	private static Texture2D _shadowTex;

	public static Shader EnemyShader
	{
		get { if (_enemyShader == null) _enemyShader = BuildEnemyShader(); return _enemyShader; }
	}

	public static Shader PlayerShader
	{
		get { if (_playerShader == null) _playerShader = BuildPlayerShader(); return _playerShader; }
	}

	public static Texture2D ShadowTexture
	{
		get { if (_shadowTex == null) _shadowTex = BuildShadowTexture(); return _shadowTex; }
	}

	// ══════════════════════════════════════════════════════════════════════
	private static Shader BuildEnemyShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

uniform vec3  ambient_color  : source_color = vec3(0.85, 0.30, 0.24);
uniform float ambient_amount : hint_range(0.0, 1.0)  = 0.35;
uniform float darken         : hint_range(0.0, 1.0)  = 0.22;
uniform float desaturate     : hint_range(0.0, 1.0)  = 0.25;
uniform float haze           : hint_range(0.0, 1.0)  = 0.10;
uniform vec3  rim_color      : source_color = vec3(1.0, 0.25, 0.15);
uniform float rim_strength   : hint_range(0.0, 3.0)  = 0.90;
uniform float rim_width      : hint_range(0.5, 80.0) = 8.0;
uniform float flash          : hint_range(0.0, 1.0)  = 0.0;

// Recoloracao: mistura o pixel com a luminancia dele tingida.
// Usar a luminancia (em vez de multiplicar a cor original) preserva o
// sombreado do sprite — multiplicar verde por roxo daria uma mancha suja.
uniform vec3  recolor : source_color = vec3(1.0, 1.0, 1.0);
uniform float recolor_amount : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec3 c = tex.rgb;

    float lum = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(c, vec3(lum), desaturate);
    c = mix(c, vec3(lum) * recolor * 1.7, recolor_amount);
    c *= mix(vec3(1.0), ambient_color, ambient_amount);
    c *= (1.0 - darken);

    vec2  o  = TEXTURE_PIXEL_SIZE * rim_width;
    float aN = texture(TEXTURE, UV + vec2(0.0, -o.y)).a;
    float aS = texture(TEXTURE, UV + vec2(0.0,  o.y)).a;
    float aE = texture(TEXTURE, UV + vec2( o.x, 0.0)).a;
    float aW = texture(TEXTURE, UV + vec2(-o.x, 0.0)).a;
    float rim = clamp(tex.a - (aN + aS + aE + aW) * 0.25, 0.0, 1.0);
    c += rim_color * rim * rim_strength;

    c = mix(c, ambient_color * 0.30, haze);
    c = mix(c, vec3(1.0, 0.30, 0.30), flash);

    COLOR = vec4(c, tex.a);
}
";
		return shader;
	}

	// ══════════════════════════════════════════════════════════════════════
	private static Shader BuildPlayerShader()
	{
		var shader = new Shader();
		shader.Code = @"
shader_type canvas_item;

// region_* resolvem o caso de AtlasTexture, onde o UV NAO vai de 0 a 1
uniform vec2  region_min  = vec2(0.0);
uniform vec2  region_size = vec2(1.0);

uniform float pad           : hint_range(0.0, 0.5)  = 0.14;
uniform float wobble_amount : hint_range(0.0, 0.25) = 0.045;
uniform float wobble_speed  : hint_range(0.0, 5.0)  = 1.6;

uniform vec4  tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

uniform vec3  ambient_color  : source_color = vec3(0.90, 0.45, 0.40);
uniform float ambient_amount : hint_range(0.0, 1.0)  = 0.16;
uniform float darken         : hint_range(0.0, 1.0)  = 0.08;
uniform float haze           : hint_range(0.0, 1.0)  = 0.05;
uniform vec3  rim_color      : source_color = vec3(1.0, 0.30, 0.20);
uniform float rim_strength   : hint_range(0.0, 3.0)  = 0.55;
uniform float rim_width      : hint_range(0.5, 80.0) = 20.0;

void fragment() {
    // UV local do sprite, 0..1, independente de atlas
    vec2 luv = (UV - region_min) / region_size;

    // sobra uma margem (pad) pra membrana poder inchar sem cortar a borda
    vec2 p = (luv - vec2(0.5)) * (1.0 + pad);
    float r   = length(p);
    float ang = atan(p.y, p.x);

    // duas ondas de frequencias diferentes = deformacao organica, nunca simetrica
    float w = sin(ang * 3.0 + TIME * wobble_speed) * 0.60
            + sin(ang * 2.0 - TIME * wobble_speed * 0.73 + 1.7) * 0.40;

    // o centro quase nao mexe, so a borda deforma
    p *= 1.0 + w * wobble_amount * smoothstep(0.0, 0.35, r);

    vec2 luv2 = p + vec2(0.5);

    if (luv2.x < 0.0 || luv2.x > 1.0 || luv2.y < 0.0 || luv2.y > 1.0) {
        COLOR = vec4(0.0);
    } else {
        vec2 uv  = region_min + luv2 * region_size;
        vec2 lo  = region_min;
        vec2 hi  = region_min + region_size;
        vec4 tex = texture(TEXTURE, uv);
        vec3 c   = tex.rgb;

        c *= mix(vec3(1.0), ambient_color, ambient_amount);
        c *= (1.0 - darken);

        vec2  o  = TEXTURE_PIXEL_SIZE * rim_width;
        float aN = texture(TEXTURE, clamp(uv + vec2(0.0, -o.y), lo, hi)).a;
        float aS = texture(TEXTURE, clamp(uv + vec2(0.0,  o.y), lo, hi)).a;
        float aE = texture(TEXTURE, clamp(uv + vec2( o.x, 0.0), lo, hi)).a;
        float aW = texture(TEXTURE, clamp(uv + vec2(-o.x, 0.0), lo, hi)).a;
        float rim = clamp(tex.a - (aN + aS + aE + aW) * 0.25, 0.0, 1.0);
        c += rim_color * rim * rim_strength;

        c = mix(c, ambient_color * 0.30, haze);

        COLOR = vec4(c * tint.rgb, tex.a * tint.a);
    }
}
";
		return shader;
	}

	// ══════════════════════════════════════════════════════════════════════
	private static Texture2D BuildShadowTexture()
	{
		var grad = new Gradient();
		grad.SetOffset(0, 0f);
		grad.SetColor(0, new Color(0f, 0f, 0f, 1f));
		grad.SetOffset(1, 1f);
		grad.SetColor(1, new Color(0f, 0f, 0f, 0f));
		grad.AddPoint(0.45f, new Color(0f, 0f, 0f, 0.85f));

		return new GradientTexture2D
		{
			Gradient = grad,
			Width    = 128,
			Height   = 128,
			Fill     = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo   = new Vector2(1.0f, 0.5f)
		};
	}

	public static ShaderMaterial MakeEnemyMaterial(float spriteScale)
	{
		var mat = new ShaderMaterial { Shader = EnemyShader };
		mat.SetShaderParameter("ambient_color",  AMBIENT_COLOR);
		mat.SetShaderParameter("ambient_amount", AMBIENT_AMOUNT);
		mat.SetShaderParameter("darken",         DARKEN);
		mat.SetShaderParameter("desaturate",     DESATURATE);
		mat.SetShaderParameter("haze",           HAZE);
		mat.SetShaderParameter("rim_strength",   RIM_STRENGTH);
		mat.SetShaderParameter("rim_width",      RIM_PIXELS / Mathf.Max(0.01f, spriteScale));
		mat.SetShaderParameter("flash",          0f);
		mat.SetShaderParameter("recolor",        Colors.White);
		mat.SetShaderParameter("recolor_amount", 0f);
		return mat;
	}

	public static ShaderMaterial MakePlayerMaterial(float spriteScale)
	{
		var mat = new ShaderMaterial { Shader = PlayerShader };
		mat.SetShaderParameter("ambient_color",  AMBIENT_COLOR);
		mat.SetShaderParameter("ambient_amount", PLAYER_AMBIENT);
		mat.SetShaderParameter("darken",         PLAYER_DARKEN);
		mat.SetShaderParameter("haze",           PLAYER_HAZE);
		mat.SetShaderParameter("rim_strength",   PLAYER_RIM);
		mat.SetShaderParameter("rim_width",      RIM_PIXELS / Mathf.Max(0.01f, spriteScale));
		mat.SetShaderParameter("pad",            PLAYER_PAD);
		mat.SetShaderParameter("wobble_amount",  WOBBLE_AMOUNT);
		mat.SetShaderParameter("wobble_speed",   WOBBLE_SPEED);
		mat.SetShaderParameter("tint",           Colors.White);
		mat.SetShaderParameter("region_min",     Vector2.Zero);
		mat.SetShaderParameter("region_size",    Vector2.One);
		return mat;
	}

	/// Necessario quando o sprite usa AtlasTexture (SpriteFrames), pois nesse caso
	/// o UV do shader NAO vai de 0 a 1 — ele cobre a regiao dentro do atlas.
	public static void ApplyRegionUniforms(ShaderMaterial mat, Texture2D tex)
	{
		if (mat == null) return;

		Vector2 min  = Vector2.Zero;
		Vector2 size = Vector2.One;

		if (tex is AtlasTexture atlas && atlas.Atlas != null)
		{
			var full = new Vector2(atlas.Atlas.GetWidth(), atlas.Atlas.GetHeight());
			if (full.X > 0f && full.Y > 0f)
			{
				min  = atlas.Region.Position / full;
				size = atlas.Region.Size / full;
			}
		}

		mat.SetShaderParameter("region_min",  min);
		mat.SetShaderParameter("region_size", size);
	}

	/// Sombra de contato que faz o corpo "pousar" no tecido.
	/// alpha < 0 usa SHADOW_ALPHA.
	public static Sprite2D MakeContactShadow(float bodyRadius, float alpha = -1f)
	{
		float a        = alpha < 0f ? SHADOW_ALPHA : alpha;
		float diameter = bodyRadius * SHADOW_SIZE;

		return new Sprite2D
		{
			Name             = "ContactShadow",
			Texture          = ShadowTexture,
			Modulate         = new Color(0f, 0f, 0f, a),
			Position         = SHADOW_OFFSET,
			Scale            = new Vector2(diameter / 128f, diameter / 128f),
			ZIndex           = -1,
			ZAsRelative      = true,
			ShowBehindParent = false
		};
	}
}
