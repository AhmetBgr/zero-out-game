using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SwapNodes : Command
{
    public List<Command> affectedCommands = new List<Command>();

    private GameManager gameManager;
    ItemManager itemManager;

    private List<GameObject> selectedObjects = new List<GameObject>();
    private List<GameObject> affectedObjects = new List<GameObject>();
    private Item commandOwner;
    private MultipleComparison<Component> swapNodeSearch;

    public delegate void PostExecuteDelegate();
    public static event PostExecuteDelegate PostExecute;

    public SwapNodes(GameManager gameManager, ItemManager itemManager, Item commandOwner, 
        List<GameObject> selectedObjects, MultipleComparison<Component> swapNodeSearch){
        this.commandOwner = commandOwner;
        this.gameManager = gameManager;
        this.itemManager = itemManager;
        this.selectedObjects.AddRange(selectedObjects);
        this.swapNodeSearch = swapNodeSearch;
    }

    public override void Execute(float dur, bool isRewinding = false)
    {
        executionTime = gameManager.timeID;

        // Swap postions between two nodes

        //itemManager.itemContainer.RemoveItem(commandOwner, dur);
        //Item nodeSwapper = commandOwner.GetComponent<Item>();
        //nodeSwapper.randomSpriteColor.enabled = false;
        //commandOwner.transform.DOMoveY(commandOwner.transform.position.y + 2f, dur);
        //commandOwner.GetComponent<Item>().itemSR.DOFade(0f, dur * 3/5)
            //.SetDelay(dur * 2/5)
            //.OnComplete(() => { commandOwner.gameObject.SetActive(false); });

        SwapNodesFunc(selectedObjects, dur);
        AudioManager.instance.PlaySound(AudioManager.instance.swapNode);
        for (int i = 0; i < selectedObjects.Count; i++)
        {
            affectedObjects.Add(selectedObjects[i]);
        }

        for (int i = 0; i < affectedCommands.Count; i++) {
            affectedCommands[i].Execute(dur, isRewinding);
        }
    }

    public override bool Undo(float dur, bool isRewinding = false)
    {
        //commandOwner.transform.DOScale(1f, 0.3f).SetEase(Ease.InOutCubic);
        // Swap postions between two nodes
        //commandOwner.TransformBackToDef();
        bool skippedAll = true;

        for (int i = affectedCommands.Count - 1; i >= 0; i--) {

            bool skipped = affectedCommands[i].Undo(dur, isRewinding);

            if (!isRewinding)
                affectedCommands.RemoveAt(i);

            if (!skipped && skippedAll)
                skippedAll = false;
        }

        Node node1 = selectedObjects[0].GetComponent<Node>();
        Node node2 = selectedObjects[1].GetComponent<Node>();
        HighlightManager highlightManager = HighlightManager.instance;
        if ((node1.isPermanent | node2.isPermanent) && isRewinding)
        {
            gameManager.ChangeCommand(Commands.RemoveNode);
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

        //NodeSwapper nodeSwapper = commandOwner.GetComponent<NodeSwapper>();
        //commandOwner.gameObject.SetActive(true);
        //itemManager.itemContainer.AddItem(commandOwner, -1, dur);
        //commandOwner.transform.DOMoveY(commandOwner.transform.position.y + 2f, 0.5f);



        //nodeSwapper.randomSpriteColor.enabled = false;
        //nodeSwapper.itemSR.DOFade(1f, dur).OnComplete(() => {
        //    if (nodeSwapper.isPermanent)
        //        nodeSwapper.randomSpriteColor.enabled = true;
        //});

        if (node1.isPermanent | node2.isPermanent) {
            return false;
        }

        SwapNodesFunc(affectedObjects, dur);
        if(isRewinding)
            AudioManager.instance.PlaySound(AudioManager.instance.swapNode, true);

        gameManager.ChangeCommand(Commands.SwapNodes);

        return false;
    }


    void SwapNodesFunc(List<GameObject> selectedObjects, float dur)
    {
        Node node1 = selectedObjects[0].GetComponent<Node>();
        Node node2 = selectedObjects[1].GetComponent<Node>();


        List<GameObject> tempArrowsFromThisNode2List = new List<GameObject>();
        List<GameObject> tempArrowsToThisNode2List = new List<GameObject>();

        for (int i = 0; i < node2.arrowsFromThisNode.Count; i++)
        {
            tempArrowsFromThisNode2List.Add(node2.arrowsFromThisNode[i]);
        }
        for (int i = 0; i < node2.arrowsToThisNode.Count; i++)
        {
            tempArrowsToThisNode2List.Add(node2.arrowsToThisNode[i]);
        }

        int arrowsFromThisNodeCount1 = node1.arrowsFromThisNode.Count;
        int arrowsToThisNodeCount1 = node1.arrowsToThisNode.Count;

        node2.ClearArrowsFromThisNodeList();
        for (int i = 0; i < arrowsFromThisNodeCount1; i++)
        {
            node2.AddToArrowsFromThisNodeList(node1.arrowsFromThisNode[i]);
            Arrow arrow = node1.arrowsFromThisNode[i].GetComponent<Arrow>();
            arrow.startingNode = node2.gameObject;
        }
        node2.ClearArrowsToThisNodeList();
        for (int i = 0; i < arrowsToThisNodeCount1; i++)
        {
            node2.AddToArrowsToThisNodeList(node1.arrowsToThisNode[i]);
            Arrow arrow = node1.arrowsToThisNode[i].GetComponent<Arrow>();
            arrow.destinationNode = node2.gameObject;
        }

        node1.ClearArrowsFromThisNodeList();
        for (int i = 0; i < tempArrowsFromThisNode2List.Count; i++)
        {
            node1.AddToArrowsFromThisNodeList(tempArrowsFromThisNode2List[i]);
            Arrow arrow = tempArrowsFromThisNode2List[i].GetComponent<Arrow>();
            arrow.startingNode = node1.gameObject;
        }

        node1.ClearArrowsToThisNodeList();
        for (int i = 0; i < tempArrowsToThisNode2List.Count; i++)
        {
            node1.AddToArrowsToThisNodeList(tempArrowsToThisNode2List[i]);
            Arrow arrow = tempArrowsToThisNode2List[i].GetComponent<Arrow>();
            arrow.destinationNode = node1.gameObject;
        }

        //float dur = 0.4f;
        Vector3 tempPos = node1.transform.localPosition;

        Sequence sequence1 = DOTween.Sequence();
        sequence1.SetDelay(dur * 0.5f/5f);
        sequence1.Append(
            node1.transform.DOScale(0, dur * 2.2f/5f).SetEase(Ease.InBack).OnComplete(() => {
                node1.Deselect(0f);
                node1.transform.localPosition = node2.transform.localPosition;
                node1.itemController.itemContainer.UpdateContainerPos();
            })
        );
        sequence1.Append(node1.transform.DOScale(1, dur * 2.2f/5f).SetEase(Ease.OutBack));

        Sequence sequence2 = DOTween.Sequence();
        sequence2.SetDelay(dur * 0.6f/5f);
        sequence2.Append(
            node2.transform.DOScale(0, dur * 2.2f/5f).SetEase(Ease.InBack).OnComplete(() => {
                node2.Deselect(0f);
                node2.transform.localPosition = tempPos;
                node2.itemController.itemContainer.UpdateContainerPos();
            })
        );
        sequence2.Append(node2.transform.DOScale(1, dur * 2.2f/5f).SetEase(Ease.OutBack));

        sequence1.OnComplete(() => {
            if (PostExecute != null) {
                PostExecute();
            }
        });
    }
}