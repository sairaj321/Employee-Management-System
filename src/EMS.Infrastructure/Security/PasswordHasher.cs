using System.Security.Cryptography;
using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace EMS.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltByteSize = 128 / 8; // 16 bytes
    private const int IterationCount = 100000;
    private const int HashByteSize = 256 / 8; // 32 bytes

    public (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = new byte[SaltByteSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: IterationCount,
            numBytesRequested: HashByteSize);

        return (Convert.ToBase64String(subkey), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
            return false;

        byte[] salt;
        try
        {
            salt = Convert.FromBase64String(storedSalt);
        }
        catch
        {
            return false;
        }

        byte[] actualSubkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: IterationCount,
            numBytesRequested: HashByteSize);

        string actualHash = Convert.ToBase64String(actualSubkey);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actualHash),
            Convert.FromBase64String(storedHash));
    }
}
