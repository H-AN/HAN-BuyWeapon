using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities;


public class GiveItem2{

        
    private readonly MemoryFunctionVoid<IntPtr, string, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>? GiveNamedItem2 = new(GameData.GetSignature("GiveNamedItem2"));
    public void PlayerGiveNamedItem(CCSPlayerController player, string item)
    {
        if (!player.PlayerPawn.IsValid)
            return;
        if (player.PlayerPawn.Value == null)
            return;
        if (!player.PlayerPawn.Value.IsValid)
            return;
        if (player.PlayerPawn.Value.ItemServices == null)
            return;


        if (GiveNamedItem2 is not null)
        {
            GiveNamedItem2.Invoke(player.PlayerPawn.Value.ItemServices.Handle, item, 0, 0, 0, 0, 0, 0);
        }
        else
        {
            player.GiveNamedItem(item);
        }
    }

}

