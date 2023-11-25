using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockedNode : Node
{
    //public Sprite basicNodeSprite;
    //private Sprite defSprite;
    private bool blocked = true;
    /*protected override void Awake(){
        base.Awake();
        defSprite = nodeSprite.sprite;
    }*/

    private void UpdateBLockStatus(){
        // Update locked status if node has lock
        int nodeCount = LevelManager.GetNodeCount();
        if (nodeCount <= 1)
        { // Unlock the node if only it is left 
            nodeSprite.sprite = basicSprite;
            indegree_text.gameObject.SetActive(true);
            blocked = false;
        }
        else if (gameManager.curCommand == Commands.SwapNodes)
        {
            blocked = false;
        }
        else{
            nodeSprite.sprite = defSprite;
            indegree_text.gameObject.SetActive(false);
            blocked = true; 
        }
    }

    protected override void CheckIfSuitable(LayerMask targetLM, int targetIndegree, ItemType itemType, int targetPermanent, bool levelEditorBypass){
        UpdateBLockStatus();
        bool hasRequiredItem = itemType == ItemType.None | itemController.FindItemWithType(itemType) != null ? true : false;
        bool hasEqualIndegree = targetIndegree == -1 ? true : targetIndegree == indegree;
        bool permanentCheck = targetPermanent == -1 ? true : (isPermanent && targetPermanent == 1) | (!isPermanent && targetPermanent == 0);

        if ( (!blocked && (((1<<gameObject.layer) & targetLM) != 0) && hasRequiredItem && permanentCheck) || levelEditorBypass){
            if(!levelEditorBypass){
                //StartCoroutine(Highlight(glowIntensity1, 1f));
                nodeColorController.Highlight(nodeColorController.glowIntensityMedium, 1f);
                col.enabled = true;
                //lockImage.SetActive(false);
            }
            else{
                //StartCoroutine(Highlight(glowIntensity1, 1f));
                nodeColorController.Highlight(nodeColorController.glowIntensityMedium, 1f);
                col.enabled = true;  
            }

            //lockImage = null;
        }
        else{
            // Not selectable
            nodeColorController.Highlight(nodeColorController.glowIntensityVeryLow, 1f);
            col.enabled = false;
        }   
    }
}
