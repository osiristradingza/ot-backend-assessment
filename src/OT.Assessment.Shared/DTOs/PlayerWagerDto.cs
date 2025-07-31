namespace OT.Assessment.Shared.DTOs;

public class PlayerWagerDto
{
    public string WagerId { get; set; }
    public string Game { get; set; }
    public string Provider { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class PlayerWagersResponseDto
{
    public List<PlayerWagerDto> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}