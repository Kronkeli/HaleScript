using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SFDGameScriptInterface;
using System.Numerics;
using System.Drawing;

// All available system namespaces in the ScriptAPI (as of Alpha 1.0.0).


namespace ConsoleApplication1
{

    class GameScript : GameScriptInterface
    {
        /// <summary>
        /// Placeholder constructor that's not to be included in the ScriptWindow!
        /// </summary>
        public GameScript() : base(null) { }

        /* SCRIPT STARTS HERE - COPY BELOW INTO THE SCRIPT WINDOW */
        //hahaYES
        public IPlayer HALE;
        public string[] HALENAMES;
        private int HALETYPE;
        public int last_hale_hp;
        public Random m_rnd = null;
        public float lastTeleported;
        public float tpCooldown;
        public bool tpEnabled;

        public IObjectTimerTrigger HaleSniperTimer;
        public IObjectTimerTrigger HaleMovementStopper;
        public IObjectTimerTrigger HaleStartCooldown;

        // Run code before triggers marked with "Activate on startup" but after players spawn from SpawnMarkers.
        public void OnStartup()
        {
            m_rnd = new Random((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000);

            // By default HALE cannot tp
            tpEnabled = false;
            HALENAMES = new string[4];
            HALENAMES[0] = "Saxton Fale";
            HALENAMES[1] = "Sin Feaster";
            HALENAMES[2] = "Speedy Fale";
            HALENAMES[3] = "Snipur Faggot";
            // { "Saxton Fale", "Sin Feaster", "Snipur Faggot"};

            // Every 200ms, delete all items from HALE
            IObjectTimerTrigger timer1 = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            timer1.SetRepeatCount(0);
            timer1.SetIntervalTime(200);
            timer1.SetScriptMethod("RemoveHaleItems");
            //timer1.Trigger();

            IObjectTimerTrigger timer2 = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            timer2.SetRepeatCount(0);
            timer2.SetIntervalTime(1000);
            timer2.SetScriptMethod("DisplayHaleStatus");
            //timer2.Trigger();

            // Player key input ( for hale teleport)
            Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);

            // Set lastTeleported to game start
            lastTeleported = Game.TotalElapsedGameTime;

            // Trigger for HALE action cooldown. 
            HaleMovementStopper = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            HaleMovementStopper.SetRepeatCount(1);
            HaleMovementStopper.SetIntervalTime(500);
            HaleMovementStopper.SetScriptMethod("ToggleHaleMovement");
            // HaleMovementStopper.Trigger();

            // Trigger for HALE beginning cooldown. In the beginning set to stop HALE for 5s
            HaleStartCooldown = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            HaleStartCooldown.SetRepeatCount(1);
            HaleStartCooldown.SetIntervalTime(5000);
            HaleStartCooldown.SetScriptMethod("ToggleHaleMovement");

            // Trigger for snipur HALE to give him snipers every 20s.
            HaleSniperTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            HaleSniperTimer.SetRepeatCount(0);
            HaleSniperTimer.SetIntervalTime(20000);
            HaleSniperTimer.SetScriptMethod("GiveSniperSnipur");

            // At the beginning of the game set next HALE
            //SetHale();
            //HelloHale();
            ModGibZones();
        }

        private void SetHale()
        {
            IPlayer[] players = Game.GetPlayers();

            foreach (IPlayer plr in players)
            {
                plr.SetTeam(PlayerTeam.Team1);
                PlayerModifiers plrmodifier = plr.GetModifiers();
                plrmodifier.MaxHealth = -2;
                plrmodifier.MaxEnergy = -2;
                plrmodifier.CurrentHealth = -2;
                plrmodifier.EnergyRechargeModifier = -2;
                plrmodifier.SizeModifier = -2;
                plrmodifier.SprintSpeedModifier = -2;
                plrmodifier.RunSpeedModifier = -2;
                plrmodifier.MeleeForceModifier = -2;
                plrmodifier.MeleeDamageDealtModifier = -2;
                plrmodifier.MeleeDamageTakenModifier = -2;
                plr.SetModifiers(plrmodifier);
            }

            // Choosing the next Hale and type of HALE
            if (!Game.LocalStorage.ContainsKey("halecandidates"))
            {
                Game.RunCommand("/MSG " + "Alustetaan halejonotuslista nyk. pelaajilla. (Ei toteutettu pidemmälle vielä)");
                SetHaleCandidates();
            }
            string[] haleCandidates = (string[])Game.LocalStorage.GetItem("halecandidates");
            // Print halecandidates list from local storage
            Game.RunCommand("/MSG " + "Halekandidaatit:");
            foreach (string name in haleCandidates )
            {
                Game.RunCommand("/MSG " + "- " + name);
            }

            string next_hale_name = haleCandidates[ (m_rnd.Next(((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000)) + (int)DateTime.Now.Millisecond) % haleCandidates.Length];
            IPlayer next_hale = players[1];
            int next_type = (m_rnd.Next(0, ((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000)) + (int)DateTime.Now.Millisecond) % (HALENAMES.Length-1);
            int random_index = 0;

            // Check if storage contains last_hale. If it does make sure that same person isn't hale again.
            bool chooseAgain = false;
            if ( !Game.LocalStorage.ContainsKey("last_hale") )
            {
                Game.RunCommand("/MSG " + "Jonotuslista resettiin.");
                Game.LocalStorage.SetItem("last_hale", next_hale_name);
            }
            else
            {
                do
                {
                    chooseAgain = false;
                    next_hale_name = haleCandidates[(m_rnd.Next(((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000) + random_index ) + (int)DateTime.Now.Millisecond) % haleCandidates.Length];
                    if ((string)Game.LocalStorage.GetItem("last_hale") == next_hale_name)
                    {
                        Game.RunCommand("/MSG " + "Sama hale kuin viimeksi --> vaihtoon");
                        chooseAgain = true;
                    }
                    foreach (IPlayer plr in players)
                    {
                        if (plr.Name == next_hale_name )
                        {
                            next_hale = plr;
                        }
                    }
                    random_index++;
                } while (chooseAgain);
            }
            // Delete new hale from halecandidates and update 'last_hale' in localstorage
            haleCandidates = haleCandidates.Where((source, index) => source != next_hale_name).ToArray();

            // Save changes to LocalStorage
            Game.LocalStorage.SetItem("halecandidates", haleCandidates);
            Game.LocalStorage.SetItem("last_hale", next_hale_name);

            HALE = next_hale;
            HALETYPE = next_type;
            
            // HALETYPE = 1;
            HALE.SetTeam(PlayerTeam.Team2);

            // Calculating hale HP based on playeramount and getting the modifier for HALE to apply changes
            
            PlayerModifiers modify = HALE.GetModifiers();
            int haleHP;
            int hpConstant = 150;
            if (players.Length <= 4)
            {
                haleHP = (players.Length - 1) * hpConstant;
            }
            else
            {
                haleHP = 4 * hpConstant + (players.Length - 4) * hpConstant / 2;
            }

            Game.RunCommand("/MSG " + " - - - Minä olen hirmuinen " + HALENAMES[HALETYPE] + " - - - ");

            // Setting HALE modifiers
            switch (HALETYPE)
            {
                // Saxton Fale;
                case 0:
                    SetHaleClothing(ref HALE);
                    SetHaleModifiers(ref modify, haleHP, 1.5f, 1.5f, 3f, 8f, 2f);
                    HALE.SetModifiers(modify);
                    break;

                // Sin Feaster
                case 1:
                    SetSinClothing(ref HALE);
                    tpEnabled = true;
                    tpCooldown = 20000;
                    SetHaleModifiers(ref modify, haleHP, 1.2f, 1.2f, 2f, 5f, 1.5f);
                    HALE.SetModifiers(modify);
                    break;

                // Speedy Fale
                case 2:
                    SetSpeedClothing(ref HALE);
                    SetHaleModifiers(ref modify, haleHP, 2f, 2f, 6f, 0.001f, 1f);
                    HALE.SetModifiers(modify);
                    break;

                // Snipur Faggot
                case 3:
                    tpEnabled = true;
                    tpCooldown = 10000;
                    HaleSniperTimer.Trigger();
                    HALE.GiveWeaponItem(WeaponItem.SNIPER);
                    HALE.GiveWeaponItem(WeaponItem.FIREAMMO);
                    SetHaleModifiers(ref modify, haleHP, 1f, 1f, 1f, 1f, 2f);
                    HALE.SetModifiers(modify);
                    break;
            }

            // Set beginning movement cooldown for all HALES.
            HALE.SetInputEnabled(false);
            HaleStartCooldown.Trigger();
        }

        public void SetHaleModifiers(ref PlayerModifiers modify, int HP, float sprintSpeed, float runSpeed, float meleeForce, float meleeDamageDealt, float meleeDamageTaken)
        {
            // First the general modifiers for every HALE
            modify.MaxHealth = HP;
            modify.CurrentHealth = HP;
            modify.MaxEnergy = 9999;
            modify.CurrentEnergy = 9999;
            modify.EnergyRechargeModifier = 100;
            modify.SizeModifier = 2f;

            // Then the HALE-specific modifiers
            modify.SprintSpeedModifier = sprintSpeed;
            modify.RunSpeedModifier = runSpeed;
            modify.MeleeForceModifier = meleeForce;
            modify.MeleeDamageDealtModifier = meleeDamageDealt;
            modify.MeleeDamageTakenModifier = meleeDamageTaken;
        }

        public void SetHaleClothing(ref IPlayer halePlayer)
        {
            IProfile haleProfile = halePlayer.GetProfile();
            haleProfile.Accesory = new IProfileClothingItem("Moustache", "ClothingBrown");
            haleProfile.Head = new IProfileClothingItem("CowboyHat", "ClothingDarkBrown", "ClothingLightBrown");
            haleProfile.ChestOver = null;
            haleProfile.ChestUnder = null;
            haleProfile.Hands = null;
            haleProfile.Legs = null;
            haleProfile.Feet = new IProfileClothingItem("Boots", "ClothingBrown");
            haleProfile.Skin = new IProfileClothingItem("Normal", "Skin3");
            halePlayer.SetProfile(haleProfile);
            // Changing the skin color to light?
        }
        
        public void SetSinClothing(ref IPlayer halePlayer)
        {
            IProfile haleProfile = halePlayer.GetProfile();
            haleProfile.Accesory = null;
            haleProfile.Head = null;
            haleProfile.ChestOver = null;
            haleProfile.ChestUnder = null;
            haleProfile.Hands = null;
            haleProfile.Legs = null;
            haleProfile.Feet = null;
            haleProfile.Skin = new IProfileClothingItem("Zombie", "Skin1");
            halePlayer.SetProfile(haleProfile);
            // Changing the skin color to light?
        }

        public void SetSpeedClothing(ref IPlayer halePlayer)
        {
            IProfile haleProfile = halePlayer.GetProfile();
            haleProfile.Accesory = null;
            haleProfile.Head = new IProfileClothingItem("WoolCap", "ClothingRed"); ;
            haleProfile.ChestOver = null;
            haleProfile.ChestUnder = new IProfileClothingItem("Tshirt", "ClothingBlack");
            haleProfile.Hands = null;
            haleProfile.Legs = new IProfileClothingItem("Shorts", "ClothingRed"); ;
            haleProfile.Feet = new IProfileClothingItem("Shoes", "ClothingBlack"); ;
            haleProfile.Skin = new IProfileClothingItem("Normal", "Skin3");
            halePlayer.SetProfile(haleProfile);
            // Changing the skin color to light?
        }

        public void RemoveHaleItems(TriggerArgs args)
        {
            HALE.RemoveWeaponItemType(WeaponItemType.Melee);
            HALE.RemoveWeaponItemType(WeaponItemType.Handgun);
            if (HALENAMES[HALETYPE] != "Snipur Faggot")
            {
                HALE.RemoveWeaponItemType(WeaponItemType.Rifle);
            }
            else if (HALE.CurrentPrimaryWeapon.WeaponItem != WeaponItem.SNIPER)
            {
                HALE.RemoveWeaponItemType(WeaponItemType.Rifle);
            }
            HALE.RemoveWeaponItemType(WeaponItemType.Thrown);
            HALE.RemoveWeaponItemType(WeaponItemType.Powerup);
        }

        public void SetHaleCandidates()
        {
            IPlayer[] players = Game.GetPlayers();
            string[] candidates = new string[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                candidates[i] = players[i].Name;
            }
            Game.LocalStorage.SetItem("halecandidates", candidates);
        }

        public void DisplayHaleStatus(TriggerArgs args)
        {
            if (HALETYPE == 0 || HALETYPE == 2)
            {
                Game.ShowPopupMessage("Hale HP: " + (int)Math.Round(HALE.GetModifiers().CurrentHealth));
            }
            else
            {
                float timeLeft = tpCooldown - (Game.TotalElapsedGameTime - lastTeleported);
                if (timeLeft < 0)
                {
                    timeLeft = 0;
                }
                Game.ShowPopupMessage("Hale HP: " + (int)Math.Round(HALE.GetModifiers().CurrentHealth) + " TELEPORT COOLDOWN: " + (int)Math.Round(timeLeft / 1000) + "s");
            }
        }

        public void ToggleHaleMovement(TriggerArgs args)
        {
            if (HALE.IsInputEnabled)
            {
                HALE.SetInputEnabled(false);
            }
            else
            {
                HALE.SetInputEnabled(true);
            }
        }

        public void GiveSniperSnipur(TriggerArgs args)
        {
            HALE.GiveWeaponItem(WeaponItem.SNIPER);
            HALE.GiveWeaponItem(WeaponItem.FIREAMMO);
        }

        // Run code after triggers marked with "Activate on startup".
        public void AfterStartup()
        {
            string[] halecandidates = (string[])Game.LocalStorage.GetItem("halecandidates");
            if ( halecandidates.Length == 0)
            {
                Game.RunCommand("/MSG " + " Nollataan lista kun on vain yks nimi.");
                SetHaleCandidates();
            }
        }

        // Run code on map restart (or script disabled).
        public void OnShutdown()
        {
            IPlayer[] players = Game.GetPlayers();
            foreach (IPlayer plr in players)
            {
                plr.SetTeam(PlayerTeam.Independent);
                PlayerModifiers plrModifier = new PlayerModifiers(false);
                plr.SetModifiers(plrModifier);
            }
            // Game.LocalStorage.Clear();
        }

        void TeleportHaleToPlayers()
        {
            IPlayer[] players = Game.GetPlayers();
            IPlayer plr = players[m_rnd.Next(0, players.Length)];
            int counter = 1;
            while ((plr == HALE || plr.IsDead == true) & counter < 20)
            {
                plr = players[m_rnd.Next(0, players.Length)];
                counter++;
            }
            Game.PlaySound("ChurchBell1", plr.GetWorldPosition(), 1);
            SFDGameScriptInterface.Vector2 nextPlace = plr.GetWorldPosition();
            HALE.SetWorldPosition(nextPlace);
        }

        void TeleportHaleToSpawns()
        {
            IObject[] spawnAreas = Game.GetObjectsByName("SpawnPlayer");
            int rnd = m_rnd.Next(0, spawnAreas.Length);
            SFDGameScriptInterface.Vector2 place = spawnAreas[rnd].GetWorldPosition();
            HALE.SetWorldPosition(place);
            // HaleMovementStopper.Trigger();
        }

        public void OnPlayerKeyInput(IPlayer player, VirtualKeyInfo[] keyEvents)
        {

            for (int i = 0; i < keyEvents.Length; i++)
            {
                // Game.WriteToConsole(string.Format("Player {0} keyevent: {1}", player.UniqueID, keyEvents[i].ToString()));

                // TP HALE to some position if HALE presses DOWN + BLOCK and cooldown is gone
                if (keyEvents[i].Event == VirtualKeyEvent.Pressed && keyEvents[i].Key == VirtualKey.BLOCK && player.KeyPressed(VirtualKey.CROUCH_ROLL_DIVE))
                {
                    if (player == HALE && (Game.TotalElapsedGameTime - lastTeleported) > tpCooldown && tpEnabled == true)
                    {
                        lastTeleported = Game.TotalElapsedGameTime;
                        if (HALENAMES[HALETYPE] == "Sin Feaster")
                        {
                            TeleportHaleToPlayers();
                            HALE.SetInputEnabled(false);
                            HaleMovementStopper.Trigger();
                        }
                        else if (HALENAMES[HALETYPE] == "Snipur Faggot")
                        {
                            TeleportHaleToSpawns();
                        }
                        break;
                    }
                }
            }
        }

        void HelloHale()
        {
            String msg = "All hail HALE " + HALE.GetUser().Name + "!";
            String lines = "";
            for (int i = 0; i < msg.Length; i++)
            {
                lines = lines + "===";
            }
            Game.RunCommand("/MSG " + "====================");
            Game.RunCommand("/MSG " + msg);
            Game.RunCommand("/MSG " + "====================");
        }

        // If falling thing is HALE, then tp to safety
        public void KillZoneTrigger(TriggerArgs args)
        {
            Game.RunCommand("/MSG " + "Jotain rajalla");
            if (args.Sender == HALE)
            {
                int newHealth = (int)HALE.GetHealth() - 50;
                HALE.SetHealth(newHealth);
                IObject[] spawnAreas = Game.GetObjectsByName("SpawnPlayer");
                int rnd = m_rnd.Next(0, spawnAreas.Length);
                SFDGameScriptInterface.Vector2 place = spawnAreas[rnd].GetWorldPosition();

                if (newHealth < 0)
                {
                    Game.RunCommand("/MSG " + "Ai saatana :( t. " + HALE.GetUser().Name);
                }
                else
                {
                    Game.RunCommand("/MSG " + "EN KUOLE SAATANA t. " + HALE.GetUser().Name);
                    HALE.SetWorldPosition(place);
                }
            }
        }

        // Try to modify Gib Zones to be non-lethal for HALE.
        public void ModGibZones()
        {
            string mapName = Game.MapName;
            Game.RunCommand("/MSG " + "Mappinimi on " + mapName);
            if ( mapName == "Hazardous" )
            {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-172, -120);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(52, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Police Station" )
            {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-740,-128);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(93, 3);
                // Left
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-740,230);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 45);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
            }
            else if ( mapName == "Canals" )
            {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-212, -164);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(37, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Castle Courtyard" )
            {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(360, -300);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(32, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Rooftops" )
            {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(125, 3);
                // Left
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-500, 330);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 67);
                // Right - ei toimi?
                SFDGameScriptInterface.Vector2 position3 = new SFDGameScriptInterface.Vector2(500, 330);
                SFDGameScriptInterface.Point sizeFactor3 = new SFDGameScriptInterface.Point(5, 67);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
                SetSafeZone(position3, sizeFactor3);
            }
            else if (mapName == "Chemical Plant")
            {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-76, -104);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(13, 3);
                SetSafeZone(position, sizeFactor);
            }
            else if( mapName == "Sector 8" )
            {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(125, 3);
                // Left
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-500, 330);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 67);
                // Right
                SFDGameScriptInterface.Vector2 position3 = new SFDGameScriptInterface.Vector2(500, 330);
                SFDGameScriptInterface.Point sizeFactor3 = new SFDGameScriptInterface.Point(5, 67);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
                SetSafeZone(position3, sizeFactor3);
            }
            else if( mapName == "Rooftops II" )
            {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(125, 3);
                // Left
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-500, 330);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 67);
                // Right
                SFDGameScriptInterface.Vector2 position3 = new SFDGameScriptInterface.Vector2(500, 330);
                SFDGameScriptInterface.Point sizeFactor3 = new SFDGameScriptInterface.Point(5, 67);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
                SetSafeZone(position3, sizeFactor3);
            }
            else 
            {
                Game.RunCommand("/MSG " + "Halen turvaverkko: Off.");
            }
        }

        // Creates new TP-zone to save HALE from falling into death (with penalty of losing life)
        public void SetSafeZone(SFDGameScriptInterface.Vector2 pos, SFDGameScriptInterface.Point sizeFactor)
        {
            // Create a new trigger with the 
            IObjectAreaTrigger saveHaleZone = (IObjectAreaTrigger)Game.CreateObject("AreaTrigger", pos);
            saveHaleZone.SetOnEnterMethod("KillZoneTrigger");
            saveHaleZone.SetSizeFactor(sizeFactor);
            Game.RunCommand("/MSG " + "SIZE : " + saveHaleZone.GetSize().ToString());
            Game.RunCommand("/MSG " + "SIZEFACTOR : " + saveHaleZone.GetSizeFactor().ToString());
            Game.RunCommand("/MSG " + "BASESIZE : " + saveHaleZone.GetBaseSize().ToString());
        }

        //hahaNO
        /* SCRIPT ENDS HERE - COPY ABOVE INTO THE SCRIPT WINDOW */


    }
}
