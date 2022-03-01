using System;
using System.Text;
using System.Threading.Tasks;

namespace InsightArchitectures.AnonymousUser
{
    public class Base64CookieEncoder : ICookieEncoder
    {
        public Task<string> DecodeAsync(string encodedValue)
        {
            if(string.IsNullOrWhiteSpace(encodedValue))
            {
                return Task.FromResult((string)null);
            }

            var bytes = Convert.FromBase64String(encodedValue);

            return Task.FromResult(Encoding.UTF8.GetString(bytes));
        }

        public Task<string> EncodeAsync(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult((string)null);
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            return Task.FromResult(Convert.ToBase64String(bytes));
        }
    }
}