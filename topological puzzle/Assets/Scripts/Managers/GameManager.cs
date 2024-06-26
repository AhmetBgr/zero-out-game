using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using TMPro;

public enum Commands{
    None, RemoveNode, SwapNodes, ChangeArrowDir, TransformNode, UnlockPadlock, SetArrowPermanent, SetNodePermanent, SetItemPermanent, TransportItem, All
}

public class GameManager : MonoBehaviour{
    public static List<Command> oldCommands = new List<Command>();
    //public static List<Command> allOldCommands = new List<Command>();
    public List<Command> skippedOldCommands = new List<Command>();
    public List<Command> nonRewindCommands = new List<Command>();

    //public LevelManager levelManager;
    public PaletteSwapper paletteSwapper;
    public ItemManager itemManager;
    private AudioManager audioManager;
    public LevelManager levelManager;
    private HighlightManager highlightManager;
    public Palette defPalette;
    public Palette changeArrowDirPalette;
    public Palette rewindPalette;
    public Palette unlockPadlockPalette;
    public Palette swapNodePalette;
    public Palette brushPalette;
    public TextMeshProUGUI undoChangesCountText;
    public InfoIndicator infoIndicator;
    public Commands curCommand;
    private Commands prevCommand;
    public LayerMask targetLM;

    public List<GameObject> selectedObjects = new List<GameObject>();
    public List<Node> nodesPool = new List<Node>();

    private Node commandOwner;
    private bool isCommandOwnerPermanent = false;

    private bool _waitForCommandUpdate;
    public bool waitForCommandUpdate {
        get { return _waitForCommandUpdate;  }
        set {
            _waitForCommandUpdate = value;

            if (value)
                ChangeCommand(Commands.None);
            else
                UpdateCommand();
        }
    }
    public int[] curPriorities = new int[2];

    public float commandDur = 0.5f;
    public float undoDur = 0.1f;
    public int timeID = 0;

    private float time = 0f;
    //private int rewindCount = 0;
    //private int nextRewindCommandIndex = 0;
    private float maxUndoDur = 0.6f;
    private bool rewindStarted = false;
    private bool rewindFinished = false;
    public bool isPriorityActive = false;
    private bool isPlayingAction = false;
    private Sequence rewindSequence;
    public Transform rewindImageParent;
    private IEnumerator setIsActionplayingCor;
        
    public delegate void OnLevelCompleteDelegate(float delay);
    public static OnLevelCompleteDelegate OnLevelComplete;

    public delegate void OnGetNodesDelegate(List<Node> nodesPool);
    public static OnGetNodesDelegate OnGetNodes;

    public delegate void OnPriorityToggleDelegate(bool isActive);
    public static OnPriorityToggleDelegate OnPriorityToggle;

    public delegate void OnRewindDelegate();
    public static OnRewindDelegate OnRewind;

    public delegate void PostRewindDelegate();
    public static PostRewindDelegate PostRewind;

    public int skippedOldCommandCount = 0;
    public int oldCommandCount = 0;
    public int nonRewindCommandsCount = 0;

    void Start(){
        //StartCoroutine(ChangeCommandWithDelay(Commands.RemoveNode, 1f));
        highlightManager = HighlightManager.instance;
        audioManager = AudioManager.instance;
        time = maxUndoDur / 2;

    }

    void OnEnable(){
        LevelManager.OnLevelLoad += ResetData;
        LevelManager.OnLevelLoad += GetNodes;
        LevelManager.OnLevelLoad += UpdateCommand;
        //LevelEditor.OnEnter += ResetData;
        //LevelEditor.OnExit += ResetData;
        LevelEditor.OnExit += GetNodes;
        Command.OnUndoSkipped += AddToSkippedOldCommands;
        Node.OnNodeRemove += CheckForLevelComplete;
    }

    void OnDisable(){
        LevelManager.OnLevelLoad -= ResetData;
        LevelManager.OnLevelLoad -= GetNodes;
        LevelManager.OnLevelLoad -= UpdateCommand;

        //LevelEditor.OnEnter -= ResetData;
        //LevelEditor.OnExit -= ResetData;
        LevelEditor.OnExit -= GetNodes;
        Command.OnUndoSkipped -= AddToSkippedOldCommands;
        Node.OnNodeRemove -= CheckForLevelComplete;
    }

    void Update(){

        if (GameState.gameState == GameState_EN.inMenu && curCommand != Commands.None)
            ChangeCommand(Commands.None);
        else if (GameState.gameState != GameState_EN.inMenu && curCommand == Commands.None && prevCommand != Commands.None)
            ChangeCommand(prevCommand);

        /*if (GameState.gameState != GameState_EN.inMenu && Input.GetKeyDown(KeyCode.LeftAlt)) {
            if(OnPriorityToggle != null) {
                isPriorityActive = !isPriorityActive;
                OnPriorityToggle(isPriorityActive);
            }
        }*/

        if (GameState.gameState != GameState_EN.playing & 
            GameState.gameState != GameState_EN.testingLevel) return;

        if (Input.GetMouseButtonDown(0)){
            Vector2 ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray, Vector2.zero, distance: 100f, 
                layerMask : targetLM);
            if (!hit) return;

            Command command = null;
            selectedObjects.Add(hit.transform.gameObject);

            // Invokes animation start event 
            // so that buttons like undo will be blocked during animation
            GameState.OnAnimationStartEvent(commandDur);
            SetPlayingAction();
            switch (curCommand){
                case Commands.RemoveNode:{
                    commandOwner = selectedObjects[0].GetComponent<Node>();

                    if (commandOwner.CompareTag("BlockedNode")) {
                        BlockedNode blockedNode = commandOwner.GetComponent<BlockedNode>();
                        if (blockedNode.BlockCheck()) {
                            audioManager.PlaySound(audioManager.deny2);
                            selectedObjects.Clear();
                            return;
                        }
                    }
                    else if (commandOwner.itemController.FindItemWithType(ItemType.Padlock)) {
                        audioManager.PlaySound(audioManager.deny);
                        selectedObjects.Clear();
                        return;
                    }

                    // Checks if player intents to remove Square Node,
                    // if so transforms and get last item from the node(if it has any)
                    if (commandOwner.hasShell) { //selectedObjects[0].CompareTag("SquareNode")
                        isCommandOwnerPermanent = commandOwner.isPermanent;

                        TransformToBasicNode transformToBasicNode = new TransformToBasicNode(this, commandOwner);
                        transformToBasicNode.Execute(commandDur);

                        ItemController itemController = selectedObjects[0].GetComponent<Node>()
                            .itemController;
                        Item item = itemController.itemContainer.GetLastItem();
                        if (item) {
                            GetItem getItem = new GetItem(item, itemController, itemManager,
                                this);

                            getItem.Execute(commandDur);

                            transformToBasicNode.affectedCommands.Add(getItem);
                        }

                        //itemManager.CheckAndUseLastItem(itemManager.itemContainer.items);
                        timeID++;
                        AddToOldCommands(transformToBasicNode);
                        UpdateCommand();
                        selectedObjects.Clear();
                        return;
                    }
                    
                    // Removes selected node
                    timeID++;
                    command = new RemoveNode(this, itemManager, selectedObjects[0]);
                    command.Execute(commandDur);

                    break;
                }
                case Commands.ChangeArrowDir:{
                    // Changes dir of selected arrow
                    timeID++;

                    ChangeArrowDir changeArrowDir = new ChangeArrowDir(this, selectedObjects[0], false);
                    changeArrowDir.Execute(commandDur);
                    Item lastItem = itemManager.GetLastItem();
                    if (lastItem && lastItem.isUsable) {
                        UseItem useItem = new UseItem(lastItem, Camera.main.ScreenToWorldPoint(Input.mousePosition), itemManager, this);
                        useItem.Execute(commandDur);
                        changeArrowDir.affectedCommands.Add(useItem);
                    }

                    command = changeArrowDir;
                    break;
                }
                case Commands.SwapNodes: {
                    if (selectedObjects.Count == 2){
                        // Swaps position of selected two nodes
                        timeID++;

                        Node node = selectedObjects[0].GetComponent<Node>(); ;
                        if (selectedObjects[0] == selectedObjects[1]) {
                            node.Deselect(0.2f);
                            selectedObjects.Clear();
                            return;
                        }

                        selectedObjects[1].GetComponent<Node>().Select(0.2f);

                        MultipleComparison<Component> searchTarget =
                            new MultipleComparison<Component>(
                            new List<Comparison> {new CompareNodeAdjecentNode(node)
                        });
                        SwapNodes swapNodes = new SwapNodes(this, itemManager, itemManager.GetLastItem(),
                            selectedObjects, searchTarget);
                        swapNodes.Execute(commandDur);

                        Item lastItem = itemManager.GetLastItem();
                        //TransportCommand transportCommand1 = new TransportCommand(this, arrow, lastItem);
                        //transportCommand1.Execute(commandDur);


                        if (lastItem && lastItem.isUsable) {
                            UseItem useItem = new UseItem(lastItem, Camera.main.ScreenToWorldPoint(Input.mousePosition), itemManager, this); //lastItem.transform.position + Vector3.up
                            useItem.Execute(commandDur);
                            swapNodes.affectedCommands.Add(useItem);
                        }

                        command = swapNodes;
                    }
                    else if (selectedObjects.Count == 1){
                        // Creates new highlight search
                        // so that only nodes adjacent to selected node will be selectable
                        Node node = selectedObjects[0].GetComponent<Node>();
                        MultipleComparison<Component> searchTarget = 
                            new MultipleComparison<Component>(
                            new List<Comparison> { 
                            new CompareNodeAdjecentNode(node)
                        });
                        HighlightManager.instance.Search(searchTarget);
                        node.Select(0.2f);
                        return;
                    }
                    break;
                }
                case Commands.UnlockPadlock:{
                    // Unlocks selected locked node 
                    timeID++;

                    commandOwner = selectedObjects[0].GetComponent<Node>();
                    Key key = itemManager.itemContainer.GetLastItem().GetComponent<Key>();

                    UnlockPadlock unlockPadlock = new UnlockPadlock(this, itemManager,
                        commandOwner, key);
                    unlockPadlock.node = commandOwner;
                    unlockPadlock.Execute(commandDur);

                    command = unlockPadlock;
                    Debug.Log("should unlock node");
                    break;
                }
                case Commands.SetArrowPermanent:{
                    // Sets selected arrow permanent 
                    timeID++;

                    Arrow arrow = selectedObjects[0].GetComponent<Arrow>();
                    Item item = itemManager.itemContainer.GetLastItem();
                    SetArrowPermanent setArrowPermanent = new SetArrowPermanent(arrow, item,
                        this, itemManager);
                    setArrowPermanent.Execute(commandDur);

                    command = setArrowPermanent;
                    break;
                }
                case Commands.TransportItem: {
                    // Sets selected arrow permanent 
                    timeID++;
                    Arrow arrow = selectedObjects[0].GetComponent<Arrow>();
                    int itemCount = arrow.startingNode.GetComponent<ItemController>().itemContainer.items.Count;
                    
                    ItemController startingItemCont = arrow.startingNode.GetComponent<ItemController>();
                    ItemController destItemCont = arrow.destinationNode.GetComponent<ItemController>();

                    TransportCommand transportCommand1 = null;
                    
                    List<Item> items = new List<Item>();
                    items.AddRange(startingItemCont.itemContainer.items);

                    Item item = startingItemCont.FindLastTransportableItem();

                    for (int i = 0; i <itemCount; i++) {
                        TransportCommand transportCommand = new TransportCommand(this, arrow, skipFix: i != 0);
                        transportCommand.Execute(commandDur, skipFix: true);
                        if (i == 0) {
                            transportCommand1 = transportCommand;
                            transportCommand1.items.AddRange(items);
                            transportCommand.isMain = true;
                        }
                        else {
                            transportCommand1.affectedCommands.Add(transportCommand);
                            transportCommand.items = transportCommand1.items;
                        }
                    }

                    List<Vector3> pathlist = new List<Vector3>();
                    pathlist.Add(item.transform.position);
                    pathlist.AddRange(arrow.linePoints);


                    startingItemCont.itemContainer.FixItemPositions(commandDur / 2, setDelayBetweenFixes: true);
                    destItemCont.itemContainer.FixItemPositions(commandDur / 2, itemFixPath: pathlist, setDelayBetweenFixes: true, itemsWithFixPath: items);
                    
                    Item lastItem = itemManager.GetLastItem();
                    //TransportCommand transportCommand1 = new TransportCommand(this, arrow, lastItem);
                    //transportCommand1.Execute(commandDur);

                    
                    if (lastItem && lastItem.isUsable) {
                        UseItem useItem = new UseItem(lastItem, Camera.main.ScreenToWorldPoint(Input.mousePosition), itemManager, this);
                        useItem.Execute(commandDur);
                        transportCommand1.affectedCommands.Add(useItem);
                    }
                    command = transportCommand1;
                    break;
                }
            }

            AddToOldCommands(command);
            UpdateCommand();
            selectedObjects.Clear();
        }

        if (waitForCommandUpdate) return;

        // Rewind
        if ( (Input.GetMouseButtonDown(1) || rewindStarted) && 
            (GameState.gameState == GameState_EN.playing | GameState.gameState == GameState_EN.testingLevel)){

            if (!rewindStarted){
                // Starts rewind
                rewindStarted = true;
                Debug.Log("Rewind should start");
                StartRewind(rewindImageParent.GetComponent<CanvasGroup>());
            }
            
            time += Time.deltaTime;
            if (time >= maxUndoDur){
                Rewind rewind = Rewind();
                //maxUndoDur = rewind.executeDur;
                time = 0;
            }

            if ( ( rewindFinished || (rewindStarted && Input.GetMouseButtonUp(1)) ) && 
                 (GameState.gameState == GameState_EN.playing | 
                 GameState.gameState == GameState_EN.testingLevel)){
                // Finishes rewind
                FinishRewind(rewindImageParent.GetComponent<CanvasGroup>());
            }
            
        }

        if ((Input.GetKeyDown(KeyCode.Z) |  Input.GetMouseButtonDown(2)) && !isPlayingAction)
            Undo();

        UpdateChangesCounter();
        //skippedOldCommandCount = skippedOldCommands.Count;
        //oldCommandCount = oldCommands.Count;
        //nonRewindCommandsCount = nonRewindCommands.Count;
    }

    private void SetPlayingAction() {
        isPlayingAction = true;

        if (setIsActionplayingCor != null)
            StopCoroutine(setIsActionplayingCor);

        setIsActionplayingCor = SetIsActionPlaying(false, commandDur);

        StartCoroutine(setIsActionplayingCor);
    }
    
    private IEnumerator SetIsActionPlaying(bool value, float delay) {
        yield return new WaitForSeconds(delay);
        isPlayingAction = value;
    }
    public void UpdateCommand() {
        UpdateCommand(false);
    }
    public void UpdateCommand(bool skipPalentUpdate = false) {
        //Debug.Log("should update command");
        if (!itemManager.CheckAndUseLastItem(itemManager.itemContainer.items))
            ChangeCommand(Commands.RemoveNode, skipPalentUpdate);
    }
    public void UpdateCommandWithDelay(float delay) {
        Invoke("UpdateCommand", delay);
    }

    public void ChangeCommand(Commands command, bool skipPalentUpdate = false){

        prevCommand = curCommand;
        curCommand = command;
        Palette palette;
        HighlightManager highlightManager = HighlightManager.instance;
        Debug.Log("command updated");
        switch (command) {
            case Commands.All: {
                highlightManager.Search(highlightManager.any);
                //paletteSwapper.ChangePalette(defPalette, 0.02f);
                targetLM = LayerMask.GetMask("Default");
                infoIndicator.HideInfoText(0f);
                break;
            }
            case Commands.RemoveNode: {
                highlightManager.Search(highlightManager.removeNode);
                //paletteSwapper.ChangePalette(defPalette, 0.5f);
                targetLM = LayerMask.GetMask("Node");
                infoIndicator.HideInfoText();
                break;
            }
            case Commands.SetArrowPermanent: {
                highlightManager.Search(highlightManager.setArrowPermanent);
                //paletteSwapper.ChangePalette(brushPalette, 0.5f);
                targetLM = LayerMask.GetMask("Arrow");
                infoIndicator.ShowInfoText(infoIndicator.setArrowPermanentText);
                break;
            }
            case Commands.None: {
                highlightManager.Search(highlightManager.none);
                //paletteSwapper.ChangePalette(defPalette, 0.02f);
                targetLM = LayerMask.GetMask("Default");
                infoIndicator.HideInfoText(0f);
                break;
            }
            case Commands.SwapNodes: {
                highlightManager.Search(highlightManager.onlyLinkedNodes);
                //paletteSwapper.ChangePalette(swapNodePalette, 0.5f);
                targetLM = LayerMask.GetMask("Node");

                infoIndicator.ShowInfoText(infoIndicator.swapNodeText);
                break;
            }
            case Commands.UnlockPadlock: {
                highlightManager.Search(highlightManager.unlockPadlock);
                //paletteSwapper.ChangePalette(unlockPadlockPalette, 0.5f);
                targetLM = LayerMask.GetMask("Node");
                infoIndicator.ShowInfoText(infoIndicator.unlockText);
                break;
            }
            case Commands.ChangeArrowDir: {
                highlightManager.Search(highlightManager.onlyArrow);
                //paletteSwapper.ChangePalette(changeArrowDirPalette, 0.5f);
                targetLM = LayerMask.GetMask("Arrow");

                infoIndicator.ShowInfoText(infoIndicator.changeArrowDirText);
                break;
            }
            case Commands.TransportItem: {
                highlightManager.Search(highlightManager.arrowsWhoCanTransport);
                //paletteSwapper.ChangePalette(changeArrowDirPalette, 0.5f);
                targetLM = LayerMask.GetMask("Arrow");

                infoIndicator.ShowInfoText(infoIndicator.transferItemsText);
                break;
            }
        }
        if (!skipPalentUpdate)
            UpdatePalette();
    }

    public void UpdatePalette() {
        switch (curCommand) {
            case Commands.All: {
                paletteSwapper.ChangePalette(defPalette, 0.02f);
                break;
            }
            case Commands.RemoveNode: {
                paletteSwapper.ChangePalette(defPalette, 0.5f);
                break;
            }
            case Commands.SetArrowPermanent: {
                paletteSwapper.ChangePalette(brushPalette, 0.5f);
                break;
            }
            case Commands.None: {
                paletteSwapper.ChangePalette(defPalette, 0.02f);
                break;
            }
            case Commands.SwapNodes: {
                paletteSwapper.ChangePalette(swapNodePalette, 0.5f);
                break;
            }
            case Commands.UnlockPadlock: {
                paletteSwapper.ChangePalette(unlockPadlockPalette, 0.5f);
                break;
            }
            case Commands.ChangeArrowDir: {
                paletteSwapper.ChangePalette(changeArrowDirPalette, 0.5f);
                break;
            }
            case Commands.TransportItem: {
                paletteSwapper.ChangePalette(changeArrowDirPalette, 0.5f);
                break;
            }
        }
    }

    public void ChangeCommandWithDelay(Commands command, float delay) {
        StartCoroutine(_ChangeCommand(command, delay));
    }
    private IEnumerator _ChangeCommand(Commands command, float delay){
        ChangeCommand(Commands.None);

        yield return new WaitForSeconds(delay);
        ChangeCommand(command);
    }
    
    public void ResetData(){
        curPriorities = new int[]{0, 1};
        timeID = 0;
        selectedObjects.Clear();
        nodesPool.Clear();
        oldCommands.Clear();
        nonRewindCommands.Clear();
        //rewindCount = 0;
        skippedOldCommands.Clear();
        UpdateChangesCounter();
        infoIndicator.HideInfoText();
        /*if (GameState.gameState == GameState_EN.inLevelEditor) {
            ChangeCommand(Commands.All);
            return;
        }
        ChangeCommand(Commands.RemoveNode);*/
    }

    public void AddToOldCommands(Command command, bool addToNonRewindCommands = true){
        oldCommands.Add(command);
        //rewindCount = 0;
        UpdateChangesCounter();

        if (!addToNonRewindCommands) return;

        nonRewindCommands.Add(command);
    }

    public void AddToSkippedOldCommands(Command command){
        skippedOldCommands.Add(command);
        UpdateChangesCounter();
    }

    public void RemoveFromSkippedOldCommands(Command command){
        skippedOldCommands.Remove(command);
        UpdateChangesCounter();
    }

    private void DeselectObjects(){
        if (selectedObjects.Count == 0) return;
        Node node;
        if (selectedObjects[0].TryGetComponent(out node)){
            node.Deselect(0.2f);
            selectedObjects.Clear();
        }
    }

    private void ChangeTargetLayer(LayerMask targetLM){
        this.targetLM = targetLM;
    }

    public Rewind Rewind(){
        //Debug.Log("nonrewind commands count: " + nonRewindCommands.Count);

        if (nonRewindCommands.Count <= 0) return null;
        
        DeselectObjects();

        Rewind rewind = new Rewind(this, 
            nonRewindCommands[nonRewindCommands.Count - 1]);
        rewind.Execute(commandDur, isRewinding: true);

        nonRewindCommands.Remove(nonRewindCommands[nonRewindCommands.Count - 1]);
        UpdateCommand(true);
        //itemManager.CheckAndUseLastItem(itemManager.itemContainer.items);

        if (!rewind.skipped)
            AddToOldCommands(rewind, false);

        return rewind;
    }

    // Undo last command
    public void Undo(){
        if (oldCommands.Count == 0 ) return;

        timeID--;
        DeselectObjects();
        GameState.OnAnimationStartEvent(undoDur + 0.3f);

        Command lastCommand = oldCommands[oldCommands.Count - 1];
        lastCommand.Undo(undoDur, isRewinding: false);

        if (lastCommand.isRewinCommand)
            nonRewindCommands.Add(lastCommand.command0);
        else {
            nonRewindCommands.Remove(lastCommand);
        }

        oldCommands.Remove(lastCommand);

        UpdateChangesCounter();
        UpdateCommand();
    }

    public void UpdateChangesCounter(){
        int changes = oldCommands.Count;
        int pChanges = skippedOldCommands.Count;
        undoChangesCountText.text = $"{changes} | {pChanges}p Changes."; //<color=#F783B0>{changes}</color>
    }

    /*public IEnumerator UndoAll(){
        
        while(oldCommands.Count > 0){
            OnlyUndoLast();
            yield return new WaitForSeconds(0.1f);
        }

        ChangeCommand(Commands.None);
    }*/

    private void GetNodes(){
        if(OnGetNodes != null){
            OnGetNodes(nodesPool);
        }

        Debug.Log("node count: " + nodesPool.Count);
    }


    private void CheckForLevelComplete(GameObject removedNode){
        // Checks if all nodes removed. If so 
        for (int i = 0; i < nodesPool.Count; i++){
            Node node = nodesPool[i];
            if (!node.isRemoved){
                // Nodes remain
                Debug.Log("nodes remain");
                return;
            }
        }

        if (levelManager.curLevelIndex == levelManager.curLevelPool.Count - 2 && levelManager.curPool == LevelPool.Original) {
            Debug.Log("should use fixed pitch for level complete sound.");
            audioManager.levelComplete.useRandomPitch = false;
            audioManager.levelComplete.pitch = 0.22f;
        }
        else {
            audioManager.levelComplete.useRandomPitch = true;
        }

        audioManager.PlaySoundWithDelay(audioManager.levelComplete, 0.5f);

        // Invoke level complete event
        if (OnLevelComplete != null)
            OnLevelComplete(1f);
    }

    public void RewindBPointerEnter(Transform rewindBParent){
        rewindBParent.DOScale(1.3f, 0.3f);
    }
    
    public void RewindBPointerExit(Transform rewindBParent){
        rewindBParent.DOScale(1f, 0.3f);
    }
    
    public void StartRewind(CanvasGroup rewindImageParent){
        if (waitForCommandUpdate) return;

        //ChangeCommand(Commands.None, true);
        paletteSwapper.ChangePalette(rewindPalette, 0.02f);

        rewindStarted = true;
        rewindFinished = false;

        rewindSequence = DOTween.Sequence();
        rewindSequence.Append(rewindImageParent.DOFade(0 , 0.5f));
        rewindSequence.Append(rewindImageParent.DOFade(1 , 0.5f));
        rewindSequence.SetLoops(-1);
        audioManager.PlaySound(audioManager.rewind);

        if (selectedObjects.Count == 1)
            DeselectObjects();

        if (OnRewind != null)
            OnRewind();
    }

    public void FinishRewind(CanvasGroup rewindImageParent){
        time = maxUndoDur / 2;
        rewindStarted = false;

        if (waitForCommandUpdate) return;

        rewindSequence.Kill();
        rewindImageParent.alpha = 1;
        rewindStarted = false;
        rewindFinished = true;
        audioManager.StartFadeOut(audioManager.rewind);
        UpdateCommand();

        if (PostRewind != null)
            PostRewind();
    }

    /*public void SetNextPriorities() {
        int value1 = curPriorities[0] + 2; 
        int value2 = curPriorities[1] + 2; 

        int priorityNext = Transporter.priorityNext; 

        if (value1 >= priorityNext && value2 >= priorityNext) {
            curPriorities[0] = 0;
            curPriorities[1] = 1;
        }
        else if (value2 >= priorityNext) {
            curPriorities[0] = value1;
            curPriorities[1] = value2 - priorityNext;
        }
        else if (value1 >= priorityNext) {
            curPriorities[0] = value1 - priorityNext;
            curPriorities[1] = value2;
        }
        else {
            curPriorities[0] = value1;
            curPriorities[1] = value2;
        }
    }*/
}



