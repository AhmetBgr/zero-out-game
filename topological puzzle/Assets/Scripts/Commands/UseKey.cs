using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseKey : Command
{
    public delegate void OnExecuteDelegate();
    public static event OnExecuteDelegate OnExecute;

    public delegate void OnUndoDelegate();
    public static event OnUndoDelegate OnUndo;

    //private Lock padlock;
    private ItemManager itemManager;
    private GameManager gameManager;
    private Key key;
    private Vector3 padlockPos;

    private float dur = 1;

    private List<GameObject> affectedObjects = new List<GameObject>();

    public UseKey(Key key, Vector3 padlockPos, ItemManager itemManager, GameManager gameManager, float dur = 1)
    {
        //this.padlock = padlock;
        this.key = key;
        this.padlockPos = padlockPos;
        this.itemManager = itemManager;
        this.gameManager = gameManager;
        this.dur = dur;
    }

    public override void Execute(float dur)
    {
        executionTime = gameManager.timeID;

        // = itemManager.GetLastItem().GetComponent<Key>();
        itemManager.itemContainer.RemoveItem(key, dur);
        key.PlayAnimSequence(key.GetUnlockSequence(padlockPos, dur));

        if (OnExecute != null)
        {
            OnExecute();
        }
    }

    public override bool Undo(float dur, bool skipPermanent = true)
    {
        if (key.isPermanent && skipPermanent)
        {
            InvokeOnUndoSkipped(this);
            return true;
        }
        else
        {
            if (gameManager.skippedOldCommands.Contains(this))
            {
                gameManager.RemoveFromSkippedOldCommands(this);
            }
        }

        itemManager.itemContainer.AddItem(key, -1, dur);

        if (OnUndo != null)
        {
            OnUndo();
        }
        return false;
    }
}
