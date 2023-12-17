using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Client.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using static Alliance.Common.Utilities.Logger;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Alliance.Client.Core.KeyBinder
{
    /// <summary>
    /// Static class to help register new keys in the system.
    /// Requires Patch_KeyBinder to work.
    /// </summary>
    public static class KeyBinder
    {
        /// <summary>
        /// Use this flag if you don't want to use the KeyBinder at all
        /// </summary>
        public static bool DontUseKeyBinder = false;

        public static readonly ICollection<BindedKeyCategory> KeysCategories = new List<BindedKeyCategory>();

        public static readonly IDictionary<string, GameKeyBinderContext> KeyContexts = new Dictionary<string, GameKeyBinderContext>();

        /// <summary>
        /// Register a new category of keys
        /// </summary>
        /// <param name="group"></param>
        public static void RegisterKeyGroup(BindedKeyCategory group)
        {
            KeysCategories.Add(group);
        }

        /// <summary>
        /// Initialize all Keys category. Set Names & Description for Main UI. 
        /// Add the keys to the system
        /// </summary>
        public static void Initialize()
        {
            if (DontUseKeyBinder) return;

            AutoRegister();

            foreach (BindedKeyCategory cat in KeysCategories)
            {
                // Create the games context for each category
                KeyContexts[cat.CategoryId] = new GameKeyBinderContext(cat.CategoryId, cat.Keys);

                // Set up category name in the menu
                GameText gameText = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_category_name");
                gameText.AddVariationWithId(cat.CategoryId, new TextObject(cat.Category, null), new List<GameTextManager.ChoiceTag>());

                foreach (BindedKey key in cat.Keys)
                {
                    // Set up key name in the menu
                    string text = cat.CategoryId;
                    GameText gameText2 = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name");
                    string variationId = text + "_" + key.KeyId.ToString();
                    gameText2.AddVariationWithId(variationId, new TextObject(key.Name, null), new List<GameTextManager.ChoiceTag>());


                    // Set up key description in the menu
                    GameText gameText3 = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description");
                    gameText3.AddVariationWithId(variationId, new TextObject(key.Description, null), new List<GameTextManager.ChoiceTag>());
                }
            }
        }

        /// <summary>
        /// Automatically discover and register all IUseKeyBinder implementations.
        /// </summary>
        private static void AutoRegister()
        {
            try
            {
                IEnumerable<Type> binderTypes = Assembly.GetExecutingAssembly()
                                            .GetTypes()
                                            .Where(t => t.GetInterfaces().Contains(typeof(IUseKeyBinder)) && !t.IsAbstract);

                foreach (Type type in binderTypes)
                {
                    if (Activator.CreateInstance(type) is IUseKeyBinder binder)
                    {
                        BindedKeyCategory category = binder.BindedKeys;
                        KeysCategories.Add(category);
                    }
                }

                Log($"Alliance - Successfully registered {KeysCategories.Count} key binding categories", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Error while registering KeyBinder categories", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
