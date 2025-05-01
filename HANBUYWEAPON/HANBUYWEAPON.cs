using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Capabilities;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Modules.Events;


namespace HanZriotWeaponPlugin;

public class HanZriotWeapon : BasePlugin
{
    public override string ModuleName => "[华仔]武器购买";
    public override string ModuleVersion => "2.1.0";
    public override string ModuleAuthor => "By : 华仔H-AN";
    public override string ModuleDescription => "华仔武器购买菜单,QQ群107866133";

    private Dictionary<int, CPointWorldText> PlayerMenuEntities = new Dictionary<int, CPointWorldText>();
    public string WordText { get; set; }

    GiveItem2 giveItem = new GiveItem2();

    private Dictionary<string, string> WeaponCommands = new Dictionary<string, string>
    {
        { "[道具]回血针", "css_hs" },
        { "[步枪]AK-47", "css_ak"  },
        { "[步枪]加利尔", "css_galil" },
        { "[步枪]M4A4", "css_m4" },
        { "[步枪]M4A1", "css_m4a1" },
        { "[步枪]SG553", "css_sg553" },
        { "[机枪]内格夫", "css_negev" },
        { "[机枪]M249", "css_m249" },
        { "[冲锋枪]野牛", "css_bizon" },
        { "[冲锋枪]P90", "css_p90" },
        { "[冲锋枪]车王ump45", "css_ump" },
        { "[冲锋枪]Mp7", "css_mp7" },
        { "[冲锋枪]Mp5", "css_mp5" },
        { "[狙击枪]大狙AWP", "css_awp" },
        { "[狙击枪]轻狙", "css_scout" },
        { "[狙击枪]G3SG1", "css_g3sg1" },
        { "[狙击枪]Scar20", "css_scar" },
        { "[手枪]CZ75", "css_cz75" },
        { "[手枪]TEC9", "css_tec" },
        { "[手枪]R8左轮", "css_r8" },
        { "[霰弹枪]连喷锯短", "css_off" },
        { "[霰弹枪]连喷xm1014", "css_xm1014" },
        { "[霰弹枪]警喷MAG-7", "css_mag7" },
        { "[霰弹枪]匪喷Nova", "css_nova" },
        { "[投掷物]破片手雷", "css_he" },
        { "[投掷物]燃烧弹", "css_fire" },
        { "[投掷物]燃烧瓶", "css_molo" }
    };

    public static string WeaponRestrict = Path.Combine(Application.RootDirectory, "configs/ZriotDate/HanZombieRiotWeaponRestrict.txt");
    public Dictionary<string, int> restrictedWeaponsWithPrice = new Dictionary<string, int>();

    private Dictionary<int, int> playerCurrentSelections = new Dictionary<int, int>();

    // 当前页码，控制每页显示多少武器
    private Dictionary<int, int> playerCurrentPages = new Dictionary<int, int>(); // 保存玩家当前所在页


    private bool[] MenuOpen = new bool[65];
    private bool[] MenuCd = new bool[65];

    public override void Load(bool hotReload)
    { 
        LoadWeaponRestrictions();
        EventInitialize();

        AddCommand("css_buy", "buymenu", buymenu);
        AddCommand("css_market", "buymenu", buymenu);
        AddCommand("css_weapon", "buymenu", buymenu);
        AddCommand("css_wp", "buymenu", buymenu);

        AddCommand("css_ak", "buyak47", BUYAK47);
        AddCommand("css_galil", "buygalil", BUYGALIL);
        AddCommand("css_cz75", "buycz75", BUYCZ75);
        AddCommand("css_tec", "buytec9", BUYTEC9);
        AddCommand("css_r8", "buyr8", BUYR8);
        AddCommand("css_off", "buym3", BUYM3);
        AddCommand("css_ump", "buyump45", BUYUMP);
        AddCommand("css_sg553", "buysg556", BUYSG556);
        AddCommand("css_g3sg1", "buyg3sg1", BUYG3SG1);
        AddCommand("css_m4", "buym4a4", BUYM4A4);
        AddCommand("css_m4a1", "buym4a1", BUYM4A1);
        AddCommand("css_m249", "buym249", BUYM249);
        AddCommand("css_negev", "buynegev", BUYNEGEV);
        AddCommand("css_bizon", "bizon", BUYBIZON);
        AddCommand("css_p90", "p90", BUYP90);
        AddCommand("css_awp", "awp", BUYAWP);
        AddCommand("css_scar", "scar", BUYSCAR);    
        AddCommand("css_he", "he", BUYhe);
        AddCommand("css_fire", "fire", BUYfire);
        AddCommand("css_molo", "fire", BUYMolotov);
        AddCommand("css_hs", "healthshot", healthshot);
        AddCommand("css_mp7", "mp7", BUYMP7);
        AddCommand("css_scout", "scout", BUYSCOUT);

        AddCommand("css_mp5", "mp5", BUYMP5);
        AddCommand("css_xm1014", "xm1014", BUYXM1014);
        AddCommand("css_mag7", "mag7", BUYMAG7);
        AddCommand("css_nova", "nova", BUYNOVA);
    }

    public void EventInitialize() 
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<CheckTransmit>(OnTransmit);

    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnChat(EventPlayerChat @event, GameEventInfo info)
    {
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (@event.Text.Trim() == "buy" || @event.Text.Trim() == "BUY" ||
            @event.Text.Trim() == "WEAPON" || @event.Text.Trim() == "weapon" ||
            @event.Text.Trim() == "MARKET" || @event.Text.Trim() == "market" ||
            @event.Text.Trim() == "WP" || @event.Text.Trim() == "wp")
        {
            buymenu(player, null!);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var client = @event.Userid;
        if(client == null)
        return HookResult.Continue;
        Remove(client);
        MenuOpen[client.Slot] = false;
        return HookResult.Continue;

    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        List<CCSPlayerController> playerlist2 = Utilities.GetPlayers();
        foreach (var client in playerlist2)
        {
            Remove(client);
            MenuOpen[client.Slot] = false; 
        }
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    { 
        List<CCSPlayerController> playerlist2 = Utilities.GetPlayers();
        foreach (var client in playerlist2)
        {
            Remove(client);
            MenuOpen[client.Slot] = false; 
        }
        return HookResult.Continue;
    }

    void OnTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || !player.IsValid || !player.Pawn.IsValid || player.Pawn.Value == null) continue;

            // 只让当前玩家看到自己菜单中的内容
            //if (PlayerMenuEntities[player.Slot] != null)
            //{
                // 只允许菜单传输给打开菜单的玩家
            //    info.TransmitEntities.Remove(PlayerMenuEntities[player.Slot]);
            //}
            // 清除不属于该玩家的其他菜单（如果需要）
            foreach (var item in PlayerMenuEntities)
            {
                int slot = item.Key; // 获取字典项的键
                CPointWorldText menuEntity = item.Value; // 获取字典项的值
                
                // 你可以在这里执行相关操作，比如移除不属于当前玩家的菜单
                if (slot != player.Slot && menuEntity != null)
                {
                    info.TransmitEntities.Remove(menuEntity);
                }
                
                
            }
            
        }
    }

    private void OnTick()
    {
        
        List<CCSPlayerController> playerlist = Utilities.GetPlayers();
        foreach (var client in playerlist)
        {
            if (client?.PlayerPawn?.Value?.MovementServices?.Buttons.ButtonStates.Length > 0 && MenuOpen[client.Slot] == true)
            {
                var buttons = (PlayerButtons)client.PlayerPawn.Value.MovementServices.Buttons.ButtonStates[0];
                HandlePlayerInput(client, buttons);
            }
        }
    }

    private void HandlePlayerInput(CCSPlayerController client, PlayerButtons buttons)
    {
        // 检测按键：W、A、S、D、E、Shift
        if ((buttons & PlayerButtons.Forward) == PlayerButtons.Forward && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} W");
            ScrollMenu(client, -1); // W 向上滚动
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Back) == PlayerButtons.Back && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} S");
            ScrollMenu(client, 1); // S 向下滚动
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Use) == PlayerButtons.Use && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} E");
            SelectWeapon(client); // 选择当前皮肤
            CloseMenu(client); // 关闭菜单
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        if ((buttons & PlayerButtons.Speed) == PlayerButtons.Speed && MenuCd[client.Slot] == false)
        {
            //client.PrintToChat($" {ChatColors.Green}[玩家按下]{ChatColors.Default} Shift");
            CloseMenu(client); // 关闭菜单
            MenuCd[client.Slot] = true;
            AddTimer(0.3f, () => {  MenuCd[client.Slot] = false;});
        }
        
    }



    private void SelectWeapon(CCSPlayerController client)
    {
        string selectedWeapon = GetSelectedWeapon(client);  // 获取当前选择的武器
        if (!string.IsNullOrEmpty(selectedWeapon) && WeaponCommands.ContainsKey(selectedWeapon))
        {
            string command = WeaponCommands[selectedWeapon];  // 获取对应的指令
            client.ExecuteClientCommandFromServer(command);
            //client.PrintToChat($"你已经购买了: {selectedWeapon}");
        }
    }

    // 获取当前选择的武器（例如根据玩家的选择，选中的索引）
    private string GetSelectedWeapon(CCSPlayerController client)
    {
        // 你需要实现从菜单中获取玩家当前选择的武器
        // 假设 playerCurrentSelections 字典保存了玩家选择项的索引
        int currentSelection = playerCurrentSelections.ContainsKey(client.Slot) ? playerCurrentSelections[client.Slot] : 0;

        // 获取武器列表中的对应武器
        var weaponList = WeaponCommands.Keys.ToList();
        return weaponList.Count > currentSelection ? weaponList[currentSelection] : null;
    }

    private void ScrollMenu(CCSPlayerController client, int direction)
    {
        List<string> weaponList = WeaponCommands.Keys.ToList();
        int totalWeapons = weaponList.Count;

        // 每页显示的武器项数
        const int itemsPerPage = 5;

        // 获取玩家的当前页码和选择项，确保每个玩家都有独立的翻页状态
        if (!playerCurrentSelections.ContainsKey(client.Slot))
        {
            playerCurrentSelections[client.Slot] = 0; // 默认选择第一个武器
        }

        int currentPage = playerCurrentSelections[client.Slot] / itemsPerPage + 1; // 计算当前页码
        int currentSelection = playerCurrentSelections[client.Slot] % itemsPerPage; // 当前页内的选项

        // 计算最大页码
        int maxPage = (int)Math.Ceiling((double)totalWeapons / itemsPerPage);

        // 根据方向滚动菜单，确保页码和选项不会越界
        if (direction == 1) // 向下滚动（选择下一项）
        {
            //if (currentSelection < itemsPerPage - 1)
            if (currentSelection < itemsPerPage - 1 && (currentPage - 1) * itemsPerPage + currentSelection + 1 < totalWeapons)
            {
                // 如果当前页面还有未显示的项，直接选择下一项
                currentSelection++;
            }
            else if (currentPage < maxPage)
            {
                // 如果当前页面到达最后一项，则翻到下一页
                currentPage++;
                currentSelection = 0; // 下一页从第一个选项开始
            }
            else
            {
                // 如果已经是最后一项，保持在当前项
                currentSelection = Math.Min(currentSelection, totalWeapons - (currentPage - 1) * itemsPerPage);
            }
        }
        else if (direction == -1) // 向上滚动（选择上一项）
        {
            if (currentSelection > 0)
            {
                // 如果当前页面还有未显示的项，直接选择上一项
                currentSelection--;
            }
            else if (currentPage > 1)
            {
                // 如果当前页面到达第一个项，则翻到上一页
                currentPage--;
                currentSelection = itemsPerPage - 1; // 上一页从最后一个选项开始
            }
        }

        // 更新字典中的玩家选择页码和选择项
        playerCurrentSelections[client.Slot] = (currentPage - 1) * itemsPerPage + currentSelection;

        // 计算当前页需要显示的武器项
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Math.Min(startIndex + itemsPerPage, totalWeapons);

        // 更新菜单文字
        WordText = $"[华仔] 武器选择菜单:\n按W/S向上下选择(第{currentPage}/{maxPage}页)\n";
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i == startIndex + currentSelection)  // 高亮当前选中的项
            {
                WordText += $"-> {weaponList[i]}\n"; // 当前选中项
            }
            else
            {
                WordText += $"{weaponList[i]}\n"; // 其他项
            }
        }

        WordText += "按E确认|按SHIFT关闭菜单";

        PlayerMenuEntities[client.Slot].AcceptInput("SetMessage", PlayerMenuEntities[client.Slot], PlayerMenuEntities[client.Slot], $"{WordText}");

        client.ExecuteClientCommand("play Ui/buttonrollover.vsnd_c");
    }


    public void buymenu(CCSPlayerController? client, CommandInfo info)
    {
        if (client == null) return;

        if (!client.PawnIsAlive || client.TeamNum != 3) return;

        if (!CheckMenusAndHandle(client)) return;

        // 每页显示的武器项数
        const int itemsPerPage = 5;

        // 获取玩家当前的页码，确保每个玩家的翻页状态是独立的
        if (!playerCurrentSelections.ContainsKey(client.Slot))
        {
            playerCurrentSelections[client.Slot] = 0; // 默认选择第一页的第一个选项
        }

        // 获取武器列表和总数
        List<string> weaponList = WeaponCommands.Keys.ToList();
        int totalWeapons = weaponList.Count;

        // 计算当前页码和当前选项
        int currentPage = playerCurrentSelections[client.Slot] / itemsPerPage + 1;
        int currentSelection = playerCurrentSelections[client.Slot] % itemsPerPage;

        // 计算最大页码
        int maxPage = (int)Math.Ceiling((double)totalWeapons / itemsPerPage);

        // 计算当前页要显示的武器项
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Math.Min(startIndex + itemsPerPage, totalWeapons);

        // 更新菜单文本
        WordText = $"[华仔] 武器选择菜单:\n按W/S向上下选择(第{currentPage}/{maxPage}页)\n";
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i == startIndex + currentSelection)  // 高亮当前选中的项
            {
                WordText += $"-> {weaponList[i]}\n"; // 当前选中项
            }
            else
            {
                WordText += $"{weaponList[i]}\n"; // 其他项
            }
        }

        WordText += "按E确认|按SHIFT关闭菜单";

        // 检查菜单是否已经打开，若没有则创建菜单实体
        var clientpawn = client.PlayerPawn.Value;
        if (clientpawn != null)
        {
            var handle = WaeponWasdMenu.GetOrCreateViewModels(clientpawn);
            if (!PlayerMenuEntities.ContainsKey(client.Slot) || PlayerMenuEntities[client.Slot] == null)
            {
                PlayerMenuEntities[client.Slot] = WaeponWasdMenu.CreateText(client, handle, WordText, 4.0f, 1.0f, 32, "Arial Bold", Color.DarkOrange);
                MenuOpen[client.Slot] = true;
                client.PrintToChat("[华仔] 武器选择菜单已开启");
            }
            else
            {
                CloseMenu(client);
            }
        }
    }


/*
    readonly HashSet<string> PrimaryWeaponsList = new HashSet<string>
    {
        "weapon_mag7", "weapon_nova", "weapon_sawedoff", "weapon_xm1014",
        "weapon_m249", "weapon_negev", "weapon_mac10", "weapon_mp5sd",
        "weapon_mp7", "weapon_mp9", "weapon_p90", "weapon_bizon",
        "weapon_ump45", "weapon_ak47", "weapon_aug", "weapon_famas",
        "weapon_galilar", "weapon_m4a1_silencer", "weapon_m4a1", "weapon_sg556",
        "weapon_awp", "weapon_g3sg1", "weapon_scar20", "weapon_ssg08"
    };

    readonly HashSet<string> SecondaryWeaponsList = new HashSet<string>
    {
        "weapon_hkp2000", "weapon_cz75a", "weapon_deagle", "weapon_elite",
        "weapon_fiveseven", "weapon_glock", "weapon_p250",
        "weapon_revolver", "weapon_tec9", "weapon_usp_silencer"
    };
    */
    public void LoadWeaponRestrictions()
    {
        if (!File.Exists(WeaponRestrict))
        {
            // 如果文件不存在，写入默认值
            File.WriteAllLines(WeaponRestrict, new[] { "weapon_usp_silencer 100", "weapon_glock 100" });
        }

        string[] lines = File.ReadAllLines(WeaponRestrict);
        foreach (var line in lines)
        {
            var parts = line.Split(' ');

            // 确保行有两个部分：武器名称和价格
            if (parts.Length == 2)
            {
                string weaponName = parts[0];
                int price = 0;

                // 尝试解析价格
                if (int.TryParse(parts[1], out price))
                {
                    // 如果价格小于等于0，则表示禁止购买
                    if (price <= 0)
                    {
                        restrictedWeaponsWithPrice[weaponName] = -1;  // -1 表示禁止购买
                    }
                    else
                    {
                        restrictedWeaponsWithPrice[weaponName] = price;
                    }
                }
                else
                {
                    // 如果价格无法解析，默认禁止购买
                    restrictedWeaponsWithPrice[weaponName] = -1;
                }
            }
            else
            {
                // 如果文件中格式错误，禁止该武器购买
                Console.WriteLine($"Warning: Invalid line format, ignoring: {line}");
            }
        }
    }


    public void BUYAK47(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_ak47"))
        {
            int price = restrictedWeaponsWithPrice["weapon_ak47"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}AK47已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                // 执行购买
                //AddTimer(0.0f, () => { client.ExecuteClientCommand("slot1;"); });
                //AddTimer(0.2f, () => { client.DropActiveWeapon(); });
                //AddTimer(0.3f, () =>
                //{
                DropWeaponSlot1(client);
                //giveItem.PlayerGiveNamedItem(client, "weapon_ak47");
                giveItem.PlayerGiveNamedItem(client, "weapon_ak47");


                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");  
                    //client.GiveNamedItem2("weapon_ak47");
               // });
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买AK47{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 2700; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                // 执行购买
                //AddTimer(0.0f, () => { client.ExecuteClientCommand("slot1;"); });
                //AddTimer(0.2f, () => { client.DropActiveWeapon(); });
                //AddTimer(0.3f, () =>
                //{
                    DropWeaponSlot1(client);
                    //giveItem.PlayerGiveNamedItem(client, "weapon_ak47");
                giveItem.PlayerGiveNamedItem(client, "weapon_ak47");


                client.InGameMoneyServices.Account = money - defaultPrice;
                    Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    //client.GiveNamedItem2("weapon_ak47");
                //});
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买AK47{ChatColors.Default}.!!");
            }
        }
    }



    private void BUYGALIL(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_galilar"))
        {
            int price = restrictedWeaponsWithPrice["weapon_galilar"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}加利尔已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;

            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                //giveItem.PlayerGiveNamedItem(client, "weapon_galilar");
                giveItem.PlayerGiveNamedItem(client, "weapon_galilar");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买加利尔{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1800; // 默认价格
            var money = client.InGameMoneyServices.Account;

            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                //giveItem.PlayerGiveNamedItem(client, "weapon_galilar");
                giveItem.PlayerGiveNamedItem(client, "weapon_galilar");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                
              
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买加利尔{ChatColors.Default}.!!");
            }
        }
    }


    

    private void BUYCZ75(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_cz75a"))
        {
            int price = restrictedWeaponsWithPrice["weapon_cz75a"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}CZ75已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;

            if (money >= price)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_cz75a");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买CZ75{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 500; // 默认价格
            var money = client.InGameMoneyServices.Account;

            if (money >= defaultPrice)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_cz75a");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买CZ75{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYTEC9(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_tec9"))
        {
            int price = restrictedWeaponsWithPrice["weapon_tec9"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}TEC9已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;

            if (money >= price)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_tec9");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买TEC9{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 500; // 默认价格
            var money = client.InGameMoneyServices.Account;

            if (money >= defaultPrice)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_tec9");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买TEC9{ChatColors.Default}.!!");
            }
        }
    }


    private void BUYR8(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_revolver"))
        {
            int price = restrictedWeaponsWithPrice["weapon_revolver"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}左轮已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_revolver");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买R8左轮{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 600; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot2(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_revolver");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买R8左轮{ChatColors.Default}.!!");
            }
        }
    }


    private void BUYM3(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_sawedoff"))
        {
            int price = restrictedWeaponsWithPrice["weapon_sawedoff"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}锯短已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_sawedoff");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                   
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买锯短{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1100; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_sawedoff");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买锯短{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYUMP(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_ump45"))
        {
            int price = restrictedWeaponsWithPrice["weapon_ump45"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}车王ump45已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_ump45");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                   
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买车王ump45{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1200; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                

                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_ump45");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买锯短{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYSG556(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_sg556"))
        {
            int price = restrictedWeaponsWithPrice["weapon_sg556"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}sg556已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_sg556");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买sg556{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 3000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_sg556");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买sg556{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYG3SG1(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_g3sg1"))
        {
            int price = restrictedWeaponsWithPrice["weapon_g3sg1"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}g3sg1已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
            
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_g3sg1");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
            }   
                
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买g3sg1{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 5000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_g3sg1");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买g3sg1{ChatColors.Default}.!!");
            }
        }
    }



    

    private void BUYM4A4(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_m4a1"))
        {
            int price = restrictedWeaponsWithPrice["weapon_m4a1"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}m4a4已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
            
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m4a1");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买m4a4{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 3100; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m4a1");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买m4a4{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYM4A1(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_m4a1_silencer"))
        {
            int price = restrictedWeaponsWithPrice["weapon_m4a1_silencer"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}m4a1已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m4a1_silencer");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买m4a1{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 2900; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m4a1_silencer");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买m4a1{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYM249(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_m249"))
        {
            int price = restrictedWeaponsWithPrice["weapon_m249"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}m249已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m249");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买m249{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 5200; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_m249");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买m249{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYNEGEV(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_negev"))
        {
            int price = restrictedWeaponsWithPrice["weapon_negev"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}内格夫已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_negev");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买内格夫{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1700; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_negev");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买内格夫{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYBIZON(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_bizon"))
        {
            int price = restrictedWeaponsWithPrice["weapon_bizon"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}野牛已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_bizon");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买野牛{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1400; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_bizon");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
              
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买野牛{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYP90(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_p90"))
        {
            int price = restrictedWeaponsWithPrice["weapon_p90"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}p90已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_p90");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买p90{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 2350; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_p90");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买p90{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYAWP(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_awp"))
        {
            int price = restrictedWeaponsWithPrice["weapon_awp"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}大狙已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_awp");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买大狙{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 4750; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_awp");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买大狙{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYSCAR(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_scar20"))
        {
            int price = restrictedWeaponsWithPrice["weapon_scar20"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}Scar已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_scar20");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买Scar{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 5000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_scar20");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买Scar{ChatColors.Default}.!!");
            }
        }
    }
    private void BUYhe(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_hegrenade"))
        {
            int price = restrictedWeaponsWithPrice["weapon_hegrenade"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}破片手雷已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_hegrenade");
                
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买破片手雷{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_hegrenade");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买破片手雷{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYfire(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_incgrenade"))
        {
            int price = restrictedWeaponsWithPrice["weapon_incgrenade"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}燃烧弹已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_incgrenade");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买燃烧弹{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_incgrenade");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买燃烧弹{ChatColors.Default}.!!");
            }
        }
    }

    private void BUYMolotov(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_molotov"))
        {
            int price = restrictedWeaponsWithPrice["weapon_molotov"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}燃烧瓶已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_molotov");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买燃烧瓶{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_molotov");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买燃烧瓶{ChatColors.Default}.!!");
            }
        }
    }

    private void healthshot(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买道具.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买道具{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_healthshot"))
        {
            int price = restrictedWeaponsWithPrice["weapon_healthshot"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}治疗针已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_healthshot");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买治疗针{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 5000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                giveItem.PlayerGiveNamedItem(client, "weapon_healthshot");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买治疗针{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYMP7(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_mp7"))
        {
            int price = restrictedWeaponsWithPrice["weapon_mp7"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}mp7已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mp7");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买mp7{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1500; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mp7");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买mp7{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYSCOUT(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_ssg08"))
        {
            int price = restrictedWeaponsWithPrice["weapon_ssg08"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}轻狙已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_ssg08");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                  
              
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买轻狙{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1700; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_ssg08");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                   
                  
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买轻狙{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYMP5(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_mp5sd"))
        {
            int price = restrictedWeaponsWithPrice["weapon_mp5sd"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}mp5sd已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mp5sd");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买mp5sd{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1700; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mp5sd");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买mp5sd{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYXM1014(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_xm1014"))
        {
            int price = restrictedWeaponsWithPrice["weapon_xm1014"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}xm1014已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_xm1014");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买xm1014{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 2000; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_xm1014");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买xm1014{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYMAG7(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_mag7"))
        {
            int price = restrictedWeaponsWithPrice["weapon_mag7"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}mag7已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mag7");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买mag7{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1300; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_mag7");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买mag7{ChatColors.Default}.!!");
            }
        }
    }

    public void BUYNOVA(CCSPlayerController? client, CommandInfo info)
    {
        // 加载限制列表缓存
        if (restrictedWeaponsWithPrice.Count == 0)
        {
            LoadWeaponRestrictions(); // 确保缓存已加载
        }

        // 检查玩家是否存活
        if (!client.PawnIsAlive)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}你已经死亡{ChatColors.Default}无法购买武器.!!");
            return;
        }

        // 检查玩家是否在丧尸团队
        if (client.TeamNum != 3)
        {
            client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}丧尸无法购买武器{ChatColors.Default}.!!");
            return;
        }

        // 检查武器是否被禁止购买
        if (restrictedWeaponsWithPrice.ContainsKey("weapon_nova"))
        {
            int price = restrictedWeaponsWithPrice["weapon_nova"];

            // 如果价格为 -1，表示禁止购买
            if (price == -1)
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Red}nova已被禁止购买!!{ChatColors.Default}.!!");
                return;
            }

            // 如果价格大于 0，继续执行购买逻辑
            var money = client.InGameMoneyServices.Account;
            
            // 确保购买时使用的是实际的限制价格
            if (money >= price)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_nova");
                client.InGameMoneyServices.Account = money - price;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
               
                
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{price}才能购买nova{ChatColors.Default}.!!");
            }
        }
        else
        {
            // 如果武器不在限制列表中，使用默认价格
            int defaultPrice = 1300; // 默认价格
            var money = client.InGameMoneyServices.Account;
            
            if (money >= defaultPrice)
            {
                
                DropWeaponSlot1(client);
                giveItem.PlayerGiveNamedItem(client, "weapon_nova");
                client.InGameMoneyServices.Account = money - defaultPrice;
                Utilities.SetStateChanged(client, "CCSPlayerController", "m_pInGameMoneyServices");
                    
                    
               
            }
            else
            {
                client.PrintToChat($" {ChatColors.Green}[华仔]{ChatColors.Default} 你的金钱不够{ChatColors.Red}需要{defaultPrice}才能购买nova{ChatColors.Default}.!!");
            }
        }
    }

    public bool CheckMenusAndHandle(CCSPlayerController client)
    {
        // 获取所有 "point_worldtext" 实体
        var entities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("point_worldtext");

        foreach (var entity in entities)
        {
            if (entity != null)
            {
                // 如果该实体属于玩家
                if (entity.OwnerEntity.Raw == client.Pawn.Raw)
                {
                    // 如果实体的名字不是 "modelsmenu" 且当前菜单不是武器菜单
                    if (entity.Entity!.Name != "weaponsmenu")
                    {
                        // 提示玩家当前有其他菜单打开
                        client.PrintToChat("你现在正在打开别的菜单，请先关闭后再打开武器购买菜单。");
                        return false;  // 返回 false 表示菜单不能正常打开
                    }
                }
            }
        }
        // 如果没有检测到其他菜单，正常打开模型菜单
        return true;  // 返回 true 表示可以正常打开模型菜单
    }

    private void CloseMenu(CCSPlayerController client)
    {
        // 关闭菜单的逻辑，可能是清除屏幕显示
        Remove(client);
        client.PrintToChat("[华仔]武器购买菜单已关闭");
        client.ExecuteClientCommand("play Ui/buttonclick.vsnd_c");
        MenuOpen[client.Slot] = false;
    }

    public void Remove(CCSPlayerController client)
    {
        if (PlayerMenuEntities.ContainsKey(client.Slot) && PlayerMenuEntities[client.Slot]?.IsValid == true)
        {
            PlayerMenuEntities[client.Slot].Remove();
        }

        // 确保字典中该玩家的菜单实体被清除
        PlayerMenuEntities[client.Slot] = null;
    }

    

    private void DropWeaponSlot1(CCSPlayerController client)
    {
        if(client?.IsValid == true && client.PlayerPawn?.IsValid == true && !client.IsBot && !client.IsHLTV && client.PawnIsAlive && client.PlayerPawn.Value!.WeaponServices != null)
        {
            foreach(var weapon in client.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if(weapon.IsValid && weapon.Value != null)
                {
                    CCSWeaponBase ccSWeaponBase = weapon.Value.As<CCSWeaponBase>();
                    if(ccSWeaponBase != null && ccSWeaponBase.IsValid) 
                    {
                        CCSWeaponBaseVData? weaponData = ccSWeaponBase.VData;
                        if(weaponData == null ||(weaponData.GearSlot != gear_slot_t.GEAR_SLOT_PISTOL && weaponData.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE))
                        {
                            client.PlayerPawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                            client.DropActiveWeapon();
                            Server.NextFrame(() =>
                            {
                                if (ccSWeaponBase != null && ccSWeaponBase.IsValid)
                                {
                                    ccSWeaponBase.AcceptInput("Kill");
                                }
                            });
                        }

                    }
                }
            }
        }

    }

    private void DropWeaponSlot2(CCSPlayerController client)
    {
        if(client?.IsValid == true && client.PlayerPawn?.IsValid == true && !client.IsBot && !client.IsHLTV && client.PawnIsAlive && client.PlayerPawn.Value!.WeaponServices != null)
        {
            foreach(var weapon in client.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if(weapon.IsValid && weapon.Value != null)
                {
                    CCSWeaponBase ccSWeaponBase = weapon.Value.As<CCSWeaponBase>();
                    if(ccSWeaponBase != null && ccSWeaponBase.IsValid) 
                    {
                        CCSWeaponBaseVData? weaponData = ccSWeaponBase.VData;
                        if(weaponData == null ||(weaponData.GearSlot == gear_slot_t.GEAR_SLOT_PISTOL && weaponData.GearSlot != gear_slot_t.GEAR_SLOT_RIFLE))
                        {
                            client.PlayerPawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                            client.DropActiveWeapon();
                            Server.NextFrame(() =>
                            {
                                if (ccSWeaponBase != null && ccSWeaponBase.IsValid)
                                {
                                    ccSWeaponBase.AcceptInput("Kill");
                                }
                            });
                        }

                    }
                }
            }
        }

    }

 

}















