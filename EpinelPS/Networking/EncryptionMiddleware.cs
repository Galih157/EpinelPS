using System.Text.Json;
using EpinelPS.Database;
using EpinelPS.Utils;
using Paseto;
using Paseto.Builder;

namespace EpinelPS.Networking;

public class EncryptionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.ToString().StartsWith("/v1") || context.Request.Path.ToString().StartsWith("/$batch"))
        {
            var x = await PacketDecryption.DecryptOrReturnContentAsync(context);

            if (x.Contents.Length > 0)
            {
                context.Request.Body = new MemoryStream(x.Contents);
            }

            ulong userId = x.UserId;
            if (userId == 0 && context.Request.Headers.ContainsKey("Authorization"))
            {
                try
                {
                    string rawToken = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                    PasetoTokenValidationResult encryptionToken = new PasetoBuilder()
                        .Use(ProtocolVersion.V4, Purpose.Local)
                        .WithKey(ServerConfigStore.LauncherTokenKey, Encryption.SymmetricKey)
                        .Decode(rawToken, new PasetoTokenValidationParameters { ValidateLifetime = true });

                    if (encryptionToken.IsValid &&
                        encryptionToken.Paseto.Payload.TryGetValue("userId", out object? rawUserId) &&
                        rawUserId is JsonElement elem)
                    {
                        userId = elem.GetUInt64();
                    }
                }
                catch
                {
                }
            }

            if (userId != 0)
            {
                context.Items["UserID"] = userId;
            }
        }

        await _next(context);
    }
}
