using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Renders phrase templates as plain text (no UI), used for read-only previews such as list-item
	/// labels (<c>[PhrasePreview]</c>). Also exposes the shared condition/brace helpers reused by the
	/// editor's interactive phrase parser, so there is a single source of truth for the condition syntax.
	/// </summary>
	public static class PhraseTextRenderer
	{
		/// <summary>
		/// Renders a template against <paramref name="obj"/> as a plain string. Supports literal text,
		/// <c>{Field}</c>, <c>{Field|Label0|Label1|...}</c> (the currently selected label) and
		/// <c>{?Condition: content}</c> conditional spans.
		/// </summary>
		public static string RenderText(string template, object obj)
		{
			if (obj == null || string.IsNullOrEmpty(template)) return string.Empty;
			StringBuilder sb = new StringBuilder();
			RenderInto(sb, template, obj);
			return sb.ToString();
		}

		private static void RenderInto(StringBuilder sb, string text, object obj)
		{
			int i = 0;
			while (i < text.Length)
			{
				int open = text.IndexOf('{', i);
				if (open < 0)
				{
					sb.Append(text, i, text.Length - i);
					break;
				}
				if (open > i) sb.Append(text, i, open - i);

				int close = FindMatchingBrace(text, open);
				if (close < 0)
				{
					sb.Append(text, open, text.Length - open);
					break;
				}

				string block = text.Substring(open + 1, close - open - 1);
				i = close + 1;

				// Conditional span: {?condition: content}
				if (block.StartsWith("?"))
				{
					string inner = block.Substring(1);
					int sep = inner.IndexOf(':');
					string condition = (sep >= 0 ? inner.Substring(0, sep) : inner).Trim();
					string content = sep >= 0 ? inner.Substring(sep + 1) : string.Empty;
					if (IsConditionSatisfied(condition, obj)) RenderInto(sb, content, obj);
					continue;
				}

				// Collection count: {#Field}
				if (block.StartsWith("#"))
				{
					string countField = block.Substring(1).Trim();
					FieldInfo cf = obj.GetType().GetField(countField, BindingFlags.Instance | BindingFlags.Public);
					object cval = cf?.GetValue(obj);
					if (cval is ICollection col) sb.Append(col.Count);
					else if (cval is IEnumerable cenum && !(cval is string))
					{
						int n = 0;
						foreach (var _ in cenum) n++;
						sb.Append(n);
					}
					else if (cf != null) sb.Append(0);
					continue;
				}

				int bar = block.IndexOf('|');
				string fieldName = (bar >= 0 ? block.Substring(0, bar) : block).Trim();
				FieldInfo fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
				object val = fi?.GetValue(obj);

				// Collection field : render each item's preview
				if (val is IEnumerable enumerable && val is not string)
				{
					bool first = true;
					foreach (object item in enumerable)
					{
						if (!first && item is not ValueSource<string>) sb.Append(", ");
						first = false;
						sb.Append(ScenarioEditorHelper.GetItemDisplayName(item));
					}
					continue;
				}

				if (bar >= 0)
				{
					string[] labels = block.Substring(bar + 1).Split('|');
					int idx = ChoiceIndexOf(val);
					if (idx >= 0 && idx < labels.Length) sb.Append(labels[idx]);
					else if (val != null) sb.Append(ValueToString(val));
				}
				else if (val != null)
				{
					sb.Append(ValueToString(val));
				}
			}
		}

		private static int ChoiceIndexOf(object val)
		{
			if (val is bool b) return b ? 1 : 0;
			if (val is Enum)
			{
				Array vals = Enum.GetValues(val.GetType());
				return Array.IndexOf(vals, val);
			}
			return -1;
		}

		/// <summary>Returns the index of the '}' matching the '{' at <paramref name="openIndex"/>, or -1 if unmatched.</summary>
		public static int FindMatchingBrace(string text, int openIndex)
		{
			int depth = 0;
			for (int k = openIndex; k < text.Length; k++)
			{
				if (text[k] == '{') depth++;
				else if (text[k] == '}')
				{
					depth--;
					if (depth == 0) return k;
				}
			}
			return -1;
		}

		/// <summary>
		/// Evaluates a phrase condition against an object. Supported forms:
		/// <c>Field</c> (bool true), <c>!Field</c> (bool false), <c>Field=Value1,Value2</c>,
		/// <c>Field==Value</c>, <c>Field!=Value1,Value2</c>. Matching is case-insensitive; enums match by name.
		/// </summary>
		public static bool IsConditionSatisfied(string condition, object obj)
		{
			if (obj == null || string.IsNullOrWhiteSpace(condition)) return false;

			int neIdx = condition.IndexOf("!=");
			if (neIdx >= 0)
			{
				return !ContainsValue(obj, condition.Substring(0, neIdx), condition.Substring(neIdx + 2));
			}

			int eqIdx = condition.IndexOf('=');
			if (eqIdx >= 0)
			{
				string valPart = condition.Substring(eqIdx + 1);
				if (valPart.StartsWith("=")) valPart = valPart.Substring(1); // tolerate '=='
				return ContainsValue(obj, condition.Substring(0, eqIdx), valPart);
			}

			// Bare bool field, optional leading '!' negation
			bool negate = condition.StartsWith("!");
			string boolField = (negate ? condition.Substring(1) : condition).Trim();
			FieldInfo fi = obj.GetType().GetField(boolField, BindingFlags.Instance | BindingFlags.Public);
			if (fi == null) return false;
			object val = fi.GetValue(obj);
			bool match;
			if (val is bool b) match = b;
			else if (val is IEnumerable enumerable && !(val is string)) match = enumerable.GetEnumerator().MoveNext();
			else match = false;
			return negate ? !match : match;
		}

		private static bool ContainsValue(object obj, string fieldPart, string valueList)
		{
			FieldInfo fi = obj.GetType().GetField(fieldPart.Trim(), BindingFlags.Instance | BindingFlags.Public);
			if (fi == null) return false;
			string valStr = ValueToString(fi.GetValue(obj));
			foreach (string v in valueList.Split(','))
			{
				if (string.Equals(v.Trim(), valStr, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private static string ValueToString(object val)
		{
			if (val == null) return string.Empty;
			if (val is Enum) return Enum.GetName(val.GetType(), val) ?? val.ToString();
			PhrasePreviewAttribute preview = val.GetType().GetCustomAttribute<PhrasePreviewAttribute>();
			if (preview != null)
			{
				return RenderText(preview.Template, val);
			}
			// LocalizedString-like value: render its default (English) text.
			MethodInfo getText = val.GetType().GetMethod("GetText", new[] { typeof(string) });
			if (getText != null) return getText.Invoke(val, new object[] { "English" }) as string ?? string.Empty;
			return val.ToString();
		}
	}
}
