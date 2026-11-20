using System.Globalization;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Safe helpers for hexadecimal color strings. Gauntlet's <c>Color.ConvertStringToColor</c> (used by
	/// <c>Brush.FontColor</c> bindings) requires the exact <c>#RRGGBBAA</c> format and throws on any other
	/// length or non-hex character, which crashes the game. Use these helpers to validate user or
	/// scenario-file input before it reaches the UI.
	/// </summary>
	public static class HexColor
	{
		/// <summary>Fallback used when a value can't be salvaged. Opaque white, matching subtitle defaults.</summary>
		public const string Default = "#FFFFFFFF";

		/// <summary>Returns true when the string is already in the exact #RRGGBBAA format expected by Gauntlet.</summary>
		public static bool IsValid(string hex)
		{
			return hex != null && hex.Length == 9 && hex[0] == '#' && IsHexDigits(hex, 1);
		}

		/// <summary>
		/// Converts tolerant input to the canonical #RRGGBBAA form. Accepts #RGB, #RGBA, #RRGGBB and #RRGGBBAA
		/// (hash optional, case-insensitive, shorthand digits doubled). Returns the fallback when parsing is impossible.
		/// </summary>
		public static string Normalize(string hex, string fallback = Default)
		{
			return TryNormalize(hex, out string result) ? result : (IsValid(fallback) ? fallback : Default);
		}

		/// <summary>Tries to convert tolerant input to the canonical #RRGGBBAA form. Never throws.</summary>
		public static bool TryNormalize(string hex, out string result)
		{
			result = null;
			if (string.IsNullOrEmpty(hex)) return false;

			int start = hex[0] == '#' ? 1 : 0;
			int digitCount = hex.Length - start;
			if (digitCount != 3 && digitCount != 4 && digitCount != 6 && digitCount != 8) return false;
			if (!IsHexDigits(hex, start)) return false;

			string digits = hex.Substring(start);
			if (digitCount == 3 || digitCount == 4)
			{
				char[] expanded = new char[digitCount * 2];
				for (int i = 0; i < digitCount; i++)
				{
					expanded[i * 2] = digits[i];
					expanded[i * 2 + 1] = digits[i];
				}
				digits = new string(expanded);
			}
			if (digits.Length == 6) digits += "FF";

			result = "#" + digits.ToUpperInvariant();
			return true;
		}

		/// <summary>Tries to extract the R, G, B, A bytes (0-255) of a tolerant hex string. Never throws.</summary>
		public static bool TryGetBytes(string hex, out byte r, out byte g, out byte b, out byte a)
		{
			r = 255; g = 255; b = 255; a = 255;
			if (!TryNormalize(hex, out string normalized)) return false;
			r = byte.Parse(normalized.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			g = byte.Parse(normalized.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			b = byte.Parse(normalized.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			a = byte.Parse(normalized.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return true;
		}

		/// <summary>Builds a canonical #RRGGBBAA string from R, G, B, A bytes.</summary>
		public static string FromBytes(byte r, byte g, byte b, byte a)
		{
			return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
		}

		private static bool IsHexDigits(string s, int start)
		{
			for (int i = start; i < s.Length; i++)
			{
				char c = s[i];
				bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
				if (!isHex) return false;
			}
			return true;
		}
	}
}
