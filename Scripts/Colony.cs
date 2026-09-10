using Godot;

// Estrutura estacionária que representa um "foco de infecção".
// Reaproveita toda a lógica de HP/dano de Enemy, mas não persegue o jogador.
public partial class Colony : Enemy
{
	public override void _Ready()
	{
		Type       = EnemyType.Bacteria;
		BaseSpeed  = 0f;
		BaseHp     = 60;
		BaseDamage = 0;
		BaseXp     = 40;
		BodyRadius = 34f;
		BodyColor  = new Color(0.55f, 0.05f, 0.05f);
		UseSprite  = false; // usa o círculo desenhado do Enemy._Draw()

		base._Ready();

		AddToGroup("Colony"); // usado pelo Minimap para desenhar o marcador
	}

	// Sobrescreve totalmente: não persegue, não bate — só existe e pulsa.
	public override void _PhysicsProcess(double delta)
	{
		float pulse = 1f + (float)Mathf.Sin(Time.GetTicksMsec() / 300.0) * 0.06f;
		Scale = new Vector2(pulse, pulse);
	}
}
