using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;

namespace Specialists
{
    public class heros : BaseScript
    {
        private int tempestFX;
        private int fireflyFX;
        public heros()
        {
            //Call("precacheitem", "iw5_mk12spr_mp");
            GSCFunctions.PreCacheShellShock("dog_bite");
            GSCFunctions.PreCacheShader("compassping_portable_radar_sweep");
            tempestFX = GSCFunctions.LoadFX("explosions/powerlines_c");
            fireflyFX = GSCFunctions.LoadFX("misc/insects_carcass_runner");
            PlayerConnected += onPlayerConnect;
        }
        public override void OnSay(Entity player, string name, string message)
        {
            if (message.StartsWith("weapon"))
            {
                initSpecialistWeapon(player, int.Parse(message.Split(' ')[1]));
            }
            if (message.StartsWith("ability"))
            {
                initSpecialistAbility(player, int.Parse(message.Split(' ')[1]));
            }
        }
        private void onPlayerConnect(Entity entity)
        {
            entity.SetField("currentSelection", 0);
            entity.SetField("selectedHero", 0);
            entity.SetField("selectedAbility", -1);
            entity.SetField("hasSelected", 0);
            entity.SetField("tempestAmmo", 0);
            entity.SetField("hiveAmmo", 0);
            entity.SetField("heroCharge", 0);
            entity.SetField("isUsingCharge", 0);

            //Create UI to select hero and ability, may change to cac item selection somehow
            startUI(entity);
            startHeroCharge(entity);

            entity.NotifyOnPlayerCommand("selectUp", "vote no");
            entity.NotifyOnPlayerCommand("selectDown", "+actionslot 5");
            entity.NotifyOnPlayerCommand("select", "+activate");
            entity.NotifyOnPlayerCommand("activatePower", "vote yes");
            entity.OnNotify("selectUp", shuffleUp);
            entity.OnNotify("selectDown", shuffleDown);
            entity.OnNotify("select", endSelection);

            entity.SpawnedPlayer += () => onPlayerSpawned(entity);

            entity.OnNotify("weapon_fired", (player, weaponName) => onWeaponFired(player, (string)weaponName));
            entity.OnNotify("missile_fire", (player, grenade, weapon) => onObjectShot(player, (Entity)grenade, (string)weapon));
            entity.OnNotify("weapon_change", (player, weapon) => onWeaponChange(player, (string)weapon));
        }
        private void onPlayerSpawned(Entity player)
        {

        }
        private void onWeaponChange(Entity player, string weapon)
        {
            switch (weapon)
            {
                case "iw5_m60jugg_mp_rof":
                    player.SetPerk("specialty_rof");
                    player.SetClientDvar("perk_weapRateMultiplier", "0.1");
                    player.Player_RecoilScaleOn(10);
                    break;
                case "iw5_pp90m1_mp_reflexsmg_hamrhybrid_camo11":
                    player.SetPerk("specialty_extendedmags");
                    player.SetClientDvar("perk_extendedMagsSMGAmmo", 154);
                    break;
            }
        }
        private void onWeaponFired(Entity player, string weapon)
        {
            switch (weapon)
            {
                case "iw5_l96a1_mp_acog_xmags_camo07":

                    break;
                case "iw5_striker_mp_eotech_xmags_camo06":

                    break;
            }
        }
        private void onObjectShot(Entity player, Entity grenade, string weapon)
        {
            switch (weapon)
            {
                case "uav_strike_marker_mp":
                    Entity trigger = GSCFunctions.Spawn("trigger_radius", grenade.Origin, 0, 32, 32);
                    trigger.SetField("owner", player);
                    trigger.LinkTo(grenade);
                    trigger.SetField("chainCount", 0);
                    OnInterval(50, () =>
                        {
                            foreach (Entity triggerer in Players)
                            {
                                bool isTouching = triggerer.IsTouching(trigger);
                                bool isEnemy = triggerer.SessionTeam == trigger.GetField<Entity>("owner").SessionTeam;
                                bool isTeamBased = GSCFunctions.GetDvar("g_gametype") != "dm";

                                if (isTouching && (isEnemy || !isTeamBased) && triggerer != trigger.GetField<Entity>("owner"))
                                {
                                    triggerer.PlayFX(tempestFX, triggerer.Origin);
                                    //Vector3 dir = Call<Vector3>("vectortoangles", trig.Origin - triggerer.As<Entity>().Origin);
                                    //triggerer.As<Entity>().Call(33340, trig, trig.GetField<Entity>("owner"), 125, 0, "MOD_EXPLOSIVE_BULLET", "uav_strike_marker_mp", player.Origin, dir, "none", 0, 0);
                                    GSCFunctions.RadiusDamage(trigger.Origin, 32, 125, 100, player);
                                    trigger.Unlink();
                                    trigger.Origin = triggerer.Origin;
                                    trigger.LinkTo(triggerer);
                                    int chainCount = trigger.GetField<int>("chainCount");
                                    chainCount++;
                                    trigger.SetField("chainCount", chainCount);
                                    AfterDelay(3000, () =>
                                        {
                                            if (chainCount == trigger.GetField<int>("chainCount"))
                                            {
                                                //trigger.SetField("doneChaining", true);
                                                trigger.ClearField("chainCount");
                                                trigger.ClearField("owner");
                                                //trigger.Notify("delete");
                                                trigger.Delete();
                                                return;
                                            }
                                        });
                                }
                                if (!Utilities.isEntDefined(trigger)) return false;
                            }
                            return true;
                        });
                    break;
                case "m320_mp":
                    Log.Write(LogLevel.All, "Running Hives");
                    Entity hive = grenade;
                    hive.SetModel("projectile_m67fraggrenade");
                    hive.SetField("owner", player);
                    hive.EnableLinkTo();
                    Entity enemyModel = GSCFunctions.Spawn("script_model", grenade.Origin);
                    enemyModel.SetModel("projectile_m67fraggrenade_bombsquad");
                    enemyModel.Hide();
                    foreach (Entity players in Players)
                        if (players.IsPlayer && players.SessionTeam != player.SessionTeam) enemyModel.ShowToPlayer(players);
                    enemyModel.LinkTo(hive);
                    hive.OnNotify("missile_stuck", (g, a) =>//Notify for impact, find a way to setup as semtex calls
                        {
                            Log.Write(LogLevel.All, "Hive stuck");
                            //Entity trig = GSCFunctions.Spawn("trigger_radius", g.Origin, 0, 128, 128);
                            Entity visual = GSCFunctions.Spawn("script_model", g.Origin);
                            visual.SetField("owner", g.GetField<Entity>("owner"));
                            visual.SetModel("projectile_m67fraggrenade_bombsquad");
                            //FX here
                            OnInterval(50, () =>
                            {
                                foreach (Entity triggerer in Players)
                                {
                                    //if (!GSCFunctions.IsDefined(trig)) return false;
                                    bool isTouching = visual.Origin.DistanceTo(triggerer.Origin) < 120;//triggerer.IsTouching(trig);
                                    bool isEnemy = triggerer.SessionTeam == visual.GetField<Entity>("owner").SessionTeam;
                                    bool isTeamBased = GSCFunctions.GetDvar("g_gametype") != "dm";

                                    if (isTouching && (isEnemy || !isTeamBased) && triggerer != visual.GetField<Entity>("owner"))
                                    {
                                        Log.Write(LogLevel.All, "Hive triggered");
                                        OnInterval(250, () =>
                                            {
                                                Vector3 dir = Vector3.RandomXY();
                                                triggerer.FinishPlayerDamage(visual, visual.GetField<Entity>("owner"), 25, 0, "MOD_SUICIDE", "trophy_mp", triggerer.Origin, dir, "none", 0);
                                                if (!triggerer.IsAlive) return false;
                                                else return true;
                                            });

                                        //trig.Notify("delete");
                                        visual.Delete();
                                        return false;
                                    }
                                    else return true;
                                }
                                return true;
                            });
                        });
                    break;
            }
        }
        private void initSpecialistWeapon(Entity player, int wep)
        {
            switch (wep)
            {
                case 0:
                    player.GiveWeapon("bomb_site_mp");
                    AfterDelay(100, () =>
                        player.SwitchToWeaponImmediate("bomb_site_mp"));
                    break;
                case 1:
                    player.GiveWeapon("iw5_l96a1_mp_acog_xmags_camo07");
                    player.SetWeaponAmmoStock("iw5_l96a1_mp_acog_xmags_camo07", 0);
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("iw5_l96a1_mp_acog_xmags_camo07"));
                    break;
                case 2:
                    player.GiveWeapon("uav_strike_marker_mp");
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("uav_strike_marker_mp"));
                    break;
                case 3:
                    player.GiveWeapon("iw5_striker_mp_eotech_xmags_camo06");
                    player.SetWeaponAmmoStock("iw5_striker_mp_eotech_xmags_camo06", 0);
                    player.SetWeaponAmmoClip("iw5_striker_mp_eotech_xmags_camo06", 6);
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("iw5_striker_mp_eotech_xmags_camo06"));
                    break;
                case 4:
                    player.GiveWeapon("iw5_44magnum_mp_xmags");
                    player.SetWeaponAmmoStock("iw5_44magnum_mp_xmags", 0);
                    player.SetWeaponAmmoClip("iw5_44magnum_mp_xmags", 5);
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("iw5_44magnum_mp_xmags"));
                    break;
                case 5:
                    player.GiveWeapon("m320_mp");
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("m320_mp"));
                    break;
                case 6:
                    player.GiveWeapon("iw5_m60jugg_mp_rof");
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("iw5_m60jugg_mp_rof"));
                    break;
                case 7:
                    player.SetPerk("specialty_extendedmelee", true, true);
                    monitorMelee(player);
                    break;
                case 8:
                    player.GiveWeapon("iw5_pp90m1_mp_reflexsmg_hamrhybrid_camo11");
                    player.SetWeaponAmmoClip("iw5_pp90m1_mp_reflexsmg_hamrhybrid_camo11", 200);
                    player.SetWeaponAmmoStock("iw5_pp90m1_mp_reflexsmg_hamrhybrid_camo11", 0);
                    AfterDelay(100, () =>
                        player.SwitchToWeapon("alt_iw5_pp90m1_mp_reflexsmg_hamrhybrid_camo11"));
                    break;
            }
            AfterDelay(200, () => runSpecialistWeaponFunctions(player, wep));
        }
        private void initSpecialistAbility(Entity player, int ability)
        {
            player.SetField("isUsingCharge", 1);
            switch (ability)
            {
                case 0:
                    player.SetMoveSpeedScale(1.5f);
                    runSpecialistTimer(player, 6.6f);
                    break;
                case 1:
                    player.VisionSetNakedForPlayer("grayscale", 1);
                    runSpecialistTimer(player, .8f);
                    break;
                case 2:
                    Vector3 glitchPos = player.GetField<Vector3>("glitchPos");
                    player.Origin = glitchPos;
                    runSpecialistTimer(player, .5f);
                    break;
                case 3:
                    player.SetField("armorActive", 500);//Set to armor value
                    runSpecialistTimer(player, 10);
                    player.SetField("isUsingCharge", 0);
                    break;
                case 4:
                    player.SetField("combatFocusActive", true);
                    runSpecialistTimer(player, 20);
                    break;
                case 5:
                    //Null since this is active on onAchieveAbility
                    break;
                case 6:
                    Entity[] clones = new Entity[3];
                    clones[0] = player.ClonePlayer(10);
                    clones[1] = player.ClonePlayer(10);
                    clones[2] = player.ClonePlayer(10);
                    player.SetField("clonesActive", clones.Length);
                    runSpecialistTimer(player, 12);
                    break;
                case 7:
                    player.Hide();
                    runSpecialistTimer(player, 6);
                    //Play some FX on player pos to make it show
                    break;
                case 8:
                    //Play burst FX
                    runSpecialistTimer(player, 0);
                    foreach (Entity players in Players)
                    {
                        bool isTeamBased = GSCFunctions.GetDvar("g_gametype") != "dm";
                        bool isEnemy = (!isTeamBased && players.SessionTeam == player.SessionTeam) || (isTeamBased && players.SessionTeam != player.SessionTeam);

                        if (players.Origin.DistanceTo(player.Origin) < 256 && players != player && isEnemy)
                            players.ShellShock("dog_bite", 5);
                    }
                    break;
            }
        }
        private void runSpecialistWeaponFunctions(Entity player, int func)
        {
            switch (func)
            {
                case 0:
                    player.SetField("specialistTime", 0);
                    doSlamAnim(player, 0);
                    updateSpecialistTimer(player);
                    Vector3 currentVel = player.GetVelocity();
                    player.SetVelocity(new Vector3(currentVel.X, currentVel.Y, 350));
                    AfterDelay(100, () =>
                    {
                        OnInterval(50, () =>
                            {
                                bool hasHitGround = player.IsOnGround();
                                if (hasHitGround)
                                {
                                    doSlamAnim(player, 1);
                                    //Play explode FX + sound
                                    player.RadiusDamage(player.Origin, 512, 300, 100, player, "MOD_EXPLOSIVE", "nuke_mp");
                                    AfterDelay(700, () =>
                                        {
                                            doSlamAnim(player, 2);
                                            player.TakeWeapon("bomb_site_mp");
                                        });
                                    return false;
                                }
                                else return true;
                            });
                    });
                    return;
                case 1:
                    //Handled in onWeaponFired
                    break;
                case 2:
                    //handled in onGrenadeFired
                    break;
                case 3:
                    //handled in onWeaponFired
                    break;
                case 4:
                    //handled in onWeaponFired
                    break;
                case 5:
                    //handled in onWeaponFired
                    break;
                case 6:
                    //handled in onWeaponFired
                    break;
                case 7:
                    //perk-based
                    break;
                case 8:
                    //handled in onWeaponFired
                    break;
            }
            //runSpecialistTimer(player);
        }
        private void runSpecialistTimer(Entity player, float time)
        {
            OnInterval(50, () =>
            {
                if (player.IsPlayer)
                {
                    if (!player.IsAlive)
                    {
                        updateSpecialistTimer(player);
                        return false;
                    }
                    else
                    {
                        int charge = player.GetField<int>("heroCharge");
                        int timePassed = GSCFunctions.GetTime();
                        if (charge == 1000 && player.GetField<int>("isUsingCharge") == 1)
                        {
                            int tick = charge--;
                            player.SetField("heroCharge", tick);
                            //try { player.SetField("heroCharge", tick); }
                            //catch { Log.Write(LogLevel.Error, "Error setting specialist timer tick in runSpecialistTimer()!"); }
                            updateSpecialistTimer(player);
                            return true;
                        }
                        else if (charge == 0) { updateSpecialistTimer(player); return false; }
                        else return true;
                    }
                }
                else return false;
            });
        }
        private void updateSpecialistTimer(Entity player)
        {
            HudElem timerRing = player.GetField<HudElem>("hud_timerRing");
            int charge = player.GetField<int>("heroCharge");
            if (charge > 999) timerRing.Color = new Vector3(.8f, .8f, 0);
        }
        private void startHeroCharge(Entity player)
        {
            OnInterval(50, () =>
                {
                    if (player.IsPlayer)
                    {
                        if (!player.IsAlive)
                        {
                            updateSpecialistTimer(player);
                            return true;
                        }
                        else
                        {
                            int charge = player.GetField<int>("heroCharge");
                            if (charge == 1000 && player.GetField<int>("isUsingCharge") == 0) 
                            {
                                int ability = player.GetField<int>("selectedAbility");
                                int hero = player.GetField<int>("selectedHero");
                                //if (ability != -1)
                                    //initSpecialistAbility(p, ability);
                                //else if (ability == -1)
                                    //initSpecialistWeapon(p, hero);
                                updateSpecialistTimer(player);
                                return true; 
                            }
                            else if (player.GetField<int>("isUsingCharge") == 0)
                            {
                                charge += 1;
                                //Log.Write(LogLevel.All, "Charge at {0}", charge);
                                player.SetField("heroCharge", charge);
                                updateSpecialistTimer(player);
                                return true;
                            }
                            else { updateSpecialistTimer(player); return true; }
                        }
                    }
                    else return false;
                });
        }

        private void monitorMelee(Entity player)
        {
            OnInterval(50, () =>
                {
                    return true;
                });
        }
        private void doSlamAnim(Entity player, int key)
        {
            //var level = Entity.GetEntity(-1);
            if (!player.HasField("slamViewHands"))
            {
                Entity hands = GSCFunctions.Spawn("script_model", player.Origin);
                hands.SetModel(player.GetViewmodel());
                //Attach spikes to hands
                player.SetField("slamViewHands", hands);
            }
            Entity viewHands = player.GetField<Entity>("slamViewHands");
            if (Utilities.isEntDefined(viewHands))
            {
                Log.Write(LogLevel.Error, "View Hand field hasn't been initialized in doSlamAnim(), aborting!");
                return;
            }
            if (key == 0)
            {
                Vector3 playerAngles = player.GetPlayerAngles();
                viewHands.Angles = playerAngles - new Vector3(0, 90, 0);
                viewHands.RotatePitch(120, .6f, .1f);
            }
            else if (key == 1)
                viewHands.RotatePitch(-120, .4f, .05f);
            else if (key > 1)
            {
                viewHands.Delete();
                //player.SetField("slamViewHands", level);
                player.ClearField("slamViewHands");
            }
            
        }

        private void shuffleUp(Entity player)
        {
            if (player.GetField<int>("hasSelected") == 1) return;
            int selection = player.GetField<int>("currentSelection");
            selection++;
            if (selection > 8) selection = 0;
            player.SetField("currentSelection", selection);
            updateUI(player);
        }
        private void shuffleDown(Entity player)
        {
            if (player.GetField<int>("hasSelected") == 1) return;
            int selection = player.GetField<int>("currentSelection");
            selection--;
            if (selection < 0) selection = 8;
            player.SetField("currentSelection", selection);
            updateUI(player);
        }
        private void endSelection(Entity player)
        {
            if (player.GetField<int>("hasSelected") == 1) return;
            int selection = player.GetField<int>("currentSelection");
            player.SetField("hasSelected", 1);
            destroyUI(player);
        }
        private void startUI(Entity player)
        {
            HudElem title = HudElem.CreateFontString(player, HudElem.Fonts.HudBig, 1);
            title.SetPoint("CENTER", "CENTER");
            title.HideWhenInMenu = true;
            title.Foreground = true;
            title.SetText("Select Specialist ([{+actionslot 5}] & [{vote no}]):\n" + getSpecialistName(player.GetField<int>("currentSelection")));
            player.SetField("nameUI", title);

            HudElem ring = HudElem.CreateIcon(player, "compassping_portable_radar_sweep", 64, 64);
            ring.SetPoint("BOTTOMRIGHT", "BOTTOMRIGHT", -50, -50);
            ring.Color = new Vector3(1, 1, 1);
            ring.Foreground = true;
            ring.Alpha = .9f;
            ring.Archived = true;
            ring.HideWhenInMenu = true;
            player.SetField("hud_timerRing", ring);
        }
        private void updateUI(Entity player)
        {
            HudElem title = player.GetField<HudElem>("nameUI");
            title.SetText("Select Specialist:\n" + getSpecialistName(player.GetField<int>("currentSelection")));
        }
        private void destroyUI(Entity player)
        {
            HudElem title = player.GetField<HudElem>("nameUI");
            title.Destroy();
        }
        private string getSpecialistWeaponIcon(int index)
        {
            switch (index)
            {
                case 0:
                    return "";
                default:
                    return "";
            }
        }
        private string getSpecialistName(int select)
        {
            switch (select)
            {
                case 0:
                    return "^2Ruin";
                case 1:
                    return "^2Outrider";
                case 2:
                    return "^2Prophet";
                case 3:
                    return "^2Battery";
                case 4:
                    return "^2Seraph";
                case 5:
                    return "^2Nomad";
                case 6:
                    return "^2Reaper";
                case 7:
                    return "^2Spectre";
                case 8:
                    return "^2Firebreak";
            }
            return "null";
        }
    }
}
