using MongoDB.Bson.Serialization.Attributes;

namespace W3ChampionsPlayerService.Models;

public class User(string battleTag, ProfilePicture profilePicture)
{
    [BsonId]
    public string BattleTag { get; set; } = battleTag;
    public string Name { get; set; } = battleTag.Split("#")[0];
    public ProfilePicture ProfilePicture { get; set; } = profilePicture;
}

public class ProfilePicture
{
    public AvatarCategory Race { get; set; }
    public long PictureId { get; set; }
    public bool IsClassic { get; set; }
}

public enum AvatarCategory
{
    RnD = 0,
    HU = 1,
    OC = 2,
    NE = 4,
    UD = 8,
    Total = 16,
    Special = 32
}
