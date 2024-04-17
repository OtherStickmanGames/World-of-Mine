using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

public class EventsHolder
{
    public class PlayerSpawned : UnityEvent<Player> { }

    public static PlayerSpawned playerSpawnedMine = new();

    //-----------------------------------------------------------------------

    public class EnemyDestroyed : UnityEvent<Transform, int> { }

    public static EnemyDestroyed enemyDestroyed = new EnemyDestroyed();

    //-----------------------------------------------------------------------

    public class PlayerSpawnedAny : UnityEvent<Player> { }

    public static PlayerSpawnedAny onPlayerSpawnAny = new();

    //-----------------------------------------------------------------------

    public class LeftJoystickMoved : UnityEvent<Vector2> { }

    public static LeftJoystickMoved leftJoystickMoved = new LeftJoystickMoved();

    //-----------------------------------------------------------------------

    public class RightJoystickMoved : UnityEvent<Vector2> { }

    public static RightJoystickMoved rightJoystickMoved = new RightJoystickMoved();

    //-----------------------------------------------------------------------

    public class RightJoystickUp : UnityEvent { }

    public static RightJoystickUp rightJoystickUp = new RightJoystickUp();

    //-----------------------------------------------------------------------

    public class JumpClicked : UnityEvent { }

    public static JumpClicked jumpClicked = new JumpClicked();

    //-----------------------------------------------------------------------

    public class PlayerAnniged : UnityEvent<Player> { }

    public static PlayerAnniged playerAnniged = new ();

    ////-----------------------------------------------------------------------

    //public class BlockPicked : UnityEvent<BlockData> { }

    //public static BlockPicked onBlockPicked = new();

    ////-----------------------------------------------------------------------

    public class ItemPicked : UnityEvent<Player, SpawnedBlockData> { }

    public static ItemPicked onItemTaked = new();

    ////-----------------------------------------------------------------------

    //public class JetpackEquiped : UnityEvent<Player, InventoryItem> { }

    //public static JetpackEquiped onJetpackEquiped = new();

    ////-----------------------------------------------------------------------

    //public class MineTeamPicked : UnityEvent<Team> { }

    //public static MineTeamPicked mineTeamPicked = new();

    ////-----------------------------------------------------------------------

    public class ClientGameCompleted : UnityEvent { }

    public static ClientGameCompleted clientGameCompleted = new();

    ////-----------------------------------------------------------------------

    //public class ProfileSeted : UnityEvent<AIProfile, Player> { }

    //public static ProfileSeted profileSeted = new ();

    ////-----------------------------------------------------------------------

    //public class OnVictory : UnityEvent<Team> { }

    //public static OnVictory onVictory = new();

    //-----------------------------------------------------------------------

    //public class OnDefeat : UnityEvent<Team> { }

    //public static OnDefeat onDefeat = new();

    //-----------------------------------------------------------------------

    public class ClientGameCompleteDataReceived : UnityEvent { }

    public static ClientGameCompleteDataReceived clientCompleteDataReceived = new();

    ////-----------------------------------------------------------------------

    
    public class CamClicked : UnityEvent { }

    public static CamClicked onCamClicked = new();

    ////-----------------------------------------------------------------------

    //public class RuneSpawned : UnityEvent<Rune> { }

    //public static RuneSpawned runeSpawned = new();

    ////-----------------------------------------------------------------------
    
    //public class QuickInventoryItemSelect : UnityEvent<Player, InventoryItem> { }

    //public static QuickInventoryItemSelect onQuickInventoryItemSelect = new();

    ////-----------------------------------------------------------------------

    //public class QuickInventoryAddItem : UnityEvent<Player, InventoryItem> { }

    //public static QuickInventoryAddItem onQuickInventoryAddItem = new();

    ////-----------------------------------------------------------------------

    //public class QuickInventoryRemoveItem : UnityEvent<Player, InventoryItem> { }

    //public static QuickInventoryRemoveItem onQuickInventoryRemoveItem = new();

    ////-----------------------------------------------------------------------

}
