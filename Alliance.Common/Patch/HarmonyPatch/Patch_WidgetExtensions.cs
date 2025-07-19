#if !SERVER
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch WidgetExtensions to tell useful information when failing a binding.
	/// </summary>
	class Patch_WidgetExtensions
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_WidgetExtensions));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					typeof(WidgetExtensions).GetMethod(nameof(WidgetExtensions.SetWidgetAttributeFromString), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_WidgetExtensions).GetMethod(
						nameof(Prefix_SetWidgetAttributeFromString), BindingFlags.Static | BindingFlags.Public)));

			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_WidgetExtensions)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Replaces the original SetWidgetAttributeFromString method to provide better error handling and logging.
		/// </summary>
		public static bool Prefix_SetWidgetAttributeFromString(
			object target,
			string name,
			string value,
			BrushFactory brushFactory,
			SpriteData spriteData,
			Dictionary<string, VisualDefinitionTemplate> visualDefinitionTemplates,
			Dictionary<string, ConstantDefinition> constants,
			Dictionary<string, WidgetAttributeTemplate> parameters,
			Dictionary<string, XmlElement> customElements,
			Dictionary<string, string> defaultParameters)
		{
			try
			{
				object obj;
				PropertyInfo propertyInfo;
				GetObjectAndProperty(target, name, 0, out obj, out propertyInfo);

				if (propertyInfo == null)
				{
					Log($"Property '{name}' not found on target type '{target?.GetType()?.FullName}'.", LogLevel.Error);
					return false;
				}

				Type propType = propertyInfo.PropertyType;

				if (propType == typeof(int))
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { Convert.ToInt32(value) });
				else if (propType == typeof(float))
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { Convert.ToSingle(value, CultureInfo.InvariantCulture) });
				else if (propType == typeof(bool))
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { value == "true" });
				else if (propType == typeof(string))
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { value });
				else if (propType == typeof(Brush))
				{
					if (brushFactory == null)
					{
						Log("BrushFactory is null.", LogLevel.Error);
						return false;
					}
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { brushFactory.GetBrush(value) });
				}
				else if (propType == typeof(Sprite))
				{
					if (spriteData == null)
					{
						Log("SpriteData is null.", LogLevel.Error);
						return false;
					}
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { spriteData.GetSprite(value) });
				}
				else if (propType.IsEnum)
				{
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { Enum.Parse(propType, value) });
				}
				else if (propType == typeof(Color))
				{
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { Color.ConvertStringToColor(value) });
				}
				else if (propType == typeof(XmlElement))
				{
					if (customElements == null || !customElements.ContainsKey(value))
					{
						Log($"Custom element '{value}' not found.", LogLevel.Error);
						return false;
					}
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { customElements[value] });
				}
				else if (typeof(Widget).IsAssignableFrom(propType))
				{
					if (target is not Widget widget)
					{
						Log($"Target is not a Widget when resolving child widget '{value}'.", LogLevel.Error);
						return false;
					}

					Widget child = widget.FindChild(new BindingPath(value));
					if (child == null)
					{
						Log($"Widget path '{value}' could not be resolved from '{widget.GetFullIDPath()}'.", LogLevel.Error);
						return false;
					}
					propertyInfo.GetSetMethod().Invoke(obj, new object[] { child });
				}
				else if (propType == typeof(VisualDefinition))
				{
					if (!visualDefinitionTemplates.ContainsKey(value))
					{
						Log($"VisualDefinition '{value}' not found.", LogLevel.Error);
						return false;
					}
					propertyInfo.GetSetMethod().Invoke(obj, new object[] {
						visualDefinitionTemplates[value].CreateVisualDefinition(
							brushFactory, spriteData, visualDefinitionTemplates,
							constants, parameters, defaultParameters)
					});
				}
				else
				{
					Log($"Unsupported property type '{propType.FullName}' for '{name}'.", LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				Log($"Exception while binding '{name}' = '{value}' on '{target?.GetType()?.FullName}': {ex.Message}", LogLevel.Error);
			}

			// Skip original method
			return false;
		}

		private static void GetObjectAndProperty(object parent, string name, int nameStartIndex, out object targetObject, out PropertyInfo targetPropertyInfo)
		{
			int num = name.IndexOf('.', nameStartIndex);
			PropertyInfo property = parent.GetType().GetProperty((num >= 0) ? name.Substring(nameStartIndex, num) : ((nameStartIndex > 0) ? name.Substring(nameStartIndex) : name), (BindingFlags)20);
			if (!(property != null))
			{
				targetPropertyInfo = null;
				targetObject = null;
				return;
			}
			if (num < 0)
			{
				targetObject = parent;
				targetPropertyInfo = property;
				return;
			}
			GetObjectAndProperty(property.GetGetMethod().Invoke(parent, new object[0]), name, num + 1, out targetObject, out targetPropertyInfo);
		}
	}
}
#endif