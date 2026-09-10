using Godot;

public partial class ChagasBoss : Enemy
{
	public static ChagasBoss Instance { get; private set; }

	public override void _Ready()
	{
		Type       = EnemyType.Virus;
		BaseSpeed  = 40f;
		BaseHp     = 1200;
		BaseDamage = 12;
		BaseXp     = 500;
		BodyRadius = 60f;
		BodyColor  = new Color(0.35f, 0.05f, 0.45f);
		UseSprite  = false; // sem sprite ainda — círculo roxo grande como placeholder

		base._Ready();
		Instance = this;

		var light = new PointLight2D();
		light.Color         = new Color(0.6f, 0.1f, 0.6f);
		light.Energy        = 1.5f;
		light.TextureScale  = 6f;
		light.BlendMode     = Light2D.BlendModeEnum.Add;
		light.Texture       = GD.Load<Texture2D>("res://Assets/light_texture.tres");
		light.ShadowEnabled = false;
		AddChild(light);
	}

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;
	}
}
