using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace player_service_net_test4.Authentication;
public class W3CAuthenticationService : IW3CAuthenticationService
{
    private static readonly string JwtPublicKey = Regex.Unescape(Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY") ?? "");

    public W3CUserAuthentication GetUserByToken(string jwt)
    {
        return W3CUserAuthentication.FromJWT(jwt, JwtPublicKey);
    }
}

public interface IW3CAuthenticationService
{
    W3CUserAuthentication GetUserByToken(string jwt);
}

public class W3CUserAuthentication
{
    public static W3CUserAuthentication FromJWT(string jwt, string publicKey)
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
            var isAdmin = bool.Parse(claims.Claims.First(c => c.Type == "isAdmin").Value);
            var name = claims.Claims.First(c => c.Type == "name").Value;

            return new W3CUserAuthentication
            {
                Name = name,
                BattleTag = btag,
                IsAdmin = isAdmin
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string BattleTag { get; set; }
    public string Name { get; set; }
    public bool IsAdmin { get; set; }
}
