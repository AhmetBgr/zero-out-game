using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeSwapper : Item
{
    public SpriteRenderer nodeSwapperSR;
    public RandomSpriteColor randomSpriteColor;

    public delegate void OnSwapperUsabilityCheckDelegate();
    public static event OnSwapperUsabilityCheckDelegate OnSwapperUsabilityCheck;

    protected override void Start()
    {
        base.Start();

        if (isPermanent)
        {
            SetPermanent();
        }

    }

    public override void CheckAndUse()
    {
        //base.CheckAndUse();
        StartCoroutine(CheckAndUseWithDelay(0.1f));    
    }

    public override IEnumerator CheckAndUseWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("should check for usability: swapper");
        if (OnSwapperUsabilityCheck != null)
        {
            
            OnSwapperUsabilityCheck();
        }
        bool isUsable = false;
        if (suitableObjCount > 0)
        {
            isUsable = true;
            Use();
        }

        InvokeOnUsabilityCheckEvent(isUsable);

        suitableObjCount = 0;
    }

    public override void Use()
    {
        Target target = new Target(Commands.SwapNodes, LayerMask.GetMask("Node"), gameManager.swapNodePalette, targetIndegree: -1);

        Target previousTarget = new Target(Commands.RemoveNode, LayerMask.GetMask("Node"), gameManager.defPalette);

        ChangeCommand changeCommand = new ChangeCommand(gameManager, null, previousTarget, target);
        changeCommand.isPermanent = isPermanent;
        changeCommand.Execute();

        //gameManager.AddToOldCommands(changeCommand);
    }

    public override void SetPermanent()
    {
        isPermanent = true;
        if (nodeSwapperSR == null)
        {
            Transform image = transform.Find("Image");
            image.TryGetComponent(out nodeSwapperSR);
        }

        if (randomSpriteColor == null)
        {
            if (!randomSpriteColor.TryGetComponent(out randomSpriteColor))
            {
                randomSpriteColor = gameObject.AddComponent<RandomSpriteColor>();
                randomSpriteColor.sr = nodeSwapperSR;
            }
        }

        randomSpriteColor.enabled = isPermanent;
    }
}