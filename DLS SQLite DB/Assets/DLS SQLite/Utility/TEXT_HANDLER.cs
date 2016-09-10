/* This is the String Handler, this handles string operations to REGEX text and filter text.
 *This is designed to be used for the editor but can be used in a variety of situations you just add it to the end
 *of a string or a string based variable and you can have the operations perfromed anywhere as long as your are
 * using the MDF_EDITOR namespace.
 * example "This is test 67".TextOnly()  : This will return "This_is_test_"
*/
using System.Text.RegularExpressions;


public static class TEXT_HANDLER {

		// This will FORCE the size of the string to whatever you want the size to always be
		public static string ForceSize(this string value, int set_length)
		{ 
			return value.PadRight(set_length).Substring(0, set_length);
		}

		// This will limit the size by the amount of characters, so a max of 5 would be "abcde" 
		public static string MinSize(this string s, int min_Length)
		{
			return s != null && s.Length < min_Length ? s.Substring(0,min_Length) : s;
		}
		
		// This will limit the size by the amount of characters, so a max of 5 would be "abcde" 
		public static string MaxSize(this string s, int maxLength)
		{
			return s != null && s.Length > maxLength ? s.Substring(0, maxLength) : s;
		}
		// This will remove all special characters, EXCEPT for Underscores.
		public static string NoSpecialCharacters(this string s)
		{
			s = Regex.Replace(s, @"[^a-zA-Z0-9_ ]", "");
			s = s.Replace(" ", "_");
			return s;
		}
		// This will force text only EXCEPT for Underscores
		public static string TextOnly(this string s)
		{
			s = Regex.Replace(s, @"[^a-zA-Z_ ]", "");
			s = s.Replace(" ", "_");
			return s;
		}
		// This will force Quotes around text. (REALLY USEFUL)
		public static string AddQuotes(this string s)
		{
			s = "\"" + s + "\"";
			return s;
		}
		// THis will force Numbers ONLY!
		public static string NumbersOnly(this string s)
		{
			s = Regex.Replace(s, @"[0-9]", "");
			return s;
		}
	}
