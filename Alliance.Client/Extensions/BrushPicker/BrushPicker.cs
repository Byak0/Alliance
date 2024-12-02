using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.BrushPicker
{
    public class BrushPicker
    {
        private static BrushPicker _instance;
        public static BrushPicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BrushPicker();
                }
                return _instance;
            }
        }

        public List<BrushInfos> BrushList { get; set; }

        private string _filePath;

        private BrushPicker()
        {
            BrushList = new List<BrushInfos>();

            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "Alliance", "BrushList.xml");
        }

        public void LoadBrush(UIContext context)
        {
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
            // LOAD EVERYTHING
            foreach (KeyValuePair<string, SpriteCategory> sc in spriteData.SpriteCategories)
            {
                sc.Value.Load(resourceContext, uiResourceDepot);
            }
            int index = 0;
            foreach (Brush brush in context.BrushFactory.Brushes)
            {
                string spriteCategory = "";
                string spriteName = "";
                if (brush.Sprite?.Name != null)
                {
                    spriteName = brush.Sprite.Name;
                    foreach (KeyValuePair<string, SpriteCategory> sc in spriteData.SpriteCategories)
                    {
                        foreach (SpritePart sp in sc.Value.SpriteParts)
                        {
                            if (sp.Name.Contains(brush.Sprite.Name))
                            {
                                spriteCategory = sc.Key;
                            }
                        }
                    }
                }
                BrushList.Add(new BrushInfos(index, brush.Name, spriteName, spriteCategory));
                index++;
            }
        }

        public void Serialize()
        {
            try
            {
                using (FileStream fs = new FileStream(_filePath, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(BrushPicker));
                    serializer.Serialize(fs, this);
                }
            }
            catch (Exception ex)
            {
                Log("Alliance - Failed to save brush list.", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }

    [Serializable]
    public class BrushInfos
    {
        public int Index { get; set; }
        public string BrushName { get; set; }
        public string SpriteName { get; set; }
        public string SpriteCategory { get; set; }

        public BrushInfos() { }

        public BrushInfos(int index, string brushName, string spriteName, string spriteCategory)
        {
            Index = index;
            BrushName = brushName;
            SpriteName = spriteName;
            SpriteCategory = spriteCategory;
        }
    }
}