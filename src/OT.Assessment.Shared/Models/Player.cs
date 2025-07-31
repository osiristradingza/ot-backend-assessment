namespace OT.Assessment.Shared.Models;

public class Player
{
    public Guid AccountId { get; set; }
    public string Username { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public virtual ICollection<CasinoWager> CasinoWagers { get; set; } = new List<CasinoWager>();
}