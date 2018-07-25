using System.Text.RegularExpressions;

namespace Vostok.Logging.Core
{
    internal static class RegexExtensions
    {
        public static string GetValueOrNull(this Group group) => 
            group.Success ? group.Value : null;
    }
}