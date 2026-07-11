using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Application.Contracts.Services;
using Domain.Entities.Users;

namespace Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var jwtSecret = _configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Secret en appsettings.");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SmartInventoryAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "SmartInventoryClient";
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "120");

            // Claims cortos ("role", "nameid") para que [Authorize(Roles=...)] funcione
            // con MapInboundClaims = false en JwtBearer.
            var claims = new List<Claim>
            {
                new("nameid", user.Id.ToString()),
                new("unique_name", user.Name.Value),
                new("email", user.Email.Value),
                new("role", user.Role.Name.Value),
            };

            if (user.CustomerId is int customerId)
                claims.Add(new Claim("customerId", customerId.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}