using Dalamud.Game.ClientState.GamePad;
using Dalamud.Interface.Components;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Gamepad;
using FFXIVClientStructs.FFXIV.Client.Game;
using PunishLib.ImGuiMethods;
using System.IO;
using System.Windows.Forms;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace Orbwalker;

internal static unsafe class UI
{
    internal static void Draw()
    {
        ImGuiEx.EzTabBar("Default",
            ("設定", Settings, null, true),
            ("職業", Jobs, null, true),
            ("額外功能", Extras, null, true),
            ("關於", () => AboutTab.Draw(P.Name), null, true),
            ("偵錯".NullWhenFalse(C.Debug), Debug, ImGuiColors.DalamudGrey3, true),
            InternalLog.ImGuiTab(C.Debug)
        );
    }

    static void Settings()
    {
        var cur = ImGui.GetCursorPos();
        if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "q.png"), out var t))
        {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 20);
            ImGui.Image(t.Handle, new Vector2(20, 20));
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (ThreadLoadImageHandler.TryGetTextureWrap(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "res", "t.png"), out var t2))
                {
                    ImGui.Image(t2.Handle, new Vector2(t2.Width, t2.Height));
                }
                ImGui.EndTooltip();
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right))
                {
                    C.Debug = !C.Debug;
                }
            }
        }
        ImGui.SetCursorPos(cur);

        if (ImGui.Checkbox("啟用 Orbwalker", ref C.Enabled))
        {
            P.Memory.EnableDisableBuffer();
        }
        ImGui.Checkbox("控制器模式", ref C.ControllerMode);
        ImGuiComponents.HelpMarker("啟用後改為設定控制器專用選項。");
        ImGui.Separator();

        #region Movement
        ImGui.TextColored(0xFFFFDCEB, "移動");
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.Text("滑步施法窗口校準");
        ImGuiComponents.HelpMarker(
            "切換自動校準滑步施法窗口，或手動設定數值。\n\n" +
            "「自動」模式較可靠，但滑步施法窗口會比較短。你也可以依目前的詠唱速度或網路延遲手動調整。"
            );
        Spacing(!C.IsSlideAuto);
        ImGuiEx.RadioButtonBool("自動", "手動", ref C.IsSlideAuto, true);
        if (!C.IsSlideAuto)
        {
            Spacing();
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderFloat("解鎖時間（秒）", ref C.Threshold, 0.1f, 1f);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGuiEx.Text("Orbwalking 模式");
        ImGuiComponents.HelpMarker(
            "切換兩種 Orbwalking 模式：\n\n" +
            "「Slidecast」是預設模式，在滑步施法窗口出現前阻止玩家移動，並在下一次施法開始時再次鎖定。大多數情況下，第一次施法前必須先停止移動。\n\n" +
            "「Slidelock」會在戰鬥中持續鎖定移動，只允許在滑步施法窗口內移動。使用此模式時，只能透過解除移動鎖定按鍵恢復移動。"
            );
        Spacing();
        if (ImGui.RadioButton("Slidecast", !C.ForceStopMoveCombat))
        {
            C.ForceStopMoveCombat = false;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Slidelock", C.ForceStopMoveCombat))
        {
            C.ForceStopMoveCombat = true;
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Checkbox("緩衝第一次施法", ref C.Buffer))
        {
            P.Memory.EnableDisableBuffer();
        }
        ImGuiComponents.HelpMarker("將第一次施法暫存至停止移動後執行，因此不必事先站定。此設定可能與 Redirect、ReAction 等插件衝突，甚至使其選項失效。");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Checkbox("封鎖所有移動", ref C.BlockAllMovement);
        ImGuiComponents.HelpMarker("按住所指定按鍵時封鎖所有移動。");
        if (C.BlockAllMovement)
        {
            if (C.ControllerMode)
                DrawKeybind("封鎖移動按鈕", ref C.BlockButton);
            else
                DrawKeybind("封鎖移動按鍵", ref C.BlockKey);
            ImGuiComponents.HelpMarker("綁定一個按鍵以完全封鎖角色移動。");
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (C.ControllerMode)
            DrawKeybind("解除移動鎖定按鈕", ref C.ReleaseButton);
        else
            DrawKeybind("解除移動鎖定按鍵", ref C.ReleaseKey);
        ImGuiComponents.HelpMarker("綁定按鍵以立即解除移動鎖定並取消正在進行的詠唱。預設只有按住時才能移動，建議使用滑鼠按鈕。");
        ImGui.Checkbox("永久解除", ref C.UnlockPermanently);
        ImGuiComponents.HelpMarker("解除玩家移動鎖定，主要搭配上方的解除按鍵使用。");
        ImGuiEx.Text("解除按鍵模式：");
        ImGuiComponents.HelpMarker("將解除移動鎖定按鍵由「按住」改為「切換」。");
        Spacing();
        ImGuiEx.RadioButtonBool("按住", "切換", ref C.IsHoldToRelease, true);
        if (!C.ControllerMode)
        {
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Checkbox("啟用滑鼠按鈕解除", ref C.DisableMouseDisabling);
            ImGuiComponents.HelpMarker("同時按住滑鼠左鍵與右鍵即可緊急移動。");
            ImGuiEx.TextV("移動按鍵：");
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidth(0.8f);
            if (ImGui.BeginCombo($"##movekeys", $"{C.MoveKeys.Print()}"))
            {
                foreach (var x in Svc.KeyState.GetValidVirtualKeys())
                {
                    ImGuiEx.CollectionCheckbox($"{x}", x, C.MoveKeys);
                }
                ImGui.EndCombo();
            }
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.PushItemWidth(300);
        ImGuiEx.TextV("地面目標停留時間（秒）：");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.InputFloat("###GroundedHoldConfig", ref P.Config.GroundedHold, 0, 0, "%.2g"))
        {
            if (P.Config.GroundedHold <= 0)
                P.Config.GroundedHold = 0;

            if (P.Config.GroundedHold % 0.01f != 0)
                P.Config.GroundedHold = (float)Math.Round(P.Config.GroundedHold, 2, MidpointRounding.ToNegativeInfinity);
        }
        ImGuiComponents.HelpMarker("需要詠唱且指定地面的技能（主要為青魔法）會在目標圈出現時開始記錄詠唱，確認目標時若正在移動便會取消。此選項會在目標圈出現時強制角色停止指定時間，避免過早取消詠唱。");
        ImGuiGroup.EndGroupBox();
        #endregion

        #region Overlay
        ImGui.TextColored(0xFFFFDCEB, "顯示層");
        ImGuiEx.RadioButtonBool("圖片模式", "向量模式", ref C.UseImguiOverlay, true, inverted: true);
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.Text("顯示顯示層");
        ImGuiComponents.HelpMarker("選擇啟用 Orbwalker 時顯示層的顯示時機。");
        Spacing(true); ImGui.Checkbox("戰鬥中", ref C.DisplayBattle);
        Spacing(true); ImGui.Checkbox("任務中", ref C.DisplayDuty);
        Spacing(true); ImGui.Checkbox("永遠顯示", ref C.DisplayAlways);
        Spacing(true);
        ImGui.SetNextItemWidth(100f);
        ImGui.SliderFloat("顯示層縮放", ref C.SizeMod.ValidateRange(0.5f, 40f), 0.5f, 4f);
        Spacing();
        if(C.UseImguiOverlay)
        {
            ImGui.Checkbox("垂直排列", ref C.DisplayVertical);
        }
        else
        {
            var b = false;
            ImGui.BeginDisabled();
            ImGui.Checkbox("垂直排列", ref b);
            ImGui.EndDisabled();
        }
        ImGuiComponents.HelpMarker("將按鈕改為垂直排列；僅向量模式可用。");
        ImGuiGroup.EndGroupBox();
        #endregion
    }
    static void Jobs()
    {
        ImGui.TextColored(0xFFFFDCEB, "職業");
        ImGuiComponents.HelpMarker("選擇要套用 Orbwalker 移動鎖定功能的職業。並非所有職業都有詠唱，但啟用通用技能的額外功能後也會套用於這些職業。");
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.TextV("切換：");
        ImGui.SameLine();
        if (ImGui.Button("全部"))
        {
            var jobs = Enum.GetValues<Job>().Where(x => x != Job.ADV);
            var b = jobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
            foreach (var x in jobs)
            {
                C.EnabledJobs[x] = !b;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("詠唱職業"))
        {
            var b = Data.CastingJobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
            foreach (var x in Data.CastingJobs)
            {
                C.EnabledJobs[x] = !b;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("DoH"))
        {
            var b = Data.HandJobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
            foreach (var x in Data.HandJobs)
            {
                C.EnabledJobs[x] = !b;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("DoL"))
        {
            var b = Data.LandJobs.All(x => C.EnabledJobs.TryGetValue(x, out var v) && v);
            foreach (var x in Data.LandJobs)
            {
                C.EnabledJobs[x] = !b;
            }
        }

        if(Player.Available)
        {
            ImGui.SameLine();
            var val = C.EnabledJobs.SafeSelect(Player.Job);
            if(ImGui.Checkbox($"目前職業：{Player.Job}", ref val))
            {
                C.EnabledJobs[Player.Job] = val;
            }
        }

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Columns(5, "###JobGrid", false);
        int i = 0;
        foreach (var job in Enum.GetValues<Job>().OrderBy(x => !x.IsCombat()).ThenBy(x => x.IsHealer()).ThenBy(x => x.IsTank()).ThenBy(x => x.IsMeleeDps()).ThenBy(x => x.IsMagicalRangedDps()))
        {
            i++;
            if (job == Job.ADV) continue;
            if (!P.Config.EnabledJobs.ContainsKey(job))
                P.Config.EnabledJobs[job] = false;

            bool val = P.Config.EnabledJobs[job];
            if(ThreadLoadImageHandler.TryGetIconTextureWrap(job.GetIcon(), false, out var tex))
            {
                ImGui.Image(tex.Handle, new(ImGui.GetFrameHeight()));
                ImGui.SameLine(0, 2);
            }
            if (ImGui.Checkbox($"{job}", ref val))
            {
                if (val)
                {
                    switch (job)
                    {
                        case Job.WHM:
                        case Job.CNJ:
                            P.Config.EnabledJobs[Job.WHM] = val;
                            P.Config.EnabledJobs[Job.CNJ] = val;
                            break;
                        case Job.BLM:
                        case Job.THM:
                            P.Config.EnabledJobs[Job.BLM] = val;
                            P.Config.EnabledJobs[Job.THM] = val;
                            break;
                        case Job.MNK:
                        case Job.PGL:
                            P.Config.EnabledJobs[Job.MNK] = val;
                            P.Config.EnabledJobs[Job.PGL] = val;
                            break;
                        case Job.ACN:
                        case Job.SMN:
                        case Job.SCH:
                            P.Config.EnabledJobs[Job.ACN] = val;
                            P.Config.EnabledJobs[Job.SMN] = val;
                            P.Config.EnabledJobs[Job.SCH] = val;
                            break;
                        case Job.MRD:
                        case Job.WAR:
                            P.Config.EnabledJobs[Job.MRD] = val;
                            P.Config.EnabledJobs[Job.WAR] = val;
                            break;
                        case Job.PLD:
                        case Job.GLA:
                            P.Config.EnabledJobs[Job.PLD] = val;
                            P.Config.EnabledJobs[Job.GLA] = val;
                            break;
                        case Job.ROG:
                        case Job.NIN:
                            P.Config.EnabledJobs[Job.ROG] = val;
                            P.Config.EnabledJobs[Job.NIN] = val;
                            break;
                        case Job.BRD:
                        case Job.ARC:
                            P.Config.EnabledJobs[Job.BRD] = val;
                            P.Config.EnabledJobs[Job.ARC] = val;
                            break;
                        case Job.LNC:
                        case Job.DRG:
                            P.Config.EnabledJobs[Job.LNC] = val;
                            P.Config.EnabledJobs[Job.DRG] = val;
                            break;
                        case Job.CUL:
                        case Job.ALC:
                        case Job.BSM:
                        case Job.GSM:
                        case Job.ARM:
                        case Job.LTW:
                        case Job.CRP:
                        case Job.WVR:
                            P.Config.EnabledJobs[Job.CUL] = val;
                            P.Config.EnabledJobs[Job.ALC] = val;
                            P.Config.EnabledJobs[Job.BSM] = val;
                            P.Config.EnabledJobs[Job.GSM] = val;
                            P.Config.EnabledJobs[Job.ARM] = val;
                            P.Config.EnabledJobs[Job.LTW] = val;
                            P.Config.EnabledJobs[Job.CRP] = val;
                            P.Config.EnabledJobs[Job.WVR] = val;
                            break;
                        case Job.BTN:
                        case Job.MIN:
                        case Job.FSH:
                            P.Config.EnabledJobs[Job.BTN] = val;
                            P.Config.EnabledJobs[Job.MIN] = val;
                            P.Config.EnabledJobs[Job.FSH] = val;
                            break;
                    }
                }

                P.Config.EnabledJobs[job] = val;
            }

            if(job.EqualsAny(Job.PCT, Job.VPR, Job.GNB, Job.SGE)) ImGui.NextColumn();
        }
        ImGui.Columns(1);
        ImGui.Spacing();
        ImGuiGroup.EndGroupBox();
    }
    static void Extras()
    {
        #region Job Specific Actions
        ImGui.TextColored(0xFFFFDCEB, "職業專用技能");
        ImGuiComponents.HelpMarker("這些技能雖然不是一般詠唱，但仍可能因移動而中斷或提早結束。");
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.Text("以下技能作用中時封鎖移動：");
        ImGui.Separator();
        ImGuiEx.Spacing(); ImGui.Checkbox("Passage of Arms (PLD)", ref C.PreventPassage);
        ImGuiEx.Spacing(); ImGui.Checkbox("Flamethrower (MCH)", ref C.PreventFlame);
        ImGuiEx.Spacing(); ImGui.Checkbox("Meditate (SAM)", ref C.PreventMeditate);
        ImGuiEx.Spacing(); ImGui.Checkbox("Improvisation (DNC)", ref C.PreventImprov);
        ImGuiEx.Spacing(); ImGui.Checkbox("Phantom Flurry (BLU)", ref C.PreventPhantom);
        ImGuiEx.Spacing();
        ImGuiGroup.EndGroupBox();
        #endregion

        #region Miscellaneous Casts
        ImGui.TextColored(0xFFFFDCEB, "其他詠唱");
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.Text("詠唱以下技能時封鎖移動：");
        ImGui.Separator();
        ImGuiEx.Spacing(); ImGui.Checkbox("Teleport", ref C.BlockTP);
        ImGuiEx.Spacing(); ImGui.Checkbox("Return", ref C.BlockReturn);
        ImGuiEx.Spacing();
        ImGuiGroup.EndGroupBox();
        #endregion

        #region PvP
        ImGui.TextColored(0xFFFFDCEB, $"PvP");
        ImGuiGroup.BeginGroupBox();
        ImGuiEx.Spacing(); ImGui.Checkbox("啟用 PvP Orbwalking", ref C.PVP);
        ImGuiComponents.HelpMarker("允許在紛爭前線與水晶衝突等 PvP 模式中使用。\n\n這可能被視為對其他玩家的不公平優勢，請自行承擔使用風險。");
        ImGuiEx.Spacing();
        ImGuiGroup.EndGroupBox();
        #endregion
    }
    static void Debug()
    {
        //ImGui.InputInt($"forceDisableMovementPtr", ref P.Memory.ForceDisableMovement);
        if (Svc.Targets.Target != null)
        {
            var addInfo = stackalloc uint[1];
            // was spell before, fixed
            ImGuiEx.Text($"{ActionManager.Instance()->GetActionStatus(ActionType.Action, 16541, Svc.Targets.Target.Struct()->EntityId, outOptExtraInfo: addInfo)} / {*addInfo}");
        }
        ImGuiEx.Text($"GCD: {Util.GCD}\nRCorGRD:{Util.GetRCorGDC()}");
    }

    static string KeyInputActive = null;

    static void Spacing(bool cont = false)
    {
        ImGuiEx.TextV($" {(cont ? "├" : "└")} ");
        ImGui.SameLine();
    }

    static bool DrawKeybind(string text, ref Keys key)
    {
        bool ret = false;
        ImGui.PushID(text);
        ImGuiEx.Text($"{text}:");
        ImGui.Dummy(new(20, 1));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##inputKey", $"{key}"))
        {
            if (text == KeyInputActive)
            {
                ImGuiEx.Text(ImGuiColors.DalamudYellow, "現在請按下新的按鍵……");
                foreach (var x in Enum.GetValues<Keys>())
                {
                    if (IsKeyPressed((int)x))
                    {
                        KeyInputActive = null;
                        key = x;
                        ret = true;
                        break;
                    }
                }
            }
            else
            {
                if (ImGui.Selectable("自動偵測新按鍵", false, ImGuiSelectableFlags.DontClosePopups))
                {
                    KeyInputActive = text;
                }
                ImGuiEx.Text("手動選擇按鍵：");
                ImGuiEx.SetNextItemFullWidth();
                ImGuiEx.EnumCombo("##selkeyman", ref key);
            }
            ImGui.EndCombo();
        }
        else
        {
            if (text == KeyInputActive)
            {
                KeyInputActive = null;
            }
        }
        if (key != Keys.None)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
            {
                key = Keys.None;
                ret = true;
            }
        }
        ImGui.PopID();
        return ret;
    }

    static bool DrawKeybind(string text, ref GamepadButtons key)
    {
        bool ret = false;
        ImGui.PushID(text);
        ImGuiEx.Text($"{text}:");
        ImGui.Dummy(new(20, 1));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f);
        if (ImGui.BeginCombo("##inputKey", $"{GamePad.ControllerButtons[key]}"))
        {
            if (text == KeyInputActive)
            {
                ImGuiEx.Text(ImGuiColors.DalamudYellow, "現在請按下新的按鍵……");
                foreach (var x in GamePad.ControllerButtons)
                {
                    if (GamePad.IsButtonPressed(x.Key))
                    {
                        KeyInputActive = null;
                        key = x.Key;
                        ret = true;
                        break;
                    }
                }
            }
            else
            {
                if (ImGui.Selectable("自動偵測新按鍵", false, ImGuiSelectableFlags.DontClosePopups))
                {
                    KeyInputActive = text;
                }
                ImGuiEx.Text("手動選擇按鍵：");
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.BeginCombo("##selkeyman", GamePad.ControllerButtons[key]))
                {
                    foreach (var button in GamePad.ControllerButtons)
                    {
                        if (ImGui.Selectable($"{button.Value}", button.Key == key))
                            key = button.Key;
                    }

                    ImGui.EndCombo();
                }
            }
            ImGui.EndCombo();
        }
        else
        {
            if (text == KeyInputActive)
            {
                KeyInputActive = null;
            }
        }
        if (key != GamepadButtons.None)
        {
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Trash))
            {
                key = GamepadButtons.None;
                ret = true;
            }
        }
        ImGui.PopID();
        return ret;
    }
}
