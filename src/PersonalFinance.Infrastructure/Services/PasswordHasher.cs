using Microsoft.AspNetCore.Identity;
using PersonalFinance.Application.Common.Interfaces;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(default!, hash, password) != PasswordVerificationResult.Failed;
}
