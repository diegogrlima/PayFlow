using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using PayFlow.Features.Authentication.Interfaces;

namespace PayFlow.Infrastructure.Authentication
{
    public class Argon2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;

        private const int MemorySize = 19456;
        private const int Iterations = 2;
        private const int DegreeOfParallelism = 1;

        public string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            using var argon2 = CreateArgon2(
                passwordBytes,
                salt,
                MemorySize,
                Iterations,
                DegreeOfParallelism);

            byte[] hash = argon2.GetBytes(HashSize);

            return FormatHash(
                MemorySize,
                Iterations,
                DegreeOfParallelism,
                salt,
                hash);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                !TryParseHash(
                    passwordHash,
                    out int memorySize,
                    out int iterations,
                    out int degreeOfParallelism,
                    out byte[] salt,
                    out byte[] hash))
            {
                return false;
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            using var argon2 = CreateArgon2(
                passwordBytes,
                salt,
                memorySize,
                iterations,
                degreeOfParallelism);

            byte[] computedHash = argon2.GetBytes(hash.Length);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }

        private static Argon2id CreateArgon2(
            byte[] password,
            byte[] salt,
            int memorySize,
            int iterations,
            int degreeOfParallelism)
        {
            return new Argon2id(password)
            {
                Salt = salt,
                MemorySize = memorySize,
                Iterations = iterations,
                DegreeOfParallelism = degreeOfParallelism
            };
        }

        private static string FormatHash(
            int memorySize,
            int iterations,
            int degreeOfParallelism,
            byte[] salt,
            byte[] hash)
        {
            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            return $"{memorySize}${iterations}${degreeOfParallelism}${saltBase64}${hashBase64}";
        }

        private static bool TryParseHash(
            string passwordHash,
            out int memorySize,
            out int iterations,
            out int degreeOfParallelism,
            out byte[] salt,
            out byte[] hash)
        {
            memorySize = 0;
            iterations = 0;
            degreeOfParallelism = 0;
            salt = [];
            hash = [];

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            string[] parts = passwordHash.Split('$');

            if (parts.Length != 5 ||
                !int.TryParse(parts[0], out memorySize) ||
                !int.TryParse(parts[1], out iterations) ||
                !int.TryParse(parts[2], out degreeOfParallelism) ||
                memorySize != MemorySize ||
                iterations != Iterations ||
                degreeOfParallelism != DegreeOfParallelism)
            {
                return false;
            }

            try
            {
                salt = Convert.FromBase64String(parts[3]);
                hash = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            return salt.Length == SaltSize && hash.Length == HashSize;
        }
    }
}
