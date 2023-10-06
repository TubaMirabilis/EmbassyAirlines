namespace EaIdentity.Infrastructure.Identity;

public class RefreshToken
{
    public required string Token { get; set; }
    public required string JwtId { get; set; }
    public required DateTime CreationDate { get; set; }
    public required DateTime ExpiryDate { get; set; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}