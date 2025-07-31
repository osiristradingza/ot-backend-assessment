namespace OT.Assessment.Shared.Models;

public class CasinoWager
{
    public long Id { get; set; }
    public Guid WagerId { get; set; }
    public Guid AccountId { get; set; }
    public int GameId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid BrandId { get; set; }
    public Guid ExternalReferenceId { get; set; }
    public Guid TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public int NumberOfBets { get; set; }
    public string CountryCode { get; set; }
    public string SessionData { get; set; }
    public long Duration { get; set; }
    public DateTime ProcessedDate { get; set; }
    
    public virtual Player Player { get; set; }
    public virtual Game Game { get; set; }
}