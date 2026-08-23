using Godot;

public partial class Bacteria : Enemy
{
	public override void _Ready()
	{
		Type       = EnemyType.Bacteria;
		BaseSpeed  = 75f;
		BaseHp     = 8;
		BaseDamage = 2;
		BaseXp     = 10;
		BodyRadius = 18f;
		UseSprite  = true;

		base._Ready();

		var sprite = new Sprite2D();
		sprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/bateria.png");
		sprite.Scale   = new Vector2(0.06f, 0.06f); // ajuste se necessário
		AddChild(sprite);

		var light = new PointLight2D();
		light.Color         = new Color(0.2f, 1f, 0.3f);
		light.Energy        = 0.6f;
		light.TextureScale  = 2.0f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);
	}
}
