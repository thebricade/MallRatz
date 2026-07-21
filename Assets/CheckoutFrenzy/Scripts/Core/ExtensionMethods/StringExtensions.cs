namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts PascalCase or camelCase string to Title Case (e.g., "ProductType" -> "Product Type").
        /// </summary>
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return System.Text.RegularExpressions.Regex
                .Replace(input, "(?<!^)([A-Z])", " $1")
                .Trim();
        }
    }
}
