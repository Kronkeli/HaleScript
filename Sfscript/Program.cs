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

// ****** IMPORTANT *******
// The script contains DEVMODE-variable for accessing script in test mode
// DEVMODE = true --> Development mode on
// DEVMODE = false --> Development mode off


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
        public float lastHaleHp;
        public Random m_rnd = null;

        public float lastTeleported;
        public float lastWarudod;
        public float tpCooldown;
        public float warudoCooldown;
        public bool tpEnabled;
        public bool warudoEnabled;
        public bool DEVMODE;

        public IObjectTimerTrigger RemoveHaleItemsTimer;
        public IObjectTimerTrigger DisplayHaleStatusTimer;
        public IObjectTimerTrigger HaleSniperTimer;
        public IObjectTimerTrigger HaleMovementStopper;
        public IObjectTimerTrigger PlayerMovementStopper;
        public IObjectTimerTrigger HaleStartCooldown;

        // Run code before triggers marked with "Activate on startup" but after players spawn from SpawnMarkers.
        public void OnStartup()
        {
            // Bool variable to set DEV mode (1 player in game is HALE)
            DEVMODE = false;

            m_rnd = new Random((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000);

            // By default HALE cannot tp or ZA WARUDO
            tpEnabled = false;
            warudoEnabled = false;

            // Initate all HALE types
            HALENAMES = new string[5];
            HALENAMES[0] = "Saxton Fale";
            HALENAMES[1] = "Sin Feaster";
            HALENAMES[2] = "Speedy Fale";
            HALENAMES[3] = "DIO";
            HALENAMES[4] = "Snipur Faggot";

            // Every 200ms, delete all items from HALE
            RemoveHaleItemsTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            RemoveHaleItemsTimer.SetRepeatCount(0);
            RemoveHaleItemsTimer.SetIntervalTime(200);
            RemoveHaleItemsTimer.SetScriptMethod("RemoveHaleItems");

            // Trigger for displaying HALE status for players
            DisplayHaleStatusTimer = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            DisplayHaleStatusTimer.SetRepeatCount(0);
            DisplayHaleStatusTimer.SetIntervalTime(1000);
            DisplayHaleStatusTimer.SetScriptMethod("DisplayHaleStatus");

            // Player key input ( for hale teleport)
            Events.PlayerKeyInputCallback.Start(OnPlayerKeyInput);

            // Set lastTeleported to game start
            lastTeleported = Game.TotalElapsedGameTime;
            lastWarudod = Game.TotalElapsedGameTime;

            // Trigger for HALE action cooldown. 
            HaleMovementStopper = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            HaleMovementStopper.SetRepeatCount(1);
            HaleMovementStopper.SetIntervalTime(500);
            HaleMovementStopper.SetScriptMethod("ToggleHaleMovement");

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

            // Trigger for disabling player input for 5 seconds (ZA WARUDO). 
            PlayerMovementStopper = (IObjectTimerTrigger)Game.CreateObject("TimerTrigger");
            PlayerMovementStopper.SetRepeatCount(1);
            PlayerMovementStopper.SetIntervalTime(1667);
            PlayerMovementStopper.SetScriptMethod("TogglePlayerMovement");

            // At the beginning of the game set next HALE
            SetHale();
            HelloHale();
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

            // Check if Local Storage contains needed items (halecandidates and last_hale)
            if (!Game.LocalStorage.ContainsKey("halecandidates"))
            {
                Game.RunCommand("/MSG " + "Local storage doesn't contain the 'halecandidates' key, so let's add it.");
                SetHaleCandidates();
            }
            if (!Game.LocalStorage.ContainsKey("last_hale"))
            {
                Game.RunCommand("/MSG " + "Local storage doesn't contain the 'last_hale' key, so let's add it");
                Game.LocalStorage.SetItem("last_hale", players[0].Name);
            }
            string[] haleCandidates = (string[])Game.LocalStorage.GetItem("halecandidates");

            // Synchronize haleCandidates queue to the players currently in server
            SynchronizeHaleCandidates(players, haleCandidates);

            string next_hale_name = haleCandidates[ (m_rnd.Next(((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000)) + (int)DateTime.Now.Millisecond) % haleCandidates.Length];
            IPlayer next_hale = players[0];
            int next_type = (m_rnd.Next(0, ((int)DateTime.Now.Millisecond * (int)DateTime.Now.Minute * 1000)) + (int)DateTime.Now.Millisecond) % (HALENAMES.Length-1);
            int random_index = 0;
            if ( !DEVMODE )
            {
                // Check if storage contains last_hale. If it does make sure that same person isn't hale again.
                bool chooseAgain = false;
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
                } while ( chooseAgain & random_index < 10 );
                
                // Delete new hale from halecandidates and update 'last_hale' in localstorage
                EraseFromHaleCandidates(next_hale_name);
            }

            HALE = next_hale;
            HALETYPE = next_type;
            
            // HALETYPE = 3;
            HALE.SetTeam(PlayerTeam.Team2);

            // Calculating hale HP based on playeramount and getting the modifier for HALE to apply changes
            
            PlayerModifiers modify = HALE.GetModifiers();
            int haleHP;
            int hpConstant = 150;
            if ( DEVMODE )
            {
                haleHP = 500;
            }
            else {
                if (players.Length <= 4)
                {
                    haleHP = (players.Length - 1) * hpConstant;
                }
                else
                {
                    haleHP = 4 * hpConstant + (players.Length - 4) * hpConstant / 2;
                }
            }
            
            lastHaleHp = haleHP;

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

                // DIO
                case 3:
                    warudoEnabled = true;
                    warudoCooldown = 20000;
                    SetDIOClothing(ref HALE);
                    SetHaleModifiers(ref modify, haleHP, 1.2f, 1.2f, 3f, 3f, 2f);
                    HALE.SetModifiers(modify);
                    break;

                // Snipur Faggot
                case 4:
                    tpEnabled = true;
                    tpCooldown = 10000;
                    HaleSniperTimer.Trigger();
                    HALE.GiveWeaponItem(WeaponItem.SNIPER);
                    HALE.GiveWeaponItem(WeaponItem.FIREAMMO);
                    SetHaleModifiers(ref modify, haleHP, 1f, 1f, 1f, 1f, 2f);
                    HALE.SetModifiers(modify);
                    break;
            }

            // Activate HALE triggers
            if ( DEVMODE )
            {
                Game.RunCommand("/INFINITE_AMMO 1");
                Game.RunCommand("/GIVE 0 shuriken");
            }
            else {
                HALE.SetInputEnabled(false);
                HaleStartCooldown.Trigger();
                RemoveHaleItemsTimer.Trigger();
            }
            DisplayHaleStatusTimer.Trigger();
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
            haleProfile.Skin = new IProfileClothingItem("Normal", "Skin3", "ClothingBrown");
            halePlayer.SetProfile(haleProfile);
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
        }

        public void SetSpeedClothing(ref IPlayer halePlayer)
        {
            IProfile haleProfile = halePlayer.GetProfile();
            haleProfile.Accesory = null;
            haleProfile.Head = new IProfileClothingItem("WoolCap", "ClothingRed");
            haleProfile.ChestOver = null;
            haleProfile.ChestUnder = new IProfileClothingItem("Tshirt", "ClothingBlack");
            haleProfile.Hands = null;
            haleProfile.Legs = new IProfileClothingItem("Shorts", "ClothingRed");
            haleProfile.Feet = new IProfileClothingItem("Shoes", "ClothingBlack");
            haleProfile.Skin = new IProfileClothingItem("Normal", "Skin3");
            halePlayer.SetProfile(haleProfile);
        }

        public void SetDIOClothing(ref IPlayer halePlayer)
        {
            IProfile haleProfile = halePlayer.GetProfile();
            haleProfile.Accesory = null;
            haleProfile.Head = new IProfileClothingItem("Beret", "ClothingYellow", "ClothingGreen");
            haleProfile.ChestOver = new IProfileClothingItem("BlazerwithShirt", "ClothingYellow", "ClothingBlack");
            haleProfile.ChestUnder = new IProfileClothingItem("ShirtWithTie", "ClothingYellow", "ClothingBlack");
            haleProfile.Hands = new IProfileClothingItem("FingerlessGloves(Black)", "ClothingYellow");
            haleProfile.Legs = new IProfileClothingItem("Pants", "ClothingYellow");
            haleProfile.Feet = new IProfileClothingItem("Boots(black)", "ClothingYellow");
            haleProfile.Skin = new IProfileClothingItem("Normal", "Skin3");
            halePlayer.SetProfile(haleProfile);
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

            // Check if Hale has picked HP and put penalty if has
            if ( lastHaleHp < HALE.GetHealth() )
            {
                Game.RunCommand("/MSG " + "HALE poimi HP joten HALE menetti 150hp");
                HALE.SetHealth(HALE.GetHealth() - 150 );
            }
            lastHaleHp = HALE.GetHealth();
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

        public void SynchronizeHaleCandidates(IPlayer[] players, string[] haleCandidates)
        {
            foreach (string name in haleCandidates )
            {
                // Check if player from haleCandidates is on server.
                // If player is, print the name on game.
                // If player isn't, delete him from haleCandidates list (in local storage).
                bool playerNotFound = true;
                foreach( IPlayer plr in players )
                {
                    if ( plr.Name  == name )
                    {
                        playerNotFound = false;
                    }
                }
                if ( playerNotFound )
                {
                    Game.RunCommand("/MSG " + " Delet this: " + name);
                    EraseFromHaleCandidates(name);
                }
                else {
                    Game.RunCommand("/MSG " + "- " + name);
                }
            }
        }

        public void EraseFromHaleCandidates(string nameToErase)
        {
            string[] haleCandidates = (string[])Game.LocalStorage.GetItem("halecandidates");
            
            // Delete new hale from halecandidates and update 'last_hale' in localstorage
            haleCandidates = haleCandidates.Where((source, index) => source != nameToErase).ToArray();

            // Save changes to LocalStorage
            Game.LocalStorage.SetItem("halecandidates", haleCandidates);
            Game.LocalStorage.SetItem("last_hale", nameToErase);
        }

        public void DisplayHaleStatus(TriggerArgs args)
        {
            if (HALETYPE == 0 || HALETYPE == 2)
            {
                Game.ShowPopupMessage("Hale HP: " + (int)Math.Round(HALE.GetModifiers().CurrentHealth));
            }
            else if( HALETYPE == 3 )
            {
                float timeLeft = warudoCooldown - (Game.TotalElapsedGameTime - lastWarudod);
                if ( timeLeft < 0 )
                {
                    timeLeft = 0;
                }
                Game.ShowPopupMessage("Hale HP: " + (int)Math.Round(HALE.GetModifiers().CurrentHealth) + " ZA WARUDO COOLDOWN: " + (int)Math.Round(timeLeft / 1000) + "s");
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

        public void TogglePlayerMovement(TriggerArgs args)
        {
            Game.RunCommand("/SLOMO " + "0");
            IPlayer[] players = Game.GetPlayers();
            foreach ( IPlayer plr in players )
            {
                plr.SetInputEnabled(true);
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
            ModGibZones();
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
                    else if ( player == HALE && (Game.TotalElapsedGameTime - lastWarudod) > warudoCooldown && warudoEnabled == true )
                    {
                        lastWarudod = Game.TotalElapsedGameTime;
                        IPlayer[] players = Game.GetPlayers();
                        foreach ( IPlayer plr in players )
                        {
                            if ( plr != HALE )
                            {
                                plr.SetInputEnabled(false);
                                Game.PlaySound("ChurchBell1", plr.GetWorldPosition(), 1);
                            }
                        }
                        Game.RunCommand("/SLOMO " + "1");
                        PlayerMovementStopper.Trigger();
                    }
                }
            }
        }

        void HelloHale()
        {
            String msg = "All hail HALE " + HALE.GetUser().Name + "!";
            Game.RunCommand("/MSG " + "====================");
            Game.RunCommand("/MSG " + msg);
            Game.RunCommand("/MSG " + "====================");
        }

        // If falling thing is HALE, then tp to safety
        public void KillZoneTrigger(TriggerArgs args)
        {
            if ( DEVMODE )
            {
                Game.RunCommand("/MSG " + "Jotain rajalla");
            }
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
            if ( mapName == "Hazardous" ) {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-172, -120);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(52, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Police Station" ) {
                // Game.RunCommand("/MAPROTATION " + "10");

                // for (int i = 0; i < 100; i++)
                // {
                //     Game.RunCommand("/MSG " + "nothing suspicious here...");
                // }
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-740,-128);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(93, 3);
                // Left
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-740,230);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 45);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
            }
            else if ( mapName == "Canals" ) {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-212, -164);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(37, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Castle Courtyard" ) {
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(360, -300);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(32, 2);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Rooftops" ) {
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
            else if (mapName == "Chemical Plant") {
                // Left acid container
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-76, -120);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(14, 3);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Sector 8" ) {
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
            else if ( mapName == "Rooftops II" ) {
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
            else if ( mapName == "Heavy Equipment" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(125, 3);
                // Right
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(500, 330);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(5, 67);
                SetSafeZone(position1, sizeFactor1);
                SetSafeZone(position2, sizeFactor2);
            }
            else if ( mapName == "Plant 47" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(132, -172);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(16, 3);
                SetSafeZone(position, sizeFactor);
                
            }
            else if ( mapName == "Old Warehouse" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(125, 3);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Tower" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(125, 3);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "Pistons" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position1 = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor1 = new SFDGameScriptInterface.Point(125, 3);
                SetSafeZone(position1, sizeFactor1);
                // The GIB-buckets:
                SFDGameScriptInterface.Vector2 position2 = new SFDGameScriptInterface.Vector2(-216, -40);
                SFDGameScriptInterface.Point sizeFactor2 = new SFDGameScriptInterface.Point(10, 4);
                SetSafeZone(position2, sizeFactor2);
            }
            else if ( mapName == "Facility" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-500, -200);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(125, 5);
                SetSafeZone(position, sizeFactor);
                // Maybe left??
            }
            else if ( mapName == "Steamship" ) {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-500, -180);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(125, 5);
                SetSafeZone(position, sizeFactor);
            }
            else if ( mapName == "East Warehouse" )
            {
                // Bottom
                SFDGameScriptInterface.Vector2 position = new SFDGameScriptInterface.Vector2(-500, -150);
                SFDGameScriptInterface.Point sizeFactor = new SFDGameScriptInterface.Point(32, 5);
                SetSafeZone(position, sizeFactor);
                // Left??
            }
            else
            {
                Game.RunCommand("/MSG " + "Mapissa " + mapName + ": Halen tervapeikko is off");
            }
        }

        // Creates new TP-zone to save HALE from falling into death (with penalty of losing life)
        public void SetSafeZone(SFDGameScriptInterface.Vector2 pos, SFDGameScriptInterface.Point sizeFactor)
        {
            // Create a new trigger with the 
            IObjectAreaTrigger saveHaleZone = (IObjectAreaTrigger)Game.CreateObject("AreaTrigger", pos);
            saveHaleZone.SetOnEnterMethod("KillZoneTrigger");
            saveHaleZone.SetSizeFactor(sizeFactor);
            if ( DEVMODE )
            {
                Game.RunCommand("/MSG " + "SIZE : " + saveHaleZone.GetSize().ToString());
                Game.RunCommand("/MSG " + "SIZEFACTOR : " + saveHaleZone.GetSizeFactor().ToString());
                Game.RunCommand("/MSG " + "BASESIZE : " + saveHaleZone.GetBaseSize().ToString());
            }
        }

        //hahaNO
        /* SCRIPT ENDS HERE - COPY ABOVE INTO THE SCRIPT WINDOW */


    }
}
