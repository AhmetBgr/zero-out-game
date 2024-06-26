using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class Arrow : MonoBehaviour {
    private enum HighlightState {
        Highlight,
        Dehighlight,
        Selecteble,
        NotSelectable,
        None,
        HintChangeDir
    }

    public GameObject startingNode;
    public GameObject destinationNode;
    //public Material defultMaterial;
    public GameObject arrowPointPrefab;
    public ArrowCC arrowColorController;
    public RandomLRColor randomLRColor;
    public RandomSpriteColor randomSprite;
    public GameObject blockerPrefab;
    public GameObject signalPrefab;
    private Signal signal;

    private List<Transform> blockers;

    public bool isPermanent = false;

    public Transporter transporter;
    private GameManager gameManager;
    public LineRenderer lr;
    private Transform head;
    [HideInInspector] public EdgeCollider2D col;

    private List<ChangeArrowDir> changeDirCommands = new List<ChangeArrowDir>();
    //private Material material;

    private IEnumerator widthAnim;
    private IEnumerator RemoveCor;
    private IEnumerator appearCor;
    private IEnumerator highlightCor;
    private Tween headScaleTween;
    public Vector3[] linePoints;
    public List<ArrowPoint> arrowPoints = new List<ArrowPoint>();
    private HighlightState highlightState;
    //private HighlightState prevHighlightState;
    //private HighlightState nextHighlightState;

    //public Transform arrowPointPreview;
    //public int arrowPointPreviewIndex;
    public float gapForArrowHead = 0.16f;
    private int pointsCount;
    private float defWidth = 0.07f;

    private float time = 0;
    private float t = 0;
    private float dur = 1f;
    private float t2 = 0;
    private float changedirHintLoopDur = 0.5f;
    private bool isHintAppearPhase = false;
    //float width = 0.15f;
    //private bool isSelectable = false;
    [HideInInspector] public bool isRemoved = false;
    private bool isSelected = false;
    /*private bool _isBlocked = false;
    public bool isBlocked {
        get { return _isBlocked; }
        set {
            _isBlocked = value;

            
            //arrowBlocker.gameObject.SetActive(value);
        }
    }*/

    public delegate void BeforeChangeDelegate();
    public event BeforeChangeDelegate BeforeChange;

    public delegate void OnChangedDelegate();
    public event OnChangedDelegate OnChanged;

    void Awake(){
        head = transform.GetChild(0);
        lr = gameObject.GetComponent<LineRenderer>();
        col = GetComponent<EdgeCollider2D>();
        lr.startWidth = defWidth;
        SavePoints();
        gameManager = FindObjectOfType<GameManager>();
        col.enabled = true;
        t2 = changedirHintLoopDur;


    }

    private void Start()
    {
        OnChangedEvent();

        // Create arrow points
        for (int i = 1; i < lr.positionCount - 1; i++)
        {
            Vector3 pos = lr.GetPosition(i);
            ArrowPoint arrowPoint = CreateArrowPoint(pos, i);
            if (GameState.gameState == GameState_EN.inLevelEditor) continue;
            arrowPoint.gameObject.SetActive(false);
        }

        if (isPermanent && !signal) {
            CreateSignal();
        }

        /*for (int i = 0; i < lr.positionCount -1; i++) {
            Vector3 point1 = lr.GetPosition(i);
            Vector3 point2 = lr.GetPosition(i + 1);

            Vector2 origin = point1;
            RaycastHit2D hit = Physics2D.Raycast(origin, (point2-point1).normalized, (point2 - point1).magnitude,
                layerMask: LayerMask.GetMask("Arrow"));

            if (hit && !isBlocked) {
                Transform blocker = Instantiate(blockerPrefab, position: hit.transform.position, Quaternion.identity).transform;
            }
        }*/
    }

    void OnEnable(){
        //Node.OnNodeRemove += ChangeDirIfLinkedToStar;
        RemoveNode.PreExecute += ChangeDirIfLinkedToStar;
        TransformToBasicNode.PreExecute += ChangeDirIfLinkedToStar;
        //Node.OnNodeAdd += UndoChangeDirIfLinkedToStar;

        //GameManager.OnCurCommandChange += CheckIfSuitable;
        HighlightManager.OnSearch += Check;
        HighlightManager.OnAvailibilityCheck += CheckAvailibility;
        LevelManager.OnLevelLoad += GetOnTheLevel;
        LevelEditor.OnEnter += EnableArrowPoints;
        LevelEditor.OnExit += DisableArrowPoints;
        Node.OnPointerEnterRemove += TryHintChangeDir;
        Node.OnPointerExitRemove += TryRevertHint;

    }

    void OnDisable(){
        //Node.OnNodeRemove -= ChangeDirIfLinkedToStar;
        RemoveNode.PreExecute -= ChangeDirIfLinkedToStar;
        TransformToBasicNode.PreExecute -= ChangeDirIfLinkedToStar;

        //Node.OnNodeAdd -= UndoChangeDirIfLinkedToStar;

        //GameManager.OnCurCommandChange -= CheckIfSuitable;
        HighlightManager.OnSearch -= Check;
        HighlightManager.OnAvailibilityCheck -= CheckAvailibility;
        LevelManager.OnLevelLoad -= GetOnTheLevel;
        LevelEditor.OnEnter -= EnableArrowPoints;
        LevelEditor.OnExit -= DisableArrowPoints;
        Node.OnPointerEnterRemove -= TryHintChangeDir;
        Node.OnPointerExitRemove -= TryRevertHint;
    }
    void OnMouseEnter(){
        ChangeHighlightState(HighlightState.Highlight);
    }

    private void OnMouseOver()
    {
        if (GameState.gameState != GameState_EN.inLevelEditor) return;

        if(!LevelEditor.arrowPointPreview.gameObject.activeSelf)
            LevelEditor.arrowPointPreview.gameObject.SetActive(true);

        int posCount = lr.positionCount;
        if (posCount < 2) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //Transform arrowPointPreview = LevelEditor.arrowPointPreview;
        //int arrowPointPrei
        int lastIndex = posCount - 1;

        Vector3 closestSegment = Vector3.zero;
        Vector3 projection = Vector3.zero;
        float closestDistance = 1000f;
        LevelEditor.arrowPointPreviewIndex = lastIndex;

        for (int i = lr.positionCount - 1; i > 0; i--)
        {
            Vector3 segment = lr.GetPosition(i) - lr.GetPosition(i-1);
            Vector3 projection2 = Vector3.Project(mouseWorldPos - lr.GetPosition(i-1), segment);

            float distance = (mouseWorldPos - lr.GetPosition(i - 1) - projection2).magnitude;

            if (distance < closestDistance && projection2.normalized == segment.normalized)
            {
                closestDistance = distance;
                closestSegment = segment;
                LevelEditor.arrowPointPreviewIndex = i;
                projection = projection2;
            }
        }

        if((projection.magnitude < 0.1f | (closestSegment - projection).magnitude < 0.1f) 
            && LevelEditor.arrowPointPreviewIndex != 0 && LevelEditor.arrowPointPreviewIndex != lastIndex)
        {
            col.enabled = false;
            return;
        }

        if (projection.magnitude >= closestSegment.magnitude | projection.normalized != closestSegment.normalized ) return;

        Vector2 pos = projection + lr.GetPosition(LevelEditor.arrowPointPreviewIndex - 1);

        LevelEditor.arrowPointPreview.position = pos;
    }

    void OnMouseExit(){
        if (!isSelected)
            ChangeHighlightState(HighlightState.Dehighlight);
        else
            isSelected = false;

        if (GameState.gameState != GameState_EN.inLevelEditor) return;

        LevelEditor.arrowPointPreview.gameObject.SetActive(false);
    }

    private void OnMouseDown() {
        isSelected = true;
        ChangeHighlightState(HighlightState.NotSelectable);
    }

    private void Update(){
        if (highlightState == HighlightState.None) return;

        if(highlightState != HighlightState.Selecteble) {
            KillHighlightCor();
            KillWidthCor();
        }

        switch (highlightState) {
            case HighlightState.Selecteble: {
                t += Time.deltaTime;
                if (t >= dur) {
                    t = 0;

                    dur = UnityEngine.Random.Range(0.5f, 2f);
                    float glowIntensity = arrowColorController.curGlowIntensity == arrowColorController.glowIntensityMedHigh ?
                        arrowColorController.glowIntensityMedium : arrowColorController.glowIntensityMedHigh;

                    //width = lr.startWidth > defWidth ? defWidth : defWidth + 0.05f;


                    highlightCor = arrowColorController.Highlight(glowIntensity, dur);
                }
                break;
            }
            case HighlightState.Highlight: {
                widthAnim = ChangeWidth(defWidth + 0.1f, 0f);
                StartCoroutine(widthAnim);

                highlightCor = arrowColorController.Highlight(arrowColorController.glowIntensityHigh, 0.1f);

                ChangeHighlightState(HighlightState.None);
                t = dur;

                break;
            }
            case HighlightState.Dehighlight: {
                widthAnim = ChangeWidth(defWidth, 0.1f);
                StartCoroutine(widthAnim);

                ChangeHighlightState(HighlightState.Selecteble);
                t = dur;

                break;
            }
            case HighlightState.NotSelectable: {
                arrowColorController.Highlight(arrowColorController.glowIntensityMedium, 1f);
                StartCoroutine(ChangeWidth(defWidth, 1f));
                t = dur;
                ChangeHighlightState(HighlightState.None);
                break;
            }
            case HighlightState.HintChangeDir: {
                t2 += Time.deltaTime;
                if (t2 >= changedirHintLoopDur) {
                    t2 = 0;

                    if (!isHintAppearPhase) {
                        RemoveCor = RemoveCor = DisappearAnim2(changedirHintLoopDur*8);
                        StartCoroutine(RemoveCor);
                        isHintAppearPhase = true;
                    }
                    else {
                        StopCoroutine(RemoveCor);

                        appearCor = AppearAnim(changedirHintLoopDur, skipDuplicate: true);
                        StartCoroutine(appearCor);
                        isHintAppearPhase = false;
                    }
                }
                break;
            }
        }
    }

    public void CreateSignal() {
        signal = Instantiate(signalPrefab, Vector3.zero, Quaternion.identity).GetComponent<Signal>();
        signal.transform.SetParent(LevelManager.curLevel.transform);
        signal.owner = transform;
    }

    public void TryHintChangeDir(Node node) {
        //if (gameManager.curCommand != Commands.RemoveNode) return;

        Node startNode = this.startingNode.GetComponent<Node>();
        //Node destNode = this.destinationNode.GetComponent<Node>();

        if (startNode == node && !startNode.hasShell) return;
        if (!startingNode.CompareTag("HexagonNode") && !destinationNode.CompareTag("HexagonNode")) return;
        if (startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode")) return;

        //StartCoroutine(ChangeWidth(defWidth + 0.1f, 0f));

        //RemoveCor = DisappearAnim(0.5f); //, onCompleteCallBack : () => { StartCoroutine(AppearAnim(0.2f)); }
        //StartCoroutine(RemoveCor); 

        //Invoke("StopDisappearAnim", 0.2f);
        isHintAppearPhase = false;
        ChangeHighlightState(HighlightState.HintChangeDir);
    }

    public void TryRevertHint(Node node) {
        if (highlightState != HighlightState.HintChangeDir) return;

        /*if (gameManager.curCommand != Commands.RemoveNode) return;

        Node startNode = this.startingNode.GetComponent<Node>();
        //Node destNode = this.destinationNode.GetComponent<Node>();

        if (startNode == node && !startNode.hasShell) return;
        if (!startingNode.CompareTag("HexagonNode") && !destinationNode.CompareTag("HexagonNode")) return;
        if (startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode")) return;

        */
        RevertHint();
        //StartCoroutine(ChangeWidth(defWidth, 0f));
    }

    private void RevertHint() {
        t2 = changedirHintLoopDur;

        ChangeHighlightState(HighlightState.None);
        if (RemoveCor != null)
            StopCoroutine(RemoveCor);
        if (appearCor != null)
            StopCoroutine(appearCor);
        appearCor = AppearAnim(0.02f, skipDuplicate: true);
        StartCoroutine(appearCor);
    }


    private void ChangeHighlightState(HighlightState state, HighlightState nextHighlightState = HighlightState.None) {
        //prevHighlightState =  highlightState;
        highlightState = state;
        //this.nextHighlightState = nextHighlightState;
    }

    // Changes direction on any other node removed if the starting node of this arrow is a star node
    private void ChangeDirIfLinkedToStar(GameObject node, RemoveNode command)
    {
        if (node == startingNode) return;
        if (command.isRewinding) return;

        if (startingNode.CompareTag("HexagonNode") || destinationNode.CompareTag("HexagonNode")){
            GameObject starNode = startingNode.CompareTag("HexagonNode") ? startingNode : destinationNode;
            if (!(startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode")))
            {
                CompareLayer nodeLayer = new CompareLayer(LayerMask.GetMask("Node"));
                MultipleComparison<Component> searchTarget = new MultipleComparison<Component>(new List<Comparison> {
                    new CompareNodeAdjecentNode(starNode.GetComponent<Node>()) });

                //if (!searchTarget.CheckAll(node.GetComponent<Node>())) return;

                ChangeArrowDir changeDirCommand = new ChangeArrowDir(gameManager, gameObject, false, true);
                //changeDirCommand.Execute(gameManager.commandDur);
                command.affectedCommands.Add(changeDirCommand);
                //changeDirCommands.Add(changeDirCommand);
            } 
        }
    }

    private void ChangeDirIfLinkedToStar(GameObject node, TransformToBasicNode command) {
        //if (node == startingNode) return;
        if (command.isRewinding) return;

        if (startingNode.CompareTag("HexagonNode") || destinationNode.CompareTag("HexagonNode")) {
            GameObject starNode = startingNode.CompareTag("HexagonNode") ? startingNode : destinationNode;
            if (!(startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode"))) {
                CompareLayer nodeLayer = new CompareLayer(LayerMask.GetMask("Node"));
                MultipleComparison<Component> searchTarget = new MultipleComparison<Component>(new List<Comparison> {
                    new CompareNodeAdjecentNode(starNode.GetComponent<Node>()) });

                //if (!searchTarget.CheckAll(node.GetComponent<Node>())) return;

                ChangeArrowDir changeDirCommand = new ChangeArrowDir(gameManager, gameObject, false, true);
                //changeDirCommand.Execute(gameManager.commandDur);
                command.affectedCommands.Add(changeDirCommand);
                //changeDirCommands.Add(changeDirCommand);
            }
        }
    }

    public void Remove(float dur){ //GameObject node
        isRemoved = true;
        LevelManager.ChangeArrowCount(-1);
        destinationNode.GetComponent<Node>().RemoveFromArrowsToThisNodeList(gameObject);
        startingNode.GetComponent<Node>().RemoveFromArrowsFromThisNodeList(gameObject);
        RemoveCor = DisappearAnim2(dur, onCompleteCallBack: () =>
        {
            DisableObject();
        });
        StartCoroutine(RemoveCor);
        /*if(node == startingNode){
            //Debug.Log("starting node equls to selected node");
            LevelManager.ChangeArrowCount(-1);
            destinationNode.GetComponent<Node>().RemoveFromArrowsToThisNodeList(gameObject);
            RemoveCor = DisappearAnim(0.6f, onCompleteCallBack: () =>
            {
                DisableObject();
            });
            StartCoroutine(RemoveCor);

        }
        else if(startingNode.CompareTag("HexagonNode") || destinationNode.CompareTag("HexagonNode")){
            if(!( startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode") ))
                ChangeDir(); //gameObject
        }*/
    }

    public void Add(float dur){//GameObject node

        /*if(node == startingNode && isPermanent){ 
            gameObject.SetActive(false);
            return; 
        }
        */
        isRemoved = false;
        if (RemoveCor != null)
            StopCoroutine(RemoveCor);
        destinationNode.GetComponent<Node>().AddToArrowsToThisNodeList(gameObject);
        startingNode.GetComponent<Node>().AddToArrowsFromThisNodeList(gameObject);
        StartCoroutine(AppearAnim(dur/2, dur/2, OnComplete: () => {
            LevelManager.ChangeArrowCount(+1);
        }));
        
        /*if(node == startingNode){
            if(RemoveCor != null)
                StopCoroutine(RemoveCor);
            destinationNode.GetComponent<Node>().AddToArrowsToThisNodeList(gameObject);
            StartCoroutine(AppearAnim(0.4f, 0.4f, () => {
                LevelManager.ChangeArrowCount(+1);
            }));
        }
        else if(!isPermanent && (startingNode.CompareTag("HexagonNode") || destinationNode.CompareTag("HexagonNode"))){
            if (!(startingNode.CompareTag("HexagonNode") && destinationNode.CompareTag("HexagonNode")))
            {
                if(RemoveCor != null)
                    StopCoroutine(RemoveCor);
                ChangeDir(); //gameObject
            }

        }*/
        
        /*if(isMagical){
            ChangeDir(gameObject, 0.8f);
        }   */     

        
    }

    private void GetOnTheLevel(){
        //transform.localScale = Vector3.zero;
        //head.localScale = Vector3.zero;
        //head.DOScale(Vector3.one, 1f);
        float duration = UnityEngine.Random.Range(0.2f, 0.7f);
        StartCoroutine(AppearAnim(duration, 0f));
    }

    public void ChangePermanent(bool isPermanent){
        this.isPermanent = isPermanent;
        randomLRColor.enabled = isPermanent;
        randomSprite.enabled = isPermanent;
        if (!isPermanent){
            arrowColorController.ChangeToDefaultColors();
            if(signal)
                Destroy(signal.gameObject);
        }
        else {
            CreateSignal();
        }
    }

    public void ChangeDirOnUndo(GameObject arrow){
        if(isPermanent) return;
        ChangeDir(0.5f); //arrow
    }

    public void ChangeDirSubscriber(GameObject arrow){
        //if(isPermanent){return;}

        ChangeDir(0.5f); //arrow

    }

    public void CheckAvailibility(MultipleComparison<Component> mp) {
        if (isRemoved) return;

        if (mp.CompareAll(this)) {
            HighlightManager.instance.availibleItemCount++;
        }
    }

    public void Check(MultipleComparison<Component> mp)
    {
        if (isRemoved) return;

        t2 = changedirHintLoopDur;
        ChangeHighlightState(HighlightState.None);
        if(RemoveCor != null) 
            StopCoroutine(RemoveCor);
        if(appearCor != null) 
            StopCoroutine(appearCor);

        if (mp.CompareAll(this))
        {
            //arrowColorController.Highlight(arrowColorController.glowIntensityHigh, 1f);
            col.enabled = true;
            //isSelectable = true;
            dur = UnityEngine.Random.Range(0.2f, 2f);
            t = 0;
            widthAnim = ChangeWidth(defWidth, 0f);
            StartCoroutine(widthAnim);
            ChangeHighlightState(HighlightState.Selecteble);
        }
        else
        {
            col.enabled = false;
            ChangeHighlightState(HighlightState.NotSelectable);
        }
    }

    public void ChangeDir(float dur, float delay = 0f){ //GameObject arrow, 
        //if(arrow != gameObject) return;

        col.enabled = false;

        /*head.DOScale(Vector3.zero, 0.5f);
        StartCoroutine(ChangeWidth(0.08f, 0.5f, 0f, () => {
            Debug.Log("on complete working");
            InvertPoints();
            StartCoroutine(ChangeWidth(defWidth, 0.5f));
            head.position = linePoints[linePoints.Length - 1];
            head.DOScale(Vector3.one, 0.5f);
            col.enabled = true;
        }));*/

        Node node1 = startingNode.GetComponent<Node>();
        Node node2 = destinationNode.GetComponent<Node>();

        node1.RemoveFromArrowsFromThisNodeList(gameObject);
        node1.AddToArrowsToThisNodeList(gameObject);

        node2.RemoveFromArrowsToThisNodeList(gameObject);
        node2.AddToArrowsFromThisNodeList(gameObject);

        GameObject temp = startingNode;
        startingNode = destinationNode;
        destinationNode = temp;

        BeforeChangeEvent();
        delay = dur == 0 ? 0 : delay;
        StartCoroutine(DisappearAnim(dur/2, delay, onCompleteCallBack : () => {
            InvertPoints();
            StartCoroutine(AppearAnim(dur/2));
            OnChangedEvent();

            //col.enabled = true;
        }));
    }

    private IEnumerator DisappearAnim(float duration, float delay = 0f, Action onCompleteCallBack = null){
        yield return new WaitForSeconds(delay);

        int piece = (1*pointsCount - 1 );
        //piece =  (2*pointsCount - 3 );
        float segmentDuration = duration / piece ;
        //Debug.Log("here1");


        for (int i = pointsCount - 1; i >= 1 ; i--) {
            //Debug.Log("here2");
            float startTime = Time.time;

            Vector3 startPosition = lr.GetPosition(i - 1); //linePoints [ i - 1 ];
            Vector3 endPosition = lr.GetPosition(i); // linePoints [ i ];

            Vector3 pos = endPosition;

            while (pos != startPosition) {
                 
                float t = segmentDuration == 0 ? 1 : (Time.time - startTime) / segmentDuration ;
                //Debug.Log("here3");
                pos = Vector3.Lerp (endPosition, startPosition, t) ;
                Vector3 pos2 = Vector3.zero;
                if(i>=2)
                    pos2 = Vector3.Lerp(lr.GetPosition(i - 1), lr.GetPosition(i - 2), t); //Vector3.Lerp(linePoints[i-1], linePoints[i-2], t);

                // animate all other points except point at index i
                for (int j = i; j < pointsCount; j++){
                    //Debug.Log("here4");
                    lr.SetPosition (j, pos);

                    if(i>= 2){
                        lr.SetPosition(j-1, pos2);
                        
                        Vector3 lookAt = lr.GetPosition(j-2); // world pos
                        float AngleRad = Mathf.Atan2( head.position.y - lookAt.y,  head.position.x - lookAt.x);
                        float AngleDeg = (180 / Mathf.PI) * AngleRad;
                        head.rotation = Quaternion.Euler(0, 0, AngleDeg);
                    }
                    head.position = pos;

                    Transporter transporter;
                    if (j == 1 && transform.CompareTag("TransporterArrow") && TryGetComponent(out transporter)) {
                        transporter.priorityObj.position = FindCenter();
                    }
                }
                yield return null ;
            }
        }

        if (onCompleteCallBack != null)
            onCompleteCallBack();
    }

    private IEnumerator DisappearAnim2(float duration, Action onCompleteCallBack = null) {

        int piece = (1 * pointsCount - 1);
        //piece =  (2*pointsCount - 3 );

        //Debug.Log("here1");
        float totalLenght = 0f;
        for (int i = pointsCount - 1; i > 0; i--) {
            totalLenght += (linePoints[i] - linePoints[i - 1]).magnitude;
        }

        for (int i = pointsCount - 1; i >= 1; i--) {
            //Debug.Log("here2");
            float startTime = Time.time;

            Vector3 startPosition = linePoints[i - 1];
            Vector3 endPosition = linePoints[i];
            Vector3 pos = endPosition;

            float pieceLength = (endPosition - startPosition).magnitude;
            float segmentDuration = duration * (pieceLength / totalLenght);

            Vector3 lookAt = startPosition; // world pos
            float AngleRad = Mathf.Atan2(endPosition.y - lookAt.y, endPosition.x - lookAt.x);
            float AngleDeg = (180 / Mathf.PI) * AngleRad;
            //head.rotation = Quaternion.Euler(0, 0, AngleDeg);

            bool headRotationChanged = head.rotation == Quaternion.Euler(0, 0, AngleDeg) ? false : true;
            if (headRotationChanged) {
                head.DORotate(Quaternion.Euler(0, 0, AngleDeg).eulerAngles, 0.15f);
            }

            while (pos != startPosition) {
                float t = (Time.time - startTime) / segmentDuration;
                //Debug.Log("here3");
                pos = Vector3.Lerp(endPosition, startPosition, t);

                // animate all other points except point at index i
                for (int j = i; j < pointsCount; j++) {
                    //Debug.Log("here4");
                    lr.SetPosition(j, pos);

                    head.position = pos;


                }
                yield return null;
            }
        }

        if (onCompleteCallBack != null)
            onCompleteCallBack();
    }

    private IEnumerator AppearAnim(float duration, float delay = 0f, bool skipDuplicate = false,  Action OnComplete = null){

        if (!skipDuplicate) {
            head.transform.localScale = Vector3.zero;
            head.transform.position = linePoints[0];
            //transform.position = new Vector3(0f, -2000f, 0f);
            delay = duration == 0 ? 0 : delay;
            float dur = delay == 0f ? duration / 2 : delay;
            //transform.DOMove(Vector3.zero, 0f).SetDelay(delay);
            head.transform.DOScale(Vector3.one, dur).SetDelay(delay);

            lr.enabled = false;
        }

        yield return new WaitForSeconds(delay);

        lr.enabled = true;

        int piece = (1*pointsCount - 1 );
        //piece =  (2*pointsCount - 3 );
        float segmentDuration = duration / piece ;


        for (int i = 0; i < pointsCount - 1; i++) {
            float startTime = Time.time;

            Vector3 startPosition = linePoints [ i ];
            Vector3 endPosition = linePoints [ i + 1 ];



            Vector3 lookAt = startPosition; // world pos
            float AngleRad = Mathf.Atan2( endPosition.y - lookAt.y,  endPosition.x - lookAt.x);
            float AngleDeg = (180 / Mathf.PI) * AngleRad;
            //head.rotation = Quaternion.Euler(0, 0, AngleDeg);

            bool headRotationChanged = head.rotation == Quaternion.Euler(0, 0, AngleDeg) ? false : true;
            if(headRotationChanged){
                head.DORotate(Quaternion.Euler(0, 0, AngleDeg).eulerAngles, 0.15f);
            }
            /*float time = 0;
            while (time < segmentDuration) {
                time += Time.deltaTime;
                float t = segmentDuration == 0 ? 1 : (Time.time - startTime) / segmentDuration;
                pos = Vector3.Lerp(startPosition, endPosition, t);

                // animate all other points except point at index i
                for (int j = i + 1; j < pointsCount; j++) {
                    lr.SetPosition(j, pos);

                    Transporter transporter;
                    if (j == 1 && transform.CompareTag("TransporterArrow") && TryGetComponent(out transporter)) {
                        transporter.priorityObj.position = FindCenter();
                    }
                }
                head.position = pos; //lr.GetPosition(pointsCount -1)
                yield return null;
            }*/

            if (skipDuplicate && startPosition == lr.GetPosition(i)) {
                startPosition = lr.GetPosition(i + 1);
            }
            Vector3 pos = startPosition;
            while (pos != endPosition) {

                t = segmentDuration == 0 ? 1 : (Time.time - startTime) / segmentDuration ;
                pos = Vector3.Lerp (startPosition, endPosition, t) ;

                // animate all other points except point at index i
                for (int j = i + 1; j < pointsCount; j++){
                    lr.SetPosition (j, pos) ;

                    Transporter transporter;
                    if(j == 1 && transform.CompareTag("TransporterArrow") && TryGetComponent(out transporter)) {
                        transporter.priorityObj.position = FindCenter();
                    }
                }
                head.position = pos; //lr.GetPosition(pointsCount -1)
                yield return null ;
            }
        }

        

        // animate all other points except point at index i
        /*for (int i = 0; i < pointsCount; i++) {
            lr.SetPosition(i, linePoints[i]);

        }
        head.position = linePoints[pointsCount - 1]; //lr.GetPosition(pointsCount -1)*/

        OnChangedEvent();

        if(OnComplete != null)
            OnComplete();
    }

    private IEnumerator ChangeWidth(float targetWidth, float duration, float delay = 0f, Action OnComplete = null){
        //animatingWidth = true;
        Vector3 endScale = targetWidth > defWidth ? Vector3.one * 1.5f : Vector3.one;
        if(headScaleTween != null)
        {
            headScaleTween.Kill();
        }

        headScaleTween = head.DOScale(endScale , duration).SetDelay(delay);

        yield return new WaitForSeconds(delay);

        float initialTime = Time.time;
        float initialWidth = lr.startWidth;
        float time = 0;
        while(time < duration){
            float t = time / duration; //(Time.time - initialTime) / duration
            float width = Mathf.Lerp(initialWidth, targetWidth, t);
            time += Time.deltaTime;

            lr.startWidth = width;

            yield return null;
        }
        //animatingWidth = false;
        lr.startWidth = targetWidth;
        widthAnim = null;
        if(OnComplete != null) 
            OnComplete();
    }

    public void SavePoints(){
        // Store a copy of lineRenderer's points in linePoints array
        pointsCount = lr.positionCount;
        linePoints = new Vector3[pointsCount] ;
        for (int i = 0; i < pointsCount; i++) {
            linePoints [ i ] = lr.GetPosition (i) ;
        }
    }

    private void InvertPoints(){
        BeforeChangeEvent();

        for (int i = 0; i < linePoints.Length/2; i++){
            if(i == 0){

            }

            Vector3 temp;
            temp = linePoints[i];
            linePoints[i] = linePoints[linePoints.Length - 1 - i];
            linePoints[linePoints.Length - 1 - i] = temp;     
        }

        // Carries the gap for the arrow's head 
        for (int i = 0; i < linePoints.Length; i += linePoints.Length - 1){
            
            Vector3 from = linePoints[ Mathf.Abs(1 - i) ]; // next point pos after the first one or previous point pos before the last one, depends on i
            Vector3 to = linePoints[i]; // first or last point pos depends on i
            Vector3 dir = (from - to);

            float segmentLength = dir.magnitude;
            float gap = i == 0 ? 0.16f : -0.16f;
            segmentLength += gap; 

            linePoints[i] = from - (dir.normalized*segmentLength);
        }

        for (int i = 0; i < linePoints.Length; i++){
            lr.SetPosition(i, linePoints[0]);
        }
        //transform.position = linePoints[0];
        OnChangedEvent();
    }


    public Vector3 FindCenter()
    {
        return (lr.GetPosition(0) + lr.GetPosition(1)) / 2;
    }

    private void DisableObject(){
        gameObject.SetActive(false);
    }



    public void FixHeadPos(){
        head.localPosition = transform.InverseTransformPoint( lr.GetPosition(lr.positionCount - 1));
        Vector3 lookAt = lr.GetPosition(lr.positionCount - 2); // world pos
        float AngleRad = Mathf.Atan2( head.position.y - lookAt.y,  head.position.x - lookAt.x);
        float AngleDeg = (180 / Mathf.PI) * AngleRad;
        head.rotation = Quaternion.Euler(0, 0, AngleDeg);
    }

    public void FixCollider(){
        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < lr.positionCount - 1; i++){
            points.Add((Vector2)lr.GetPosition(i));
        }
        points.Add((Vector2)lr.GetPosition(lr.positionCount - 1));
        col.points = points.ToArray();
    }

    public void FixEdgePointPos(Node edgeNode, Vector3 neighborPos, int edgeIndex)
    {
        Vector3 fixedEdgePointPos = edgeNode.col.ClosestPoint(neighborPos);
        if (edgeIndex == lr.positionCount - 1)
        {
            // Leave gap for arrow head to fit in between last line pos and the node
            Vector3 dir = fixedEdgePointPos - neighborPos;
            float length = dir.magnitude - gapForArrowHead;
            fixedEdgePointPos = dir.normalized * length + neighborPos;

            fixedEdgePointPos = new Vector3(fixedEdgePointPos.x, fixedEdgePointPos.y, 0f);
        }
        lr.SetPosition(edgeIndex, fixedEdgePointPos);
    }

    public ArrowPoint CreateArrowPoint(Vector3 pos, int index)
    {
        ArrowPoint arrowPoint = Instantiate(arrowPointPrefab, pos, Quaternion.identity).GetComponent<ArrowPoint>();
        arrowPoint.transform.SetParent(LevelManager.curLevel.transform);
        arrowPoint.arrow = this;
        arrowPoint.index = index;
        arrowPoints.Add(arrowPoint);
        return arrowPoint;
    }
    public void RemoveArrowPoint(ArrowPoint arrowPoint, bool destroyAfterRemoving = false)
    {
        arrowPoints.Remove(arrowPoint);

        if(destroyAfterRemoving)
            Destroy(arrowPoint.gameObject);
    }

    public void UpdateArrowPointPosition(Transform arrowPoint, Vector3 pos)
    {
        arrowPoint.position = pos;
    }

    public void UpdateAllArrowPoints()
    {
        for (int i = 0; i < arrowPoints.Count; i++)
        {
            arrowPoints[i].index = i + 1;
            arrowPoints[i].transform.position = lr.GetPosition(i + 1);
        }
    }

    private void EnableArrowPoints()
    {
        foreach (var item in arrowPoints)
        {
            item.gameObject.SetActive(true);
        }
    }

    private void DisableArrowPoints()
    {
        foreach (var item in arrowPoints)
        {
            item.gameObject.SetActive(false);
        }
    }
    public ArrowPoint FindArrowPoint(Vector3 pos, int index)
    {
        foreach (var item in arrowPoints)
        {
            if (index == item.index)
            {
                return item;
            }
        }
        return CreateArrowPoint(pos, index);
    }

    public void InsertLinePoint(Vector3 pos, int index)
    {
        lr.positionCount++;
        Vector3[] positions = new Vector3[lr.positionCount];
        lr.GetPositions(positions);
        lr.SetPosition(index, pos);
        UpdateArrowPointPosition(FindArrowPoint(pos, index).transform, pos);
        for (int i = index+1; i < lr.positionCount; i++)
        {
            lr.SetPosition(i, positions[i-1]);

        }
        FixCollider();
        SavePoints();
        UpdateAllArrowPoints();

    }
    public void RemoveLinePointAt(int index)
    {
        if (index > lr.positionCount - 1) return;

        Vector3[] positions = new Vector3[lr.positionCount];
        lr.GetPositions(positions);
        var pointsList = new List<Vector3>(positions);
        pointsList.RemoveAt(index);
        positions = pointsList.ToArray();
        lr.positionCount--;
        lr.SetPositions(positions);
        FixCollider();
        SavePoints();
        UpdateAllArrowPoints();
        if (index == lr.positionCount - 2)
            FixHeadPos();
    }

    // Moves single line point in word pos
    public void MoveLinePoint(int linePointIndex, Vector3 targetPos)
    {
        BeforeChangeEvent();
        lr.SetPosition(linePointIndex, targetPos);

        // Also moves the head if moving last line point
        if ( linePointIndex == lr.positionCount - 1)
        {
            FixHeadPos();
        }

        OnChangedEvent();
    }

    private void KillWidthCor()
    {
        if (widthAnim != null)
        {
            StopCoroutine(widthAnim);
            widthAnim = null;
        }
    }
    private void KillHighlightCor()
    {
        if (highlightCor != null)
        {
            arrowColorController.StopAllCoroutines();
            highlightCor = null;
        }
    }
    private void BeforeChangeEvent()
    {
        if (BeforeChange != null)
        {
            BeforeChange();
        }
    }
    public void OnChangedEvent()
    {
        if(OnChanged != null)
        {
            OnChanged();
        }
    }
}
