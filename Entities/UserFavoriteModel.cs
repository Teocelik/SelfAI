namespace SelfAI.Entities;

/// <summary>
/// Kullanıcının favori olarak işaretlediği model (F.M.5).
/// (UserId, EndpointId) çifti unique — aynı model iki kez favorilenemez.
/// </summary>
public class UserFavoriteModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EndpointId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
