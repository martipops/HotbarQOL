using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tModPorter;

namespace HotbarQOL
{

    public enum HorizontalAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    public enum VerticalAlignment
    {
        Top = 0,
        Bottom = 1
    }
    class Config : ModConfig
    {
        public static Config Instance = ModContent.GetInstance<Config>();
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("HotbarSettings")]
        [DefaultValue(10), Range(1, 50)]
        public int numSlots;

        [DefaultValue(1), Range(1, 5)]
        public int numRows;

        [Header("HotbarPosition")]

        [DefaultValue(VerticalAlignment.Top)]
        public VerticalAlignment vAlign;

        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment hAlign;


        [DefaultValue(20), Range(-1200, 1200)]
        public int xOffset;

        [DefaultValue(0), Range(-1200, 1200)]
        public int yOffset;

        [Header("ItemSwapping")]

        [DefaultValue(true)]
        public bool itemSwapper;

        [DefaultValue(200), Range(100, 1000)]
        public int doubleTapSpeed;

        [DefaultValue(false)]
        public bool deprioritizeInventory;

        [Header("Advanced")]

        [DefaultValue(0), Range(-32, 32)]
        public int rowGap;


        public override void OnChanged()
        {
            Main.hotbarScale = new float[numSlots];
            Main.hotbarScale[0] = 1f;
            for (int i = 1; i < numSlots; i++) Main.hotbarScale[i] = 0.75f;
            HotbarEdit.UpdateSlotCount();
            if (numSlots >= 50) 
                HotbarEdit.IsSwappedBar = false;
        }
    }
}
