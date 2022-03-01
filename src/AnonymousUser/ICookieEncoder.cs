using System.Threading.Tasks;

namespace InsightArchitectures.AnonymousUser
{
    public interface ICookieEncoder
    {
         Task<string> EncodeAsync(string value);

         Task<string> DecodeAsync(string encodedValue);
    }
}