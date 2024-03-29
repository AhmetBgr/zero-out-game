using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveArrow : Command
{
    public delegate void OnExecuteDelegate();
    public static event OnExecuteDelegate OnExecute;

    public delegate void OnUndoDelegate();
    public static event OnUndoDelegate OnUndo;

    private Arrow arrow;
    private GameManager gameManager;
    //Transporter transporter;

    private List<GameObject> affectedObjects = new List<GameObject>();

    public RemoveArrow(Arrow arrow, GameManager gameManager)
    {
        this.arrow = arrow;
        this.gameManager = gameManager;
    }

    public override void Execute(float dur, bool isRewinding = false)
    {
        executionTime = gameManager.timeID;

        if (!arrow.gameObject.activeSelf) return;

        arrow.col.enabled = false;

        arrow.Remove(dur);


        /*if(arrow.TryGetComponent(out transporter)) {
            transporter.PriorityObjDisappear(dur * 0.1f);
        }*/

        if (OnExecute != null)
        {
            OnExecute();
        }
    }

    public override bool Undo(float dur, bool isRewinding = false)
    {
        if (arrow.isPermanent && isRewinding)
        {
            arrow.gameObject.SetActive(false);
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

        arrow.gameObject.SetActive(true);
        arrow.col.enabled = true;

        arrow.Add(dur);

        /*if (transporter) {
            transporter.PriorityObjAppear(dur * 0.1f);
        }*/
        
        if(OnUndo != null){
            OnUndo();
        }

        return false;
    }

}
