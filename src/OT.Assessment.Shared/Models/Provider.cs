namespace OT.Assessment.Shared.Models;

public class Provider
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public virtual ICollection<Game> Games { get; set; } = new List<Game>();
}