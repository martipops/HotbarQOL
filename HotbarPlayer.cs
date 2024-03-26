using HotbarQOL;
using System.Diagnostics;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;


public class HotbarPlayer : ModPlayer
{

    private static bool[] slotPresses = { false, false, false, false, false, false, false, false, false, false };
    private static bool[] slotOneShots = { false, false, false, false, false, false, false, false, false, false };
    private static long[] lastShots = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static Stopwatch slotWatch;

    public override void OnEnterWorld()
    {
        slotWatch = Stopwatch.StartNew();
    }

    //Restart watch when died cuz why not *BECAUSE IT CAUSES BUGS but F it*
    public override void OnRespawn()
    {
        for (int i = 0; i < slotPresses.Length; i++)
        {
            slotPresses[i] = false;
            slotOneShots[i] = false;
            lastShots[i] = 0;
        }
        slotWatch = Stopwatch.StartNew();
        base.OnRespawn();
    }

    public override void Load()
    {
        slotWatch = new Stopwatch();
    }

    public override void Unload()
    {
        slotWatch = null;
    }

    private void processAllOneShots(in TriggersSet triggersSet)
    {
        if (!Config.Instance.itemSwapper) return;
        processOneShot(triggersSet.Hotbar1, 0);
        processOneShot(triggersSet.Hotbar2, 1);
        processOneShot(triggersSet.Hotbar3, 2);
        processOneShot(triggersSet.Hotbar4, 3);
        processOneShot(triggersSet.Hotbar5, 4);
        processOneShot(triggersSet.Hotbar6, 5);
        processOneShot(triggersSet.Hotbar7, 6);
        processOneShot(triggersSet.Hotbar8, 7);
        processOneShot(triggersSet.Hotbar9, 8);
        processOneShot(triggersSet.Hotbar10, 9);
    }

    private void processOneShot(bool keyState, int i)
    {
        if (keyState)
        {
            if (slotPresses[i] == false)
            {
                slotPresses[i] = true;
                slotOneShots[i] = true;
            }
        }
        else
        {
            slotPresses[i] = false;
        }
    }

    private void handleAllOneShots()
    {
        if (!Config.Instance.itemSwapper) return;
        for (int i = 0; i < slotOneShots.Length; i++)
        {
            if (slotOneShots[i] == true)
            {
                handleOneShot(i);
                slotOneShots[i] = false;
            }
        }
    }

    private void handleOneShot(int i)
    {
        if (slotWatch.ElapsedMilliseconds - lastShots[i] < Config.Instance.doubleTapSpeed)
        {
            swapItem(i);
        }
        lastShots[i] = slotWatch.ElapsedMilliseconds;
    }

    // TODO: Simplify this
    private void swapItem(int i)
    {
        int destSlot, swapSlot;
        if (HotbarEdit.IsSwappedBar) {
            destSlot = i;
            swapSlot = i + HotbarEdit.SlotRange.Item1;
        } else {
            destSlot = i + HotbarEdit.SlotRange.Item2;
            swapSlot = i;
        }
        Item itemCache = Player.inventory[destSlot].Clone();
        if (destSlot < 0 || destSlot > 50) return;
        if (swapSlot < 0 || swapSlot > 50) return;
        Player.inventory[destSlot] = Player.inventory[swapSlot].Clone();
        Player.inventory[swapSlot] = itemCache.Clone();
    }


    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        processAllOneShots(in triggersSet);

        if (!Main.hasFocus ||
            Player.itemAnimation != 0 ||
            !Player.ItemTimeIsZero ||
            Player.reuseDelay != 0 ||
            Main.drawingPlayerChat ||
            Player.selectedItem == 58 ||
            Main.editSign ||
            Main.editChest)
        {
            return;
        }

        if (HotbarEdit.SwapKeybind.JustPressed)
        {
            HotbarEdit.SwapBar(Player);
        }

        handleAllOneShots();


    }

}