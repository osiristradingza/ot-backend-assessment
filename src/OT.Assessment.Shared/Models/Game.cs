namespace OT.Assessment.Shared.Models;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Theme { get; set; }
    public int ProviderId { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public virtual Provider Provider { get; set; }
    public virtual ICollection<CasinoWager> CasinoWagers { get; set; } = new List<CasinoWager>();
}