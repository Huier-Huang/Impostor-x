using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Impostor.Api.Http;
using Impostor.Server.Net;
using Microsoft.AspNetCore.Mvc;

namespace Impostor.Server.Http;

/// <summary>
///     This controller has a method to get an auth token.
/// </summary>
[Route("/api/user")]
[ApiController]
public sealed class TokenController(PlayerAuthInfoManager _playerAuthInfoManager) : ControllerBase
{
    /// <summary>
    ///     Get an authentication token.
    /// </summary>
    /// <param name="request">Token parameters that need to be put into the token.</param>
    /// <returns>A bare minimum authentication token that the client will accept.</returns>
    [HttpPost]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        var token = new Token
        {
            Content = new TokenPayload
            {
                ProductUserId = request.ProductUserId,
                ClientVersion = request.ClientVersion,
            },
            Hash = "impostor_was_here",
        };
        _playerAuthInfoManager.Register(request, token);

        // Wrap into a Base64 sandwich
        return Ok(token.Serialize());
    }
}
