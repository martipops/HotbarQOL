using HotbarQOL;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

// This is kind of stupid. dont do this.
public class HotbarPlayer : ModPlayer {
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (HotbarEdit.SwapKeybind.JustPressed) {
            HotbarEdit.SwapBar(this.Player);
        }
    }
}