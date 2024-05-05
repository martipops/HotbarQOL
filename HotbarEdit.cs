using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
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
        public static bool IsSwappedBar = false;
        public static (int, int) SlotRange = (0, 10);
        private static int slotCount = 10;

        public static ModKeybind SwapKeybind { get; private set; }

        public override void Load()
        {
            On_Main.GUIHotbarDrawInner += On_Main_GUIHotbarDrawInner;
            On_Player.ScrollHotbar += On_Player_ScrollHotbar;
            IL_Player.Update += IL_Player_Update;
            IL_Player.GetItem += IL_Player_GetItem;

            // Keybinds
            SwapKeybind = KeybindLoader.RegisterKeybind(Mod, "SwapKeybind", "CapsLock");
            slotCount = 10;

        }



        public override void Unload()
        {
            On_Main.GUIHotbarDrawInner -= On_Main_GUIHotbarDrawInner;
            On_Player.ScrollHotbar -= On_Player_ScrollHotbar;
            IL_Player.Update -= IL_Player_Update;
            IL_Player.GetItem -= IL_Player_GetItem;

            // Keybinds
            SwapKeybind = null;
            IsSwappedBar = false;
        }

        // If enabled, picked-up items will stack into the lastmost slots of the inventory
        private void IL_Player_GetItem(ILContext il)
        {
            ILCursor cur = new(il);
            if (!cur.TryGotoNext(MoveType.After, i => i.MatchLdfld(typeof(Item).GetField("useStyle")))) return;
            cur.EmitDelegate(HotbarItemPickupOverride);
        }

        // TODO: Remove this delegate entirely.
        public static bool HotbarItemPickupOverride(int useStyle)
        {
            return !(useStyle == 0 || Config.Instance.deprioritizeInventory);
        }


        // Refresh config on world load
        public override void OnWorldLoad()
        {
            Config.Instance.OnChanged();
        }


        // Never forget this BS. Cant beleive i fixed this
        // Fixes bug where switching to an item while using it does 
        // not go to the correct position while the hotbar is swapped.
        private void IL_Player_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            for (int i = 0; i < 10; i++)
            {
                if (!c.TryGotoNext(MoveType.Before, i => i.MatchStfld(typeof(Player).GetField("selectedItem")))) return;
                c.EmitDelegate(GetHotbarOffset);
                c.Index += 2;
            }
            if (!c.TryGotoNext(MoveType.After, i => i.MatchStloc(50))) return;
            for (int i = 0; i < 10; i++)
            {
                if (!c.TryGotoNext(MoveType.Before, i => i.MatchStloc(50))) return;
                c.EmitDelegate(GetHotbarOffset);
                c.Index += 2;
            }

        }

        public static int GetHotbarOffset(int i)
        {
            return Math.Min(i + SlotRange.Item1, 50);
        }

        public static void SwapBar(Player player)
        {
            if (Config.Instance.numSlots >= 50)
            {
                IsSwappedBar = false;
                return;
            }
            IsSwappedBar = !IsSwappedBar;
            UpdateSlotCount();
            if (IsSwappedBar)
            {
                player.selectedItem += SlotRange.Item1 % slotCount;
            }
            else
            {
                player.selectedItem -= SlotRange.Item2;
            }
        }

        // TODO: simplify this bs
        public static void UpdateSlotCount()
        {
            if (Config.Instance == null) return;
            if (IsSwappedBar)
            {
                slotCount = Math.Min(Config.Instance.numSlots, 50 - Config.Instance.numSlots);
            }
            else
            {
                slotCount = Config.Instance.numSlots;
            }
            updateSlotRange();
        }

        private static void updateSlotRange()
        {
            if (IsSwappedBar)
            {
                SlotRange = (Config.Instance.numSlots, Config.Instance.numSlots + slotCount);
            }
            else
            {
                SlotRange = (0, Config.Instance.numSlots);
            }
        }


        // Modified terraria source. updates the hotbar on scroll depending where the slot is.
        private void On_Player_ScrollHotbar(On_Player.orig_ScrollHotbar orig, Player self, int scrollAmount)
        {
            int slotPos = self.selectedItem;

            // Offset the `selected item position` to be relative to 0
            if (IsSwappedBar)
            {
                slotPos -= SlotRange.Item1;
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

            // Handle manual (click) select
            if (self.changeItem >= 0)
            {
                if (slotPos != self.changeItem)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                slotPos = self.changeItem;
                self.changeItem = -1;
            }

            // gracefully handle bounds *again*
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
            self.selectedItem = slotPos + SlotRange.Item1;
        }

        // Modified terraria source code?
        private void On_Main_GUIHotbarDrawInner(On_Main.orig_GUIHotbarDrawInner orig, Main self)
        {
            int rowLength = slotCount / Config.Instance.numRows;
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
                xOff += screenWidth - (46 * rowLength);
                if (Config.Instance.hAlign == HorizontalAlignment.Center)
                    xOff /= 2;
            }
            if (Config.Instance.vAlign == VerticalAlignment.Bottom)
            {
                yOff += screenHeight - 72;
            }
            DynamicSpriteFontExtensionMethods.DrawString(
                position: new Vector2(23 * rowLength - FontAssets.MouseText.Value.MeasureString(text).X / 2 + xOff, 0f + yOff),
                spriteBatch: spriteBatch,
                spriteFont: FontAssets.MouseText.Value,
                text: text,
                color: new Color(mouseTextColor, mouseTextColor, mouseTextColor, mouseTextColor), 
                rotation: 0f, 
                origin: default(Vector2),
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: 0f
                );
            int num = xOff;
            for (int i = 0; i < slotCount; i++)
            {
                if (i % rowLength == 0 && i != 0)
                {
                    yOff += 48 + Config.Instance.rowGap;
                    num = xOff;
                }

                //Swap Keybind
                int j = i;
                j += SlotRange.Item1;


                if (j == player[myPlayer].selectedItem)
                {
                    if (hotbarScale[i] < 1f)
                    {
                        hotbarScale[i] += 0.05f;
                    }
                }
                else if ((double)hotbarScale[i] > 0.75)
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
            if (selectedItem >= SlotRange.Item2 || selectedItem < SlotRange.Item1 && (selectedItem != 58 || mouseItem.type > ItemID.None))
            {
                float num4 = 1f;
                int num5 = (int)(20f + 22f * (1f - num4)) + yOff;
                int a2 = (int)(75f + 150f * num4);
                Color lightColor2 = new Color(255, 255, 255, a2);
                float num7 = inventoryScale;
                inventoryScale = num4;
                ItemSlot.Draw(spriteBatch, player[myPlayer].inventory, 13, selectedItem, new Vector2(num, num5), lightColor2);
                inventoryScale = num7;
            }
        }
    }
}
