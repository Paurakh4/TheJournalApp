using System.Security.Cryptography;
using System.Text;

string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(bytes);
}

string targetHash = "6xf+oQlKWFsnjrCcVELnFQ5vJM8ul5XFXHJo6Dxtg0Q=";
string[] candidates = { "password", "Password", "123456", "paurakh" };

foreach (var pwd in candidates)
{
    string computedHash = HashPassword(pwd);
    Console.WriteLine($"Trying '{pwd}': {computedHash} - Match: {computedHash == targetHash}");
}
