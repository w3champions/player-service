using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace player_service.Authentication;
public class W3CAuthenticationService
{
    private static readonly string JwtPublicKey = Regex.Unescape(Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY") ?? "");

    public W3CUserAuthentication? GetUserByToken(string? jwt)
    {
        return W3CUserAuthentication.FromJWT(jwt, JwtPublicKey);
    }
}

public class W3CUserAuthentication
{
    public static W3CUserAuthentication? FromJWT(string? jwt, string publicKey)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateTokenReplay = false,
                ValidateActor = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa)
            };

            var handler = new JwtSecurityTokenHandler();
            var claims = handler.ValidateToken(jwt, validationParameters, out _);
            var btag = claims.Claims.First(c => c.Type == "battleTag").Value;

            return new W3CUserAuthentication
            {
                BattleTag = btag,
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public required string BattleTag { get; set; }
}
