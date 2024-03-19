using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static Terraria.Main;

namespace HotbarQOL
{
    public class HotbarEdit : ModSystem
    {
        public static bool swappedBar = false;
        private static int slotCount = 10;
        private static (int, int) slotRange = (0, 10);

        public static ModKeybind SwapKeybind { get; private set; }

        public override void Load()
        {
            On_Main.GUIHotbarDrawInner += On_Main_GUIHotbarDrawInner;
            On_Player.ScrollHotbar += On_Player_ScrollHotbar;

            // Keybinds
            SwapKeybind = KeybindLoader.RegisterKeybind(Mod, "SwapKeybind", "CapsLock");
            slotCount = 10;

        }

        public override void Unload()
        {
            On_Main.GUIHotbarDrawInner -= On_Main_GUIHotbarDrawInner;
            On_Player.ScrollHotbar -= On_Player_ScrollHotbar;

            // Keybinds
            SwapKeybind = null;
            swappedBar = false;
        }
        public override void OnWorldLoad()
        {
            Config.Instance.OnChanged();
        }

        public static void SwapBar(Player player) {
            if (Config.Instance.numSlots >= 50) {
                swappedBar = false;
                return;
            }
            swappedBar = !swappedBar;
            UpdateSlotCount();
            if (swappedBar) {
                player.selectedItem += slotRange.Item1 % slotCount;
            } else {
                player.selectedItem -= slotRange.Item2;
            }
        }
        public static void UpdateSlotCount() {
            if (Config.Instance == null) return;
            if (swappedBar) {
                slotCount = Math.Min(Config.Instance.numSlots, 50 - Config.Instance.numSlots);
            } else {
                slotCount = Config.Instance.numSlots;
            }
            updateSlotRange();
        }

        private static void updateSlotRange() {
            if(swappedBar) {
                slotRange = (Config.Instance.numSlots, Config.Instance.numSlots + slotCount);
            } else {
                slotRange = (0, Config.Instance.numSlots);
            }
        }



        private void On_Player_ScrollHotbar(On_Player.orig_ScrollHotbar orig, Player self, int scrollAmount)
        {
            int slotPos = self.selectedItem;

            // Offset the `selected item position` to be relative to 0
            if (swappedBar) {
                slotPos -= slotRange.Item1;
            }

            // Prevent selected item from out of bounds of hotbar
            if (slotPos > slotCount)
            {
                self.selectedItem %= slotCount;
                return;
            } 
            
            // Overscroll handling
            while (scrollAmount > slotCount - 1)
            {
                scrollAmount -= slotCount;
            }
            while (scrollAmount < 0)
            {
                scrollAmount += slotCount;
            }
            slotPos += scrollAmount;

            // Handle scrolling
            if (scrollAmount != 0)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                int num = slotPos - scrollAmount;
                self.DpadRadial.ChangeSelection(-1);
                self.CircularRadial.ChangeSelection(-1);
                slotPos = num + scrollAmount;
                self.nonTorch = -1;
            }

            // Handle manual select
            if (self.changeItem >= 0)
            {
                if (slotPos != self.changeItem)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                slotPos = self.changeItem;
                self.changeItem = -1;
            }

            // idek tbh
            if (self.itemAnimation == 0 && self.selectedItem != 58)
            {
                while (slotPos > slotCount - 1)
                {
                    slotPos -= slotCount;
                }
                while (slotPos < 0)
                {
                    slotPos += slotCount;
                }
            }

            //set item to updated position + offset
            self.selectedItem = slotPos + slotRange.Item1;
        }

        private void On_Main_GUIHotbarDrawInner(On_Main.orig_GUIHotbarDrawInner orig, Main self)
        {
            if (playerInventory || player[myPlayer].ghost)
            {
                return;
            }
            string text = Language.GetText("LegacyInterface.37").Value;
            if (player[myPlayer].inventory[player[myPlayer].selectedItem].Name != null && player[myPlayer].inventory[player[myPlayer].selectedItem].Name != "")
            {
                text = player[myPlayer].inventory[player[myPlayer].selectedItem].AffixName();
            }
            int xOff = Config.Instance.xOffset,
                yOff = Config.Instance.yOffset;
            if (Config.Instance.hAlign != HorizontalAlignment.Left)
            {
                xOff += screenWidth - (46 * slotCount);
                if (Config.Instance.hAlign == HorizontalAlignment.Center)
                    xOff /= 2;
            }
            if (Config.Instance.vAlign == VerticalAlignment.Bottom) {
                yOff += screenHeight - 72;
            }
            DynamicSpriteFontExtensionMethods.DrawString(position: new Vector2(23 * slotCount - FontAssets.MouseText.Value.MeasureString(text).X/2 + xOff, 0f + yOff), spriteBatch: spriteBatch, spriteFont: FontAssets.MouseText.Value, text: text, color: new Color(mouseTextColor, mouseTextColor, mouseTextColor, mouseTextColor), rotation: 0f, origin: default(Vector2), scale: 1f, effects: SpriteEffects.None, layerDepth: 0f);
            int num = xOff;
            for (int i = 0; i < slotCount; i++)
            {
                //Swap Keybind
                int j = i;
                j += slotRange.Item1;


                if (j == player[myPlayer].selectedItem)
                {
                    if (hotbarScale[i] < 1f)
                    {
                        hotbarScale[i] += 0.05f;
                    }
                }
                else if ((double) hotbarScale[i] > 0.75)
                {
                    hotbarScale[i] -= 0.05f;
                }

                float num2 = hotbarScale[i];
                int num3 = (int)(20f + 22f * (1f - num2)) + yOff;
                int a = (int)(75f + 150f * num2);
                Color lightColor = new Color(255, 255, 255, a);
                if (!player[myPlayer].hbLocked && !PlayerInput.IgnoreMouseInterface && mouseX >= num && (float)mouseX <= (float)num + (float)TextureAssets.InventoryBack.Width() * hotbarScale[i] && mouseY >= num3 && (float)mouseY <= (float)num3 + (float)TextureAssets.InventoryBack.Height() * hotbarScale[i] && !player[myPlayer].channel)
                {
                    player[myPlayer].mouseInterface = true;
                    player[myPlayer].cursorItemIconEnabled = false;
                    if (mouseLeft && !player[myPlayer].hbLocked && !blockMouse)
                    {
                        player[myPlayer].changeItem = i;
                    }
                    hoverItemName = player[myPlayer].inventory[j].AffixName();
                    if (player[myPlayer].inventory[j].stack > 1)
                    {
                        hoverItemName = hoverItemName + " (" + player[myPlayer].inventory[j].stack + ")";
                    }
                    rare = player[myPlayer].inventory[j].rare;
                }
                float num6 = inventoryScale;
                inventoryScale = num2;
                ItemSlot.Draw(spriteBatch, player[myPlayer].inventory, 13, j, new Vector2(num, num3), lightColor);
                inventoryScale = num6;
                num += (int)((float)TextureAssets.InventoryBack.Width() * hotbarScale[i]) + 4;
            }
            int selectedItem = player[myPlayer].selectedItem;
            // Main.NewText($"item:{selectedItem}, {slotRange.Item1}, {slotRange.Item2}");
            if (selectedItem >= slotRange.Item2 || selectedItem < slotRange.Item1 && (selectedItem != 58 || mouseItem.type > ItemID.None))
            {
                float num4 = 1f;
                int num5 = (int)(20f + 22f * (1f - num4)) + yOff;
                int a2 = (int)(75f + 150f * num4);
                Color lightColor2 = new Color(255, 255, 255, a2);
                float num7 = inventoryScale;
                inventoryScale = num4;
                ItemSlot.Draw(spriteBatch, player[myPlayer].inventory, 13, selectedItem, new Vector2(num,num5), lightColor2);
                inventoryScale = num7;
            }
        }
    }
}
