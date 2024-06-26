using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetArrowPermanent : Command
{
    Arrow arrow;
    GameManager gameManager;
    private Item item;
    private ItemManager itemManager;
    private UseItem useItem;
    public SetArrowPermanent(Arrow arrow, Item item, 
        GameManager gameManager, ItemManager itemManager){
        this.arrow = arrow;
        this.gameManager = gameManager;
        this.item = item;
        this.itemManager = itemManager;
    }

    public override void Execute(float dur, bool isRewinding = false){
        executionTime = gameManager.timeID;

        useItem = new UseItem(item, arrow.FindCenter(), itemManager, gameManager);
        useItem.Execute(dur);
        AudioManager.instance.PlaySound(AudioManager.instance.brushA);
        arrow.ChangePermanent(true);
    }

    public override bool Undo(float dur, bool isRewinding = false){
        if (useItem != null)
            useItem.Undo(dur, isRewinding);

        if(item.isPermanent && isRewinding){
            InvokeOnUndoSkipped(this);
            return true;
        }
        else if (gameManager.skippedOldCommands.Contains(this))
            gameManager.RemoveFromSkippedOldCommands(this);
        
        arrow.ChangePermanent(false);

        if(isRewinding)
            AudioManager.instance.PlaySound(AudioManager.instance.brushA, true);


        /*if (item.isPermanent && isRewinding)
            gameManager.ChangeCommand(Commands.RemoveNode);
        else
            gameManager.ChangeCommand(Commands.SetArrowPermanent);
        */
        return false;
    }
}
