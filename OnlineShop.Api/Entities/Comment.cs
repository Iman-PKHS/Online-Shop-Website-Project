using System.Net.NetworkInformation;

namespace OnlineShop.Api.Entities;

public class Comment
{
    //Properties
    public int Id { get; set;}
    public int ItemId { get; set;}
    public int UserId { get; set;}
    public string Test { get; set;} = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsVisible { get; set;} = true;
    //
    
    //Navigations
    public Item Item { get; set; } = null!;
    public User User{ get; set;} = null!;
    public List<CommentReply> CommentReplies { get; set;} = new();
    //
}