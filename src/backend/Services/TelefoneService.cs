
namespace Parking.Api.Services
{
    public static class TelefoneService
    {
        public static string? Normalizar(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone)) return null;
            var digits = new string(telefone.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? null : digits;
        }
    }
}
