using System.Net.Mail;
using System.Text.RegularExpressions;
using PhoneNumbers;
namespace PayFlow.Domain.Entities;

public static class TransferKeyNormalizer
{
    public static string Normalize(TransferKeyType type, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 254)
            throw new ArgumentException("Chave de transfer\u00eancia inv\u00e1lida.");
        value = value.Trim();
        if (type == TransferKeyType.Email && MailAddress.TryCreate(value, out var email)
            && email.Address == value && !value.Any(char.IsWhiteSpace))
            return value.ToLowerInvariant();
        if (type == TransferKeyType.Phone && Regex.IsMatch(value, @"^\+[0-9 ()-]+$"))
        {
            try
            {
                var util = PhoneNumberUtil.GetInstance();
                var phone = util.Parse(value, null);
                if (util.IsValidNumber(phone) && !phone.HasExtension)
                    return util.Format(phone, PhoneNumberFormat.E164);
            }
            catch (NumberParseException) { }
        }
        if (type is TransferKeyType.Cpf or TransferKeyType.Cnpj && Regex.IsMatch(value, @"^[0-9 ./-]+$"))
        {
            var digits = Regex.Replace(value, @"[^0-9]", "");
            var length = type == TransferKeyType.Cpf ? 11 : 14;
            if (digits.Length == length && digits.Distinct().Count() > 1)
            {
                var valid = true;
                for (var position = length - 2; position < length; position++)
                {
                    var sum = 0;
                    for (var i = 0; i < position; i++)
                    {
                        var weight = type == TransferKeyType.Cpf ? position + 1 - i : (position - 1 - i) % 8 + 2;
                        sum += (digits[i] - '0') * weight;
                    }
                    var remainder = sum % 11;
                    valid &= digits[position] - '0' == (remainder < 2 ? 0 : 11 - remainder);
                }
                if (valid) return digits;
            }
        }
        throw new ArgumentException("Tipo ou formato da chave de transfer\u00eancia inv\u00e1lido.");
    }

    public static string? Mask(TransferKeyType? type, string? value) => value is null ? null
        : type == TransferKeyType.Email ? value
        : new string('*', value.Length - 4) + value[^4..];
}
