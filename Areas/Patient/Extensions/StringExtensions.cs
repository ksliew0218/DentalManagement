namespace DentalManagement.Areas.Patient.Extensions
{
    public static class StringExtensions
    {
        public static string ToFirstUpper(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
                
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
} 