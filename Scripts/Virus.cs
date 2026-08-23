using Godot;

public partial class Virus : Enemy
{
	public override void _Ready()
	{
		Type       = EnemyType.Virus;
		BaseSpeed  = 55f;
		BaseHp     = 25;
		BaseDamage = 5;
		BaseXp     = 35;
		BodyRadius = 22f;
		UseSprite  = true; // ← diz ao Enemy para não desenhar o círculo

		base._Ready();

		// Sprite
		var sprite = new Sprite2D();
		sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/virus.png");
		sprite.Scale   = new Vector2(0.8f, 0.8f);
		AddChild(sprite);

		// Luz igual ao player — azul/roxo brilhante
		var light = new PointLight2D();
		light.Color         = new Color(0.6f, 0.2f, 1f);
		light.Energy        = 1.2f;
		light.TextureScale  = 3.5f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);
	}
}
