namespace OnlineShop.Api.Entities;

public class CommentReply
{
    //Propreties
    public int Id { get; set;}
    public int CommentId { get; set;}
    public int AdminUserId { get; set;}
    public string Text { get; set;} = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    //

    //Navigations
    public Comment Comment { get; set;} = null!;
    public User AdminUser { get; set;} = null!;
    //
}