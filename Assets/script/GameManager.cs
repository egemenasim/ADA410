using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum PlayerColor { Blue = 0, Green = 1, Yellow = 2, Red = 3 }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Board tiles: 0..39 main path, 40..55 home run tiles (4 per color: blue(40-43), green(44-47), yellow(48-51), red(52-55))
    [Header("Board")]
    [Tooltip("Assign exactly 56 Transforms: 0..39 main path (starting from Blue start), 40..55 home run tiles (4 per color in order Blue,Green,Yellow,Red)")]
    public Transform[] boardTiles = new Transform[56];

    [Tooltip("Assign 16 spawn slots (4 per color): Blue 0..3, Green 4..7, Yellow 8..11, Red 12..15")]
    public Transform[] spawnSlots = new Transform[16];

    [Header("Pawn Prefabs")]
    public GameObject pawnPrefabBlue;
    public GameObject pawnPrefabGreen;
    public GameObject pawnPrefabYellow;
    public GameObject pawnPrefabRed;

    [Header("UI")]
    public Button rollDiceButton;
    public Button selectPawnButton;
    public Button makeAMoveButton;
    public TextMeshProUGUI rollText;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI turnIndicatorDot; // the "." indicator next to Turn text

    [Header("Markers")]
    [Tooltip("Seçili pawnın yukarı aşağı hareket mesafesi")]
    public float pawnFloatHeight = 0.1f;
    [Tooltip("Floating animasyon hızı")]
    public float pawnFloatSpeed = 2f;

    [Header("Movement")]
    [Tooltip("Pawn hareket ederken zıplama yüksekliği")]
    public float pawnJumpHeight = 0.6f;

    [Header("Dice")]
    public Transform dice3D; // 3D dice model
    public float diceRotationDuration = 0.5f;

    // gameplay state
    [HideInInspector] public int currentRoll = 0;
    private List<Pawn> allPawns = new List<Pawn>();
    private List<Pawn> movablePawns = new List<Pawn>();
    private int selectedPawnIndex = -1;
    private Pawn currentFloatingPawn = null;
    private Coroutine floatingCoroutine = null;
    private Vector3 floatingStartPos; // floating animasyon başlangıç pozisyonu

    private PlayerColor[] turnOrder = new PlayerColor[] { PlayerColor.Blue, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Red };
    private int currentTurnIndex = 0;
    private bool isAIPlaying = false;

    // safe tiles on main path (start tiles) indices
    private HashSet<int> safeMainIndices = new HashSet<int> { 0, 9, 19, 29 }; // using 0-based: 1,10,20,30 become 0,9,19,29
    private bool isMoving = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;

        if (rollDiceButton != null)
            rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
        if (selectPawnButton != null)
            selectPawnButton.onClick.AddListener(OnSelectPawnButtonClicked);
        if (makeAMoveButton != null)
            makeAMoveButton.onClick.AddListener(OnMakeAMoveButtonClicked);
    }

    private void Start()
    {
        // instantiate all pawns at spawn slots
        InstantiateAllPawns();
        UpdateRollText(0);
        UpdateTurnText();
        UpdateButtonStates();
        // debug overview
        Debug.Log($"GameManager started. boardTiles length={boardTiles?.Length ?? 0}, spawnSlots length={spawnSlots?.Length ?? 0}, pawns instantiated={allPawns.Count}");
        for (int i = 0; i < allPawns.Count; i++)
        {
            var p = allPawns[i];
            if (p != null)
            {
                var col = p.GetComponent<Collider>();
                Debug.Log($"Pawn[{i}] = {p.name}, color={p.Color}, localIndex={p.LocalIndex}, pos={p.transform.position}, hasCollider={(col!=null)}");
            }
        }
    }

    private void OnDestroy()
    {
        if (rollDiceButton != null)
            rollDiceButton.onClick.RemoveListener(OnRollDiceButtonClicked);
        if (selectPawnButton != null)
            selectPawnButton.onClick.RemoveListener(OnSelectPawnButtonClicked);
        if (makeAMoveButton != null)
            makeAMoveButton.onClick.RemoveListener(OnMakeAMoveButtonClicked);
    }

    #region Instantiation
    private void InstantiateAllPawns()
    {
        allPawns.Clear();

        // Blue spawn slots 0..3
        for (int i = 0; i < 4; i++)
            SpawnPawn(PlayerColor.Blue, i, spawnSlots != null && spawnSlots.Length > i ? spawnSlots[i] : null);

        // Green spawn slots 4..7
        for (int i = 0; i < 4; i++)
            SpawnPawn(PlayerColor.Green, i, spawnSlots != null && spawnSlots.Length > 4 + i ? spawnSlots[4 + i] : null);

        // Yellow spawn slots 8..11
        for (int i = 0; i < 4; i++)
            SpawnPawn(PlayerColor.Yellow, i, spawnSlots != null && spawnSlots.Length > 8 + i ? spawnSlots[8 + i] : null);

        // Red spawn slots 12..15
        for (int i = 0; i < 4; i++)
            SpawnPawn(PlayerColor.Red, i, spawnSlots != null && spawnSlots.Length > 12 + i ? spawnSlots[12 + i] : null);
    }

    private void SpawnPawn(PlayerColor color, int localIndex, Transform spawnSlot)
    {
        GameObject prefab = GetPrefabForColor(color);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab assigned for {color} pawns.");
            return;
        }

        Vector3 pos = spawnSlot != null ? spawnSlot.position : transform.position;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnSlot != null ? spawnSlot : transform);
        Pawn p = go.GetComponent<Pawn>();
        if (p == null) p = go.AddComponent<Pawn>();
        p.Initialize(color, localIndex);
        // record which spawn slot index (0..15) it was created at so SendHome can reuse indices if needed
        if (spawnSlot != null && spawnSlots != null)
        {
            for (int si = 0; si < spawnSlots.Length; si++)
            {
                if (spawnSlots[si] == spawnSlot)
                {
                    p.SetSpawnSlotIndex(si);
                    break;
                }
            }
        }
        allPawns.Add(p);
    }

    private GameObject GetPrefabForColor(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Blue: return pawnPrefabBlue;
            case PlayerColor.Green: return pawnPrefabGreen;
            case PlayerColor.Yellow: return pawnPrefabYellow;
            case PlayerColor.Red: return pawnPrefabRed;
        }
        return null;
    }

    #endregion

    #region UI / Dice / Buttons
    private void OnRollDiceButtonClicked()
    {
        if (currentRoll != 0) return; // already rolled

        int val = UnityEngine.Random.Range(1, 7);
        currentRoll = val;
        UpdateRollText(val, turnOrder[currentTurnIndex]);
        Debug.Log($"Dice rolled: {val}");

        // rotate dice to show the rolled face
        if (dice3D != null)
            StartCoroutine(RotateDiceToFace(val));

        // compute movable pawns for current player
        movablePawns.Clear();
        selectedPawnIndex = -1;
        StopFloatingAnimation();

        PlayerColor current = turnOrder[currentTurnIndex];
        foreach (var p in allPawns)
        {
            if (p.Color == current && CanPawnMoveWithRoll(p, val))
                movablePawns.Add(p);
        }

        if (movablePawns.Count == 0)
        {
            Debug.Log($"No valid moves for {current} with roll {val}, will pass turn after brief delay.");
            StartCoroutine(AutoPassIfNoMove(val));
        }

        UpdateButtonStates();
    }

    private void OnSelectPawnButtonClicked()
    {
        if (movablePawns.Count == 0) return;

        // cycle through movable pawns
        selectedPawnIndex = (selectedPawnIndex + 1) % movablePawns.Count;
        Pawn selected = movablePawns[selectedPawnIndex];

        // stop old floating animation
        StopFloatingAnimation();

        // start floating animation on selected pawn
        currentFloatingPawn = selected;
        if (this != null && gameObject.activeInHierarchy)
            floatingCoroutine = StartCoroutine(FloatPawn(selected));

        Debug.Log($"Selected pawn: {selected.Color}#{selected.LocalIndex}");
        UpdateButtonStates();
    }

    private void OnMakeAMoveButtonClicked()
    {
        if (selectedPawnIndex < 0 || selectedPawnIndex >= movablePawns.Count) return;
        if (currentRoll == 0) return;

        Pawn selected = movablePawns[selectedPawnIndex];
        StopFloatingAnimation();
        StartCoroutine(TryMovePawn(selected, currentRoll));
    }

    private void UpdateButtonStates()
    {
        // Disable all buttons during AI turn
        bool isPlayerTurn = turnOrder[currentTurnIndex] == PlayerColor.Blue;
        
        // Roll Dice: enabled if currentRoll == 0 and it's player's turn
        if (rollDiceButton != null)
            rollDiceButton.interactable = (currentRoll == 0 && !isMoving && isPlayerTurn);

        // Select A Pawn: enabled if there are movable pawns and it's player's turn
        if (selectPawnButton != null)
            selectPawnButton.interactable = (movablePawns.Count > 0 && !isMoving && isPlayerTurn);

        // Make A Move: enabled if a pawn is selected and it's player's turn
        if (makeAMoveButton != null)
            makeAMoveButton.interactable = (selectedPawnIndex >= 0 && selectedPawnIndex < movablePawns.Count && !isMoving && isPlayerTurn);
    }

    public void RollDice()
    {
        int val = UnityEngine.Random.Range(1, 7);
        currentRoll = val;
        // always show the roll immediately and color it by the current player
        UpdateRollText(val, turnOrder[currentTurnIndex]);
        Debug.Log($"Dice rolled: {val}");
        // if there is no legal move for this player with this roll, show the value briefly then pass the turn
        if (!HasAnyValidMoveForCurrentPlayer(val))
        {
            Debug.Log($"No valid moves for {turnOrder[currentTurnIndex]} with roll {val}, will pass turn after brief delay.");
            StartCoroutine(AutoPassIfNoMove(val));
        }
    }

    private IEnumerator AutoPassIfNoMove(int val)
    {
        // leave the roll visible for a short moment so player can see it
        float delay = 1.0f;
        yield return new WaitForSeconds(delay);

        // if the roll hasn't been consumed or changed meanwhile, pass the turn
        if (currentRoll == val)
        {
            Debug.Log($"Auto-passing after roll {val} for {turnOrder[currentTurnIndex]}");
            AfterMoveConsumeRoll(val);
        }
    }

    private void UpdateRollText(int val, PlayerColor? color = null)
    {
        if (rollText == null) return;

        if (val == 0)
        {
            // hide default '-' as requested
            rollText.text = "";
            rollText.color = Color.white;
        }
        else
        {
            rollText.text = val.ToString();
            // color by player if provided
            if (color.HasValue)
                rollText.color = GetUnityColor(color.Value);
        }
    }

    private void UpdateTurnText()
    {
        if (turnText != null) turnText.text = $"Turn: {turnOrder[currentTurnIndex]}";
        
        // update turn indicator dot color
        if (turnIndicatorDot != null)
        {
            turnIndicatorDot.color = GetUnityColor(turnOrder[currentTurnIndex]);
        }

        // if current player is AI (not Blue), start AI turn
        if (turnOrder[currentTurnIndex] != PlayerColor.Blue && !isAIPlaying)
        {
            StartCoroutine(PlayAITurn());
        }
    }

    private IEnumerator RotateDiceToFace(int face)
    {
        if (dice3D == null) yield break;

        // Kullanıcının zar konumlandırması:
        // 1: Kameraya bakıyor (bana - front, kameraya doğru)
        // 4: Üstte (+Y)
        // 3: Altta (-Y)
        // 2: X pozitif eksende (sağ, +X)
        // 5: X negatif eksende (sol, -X)
        // 6: Arka (+Z, kameradan uzak)
        //
        // Hedef: Her zar değeri için ilgili yüzü Z negatif eksene (kameraya) döndür

        Quaternion targetRotation = Quaternion.identity;
        switch (face)
        {
            case 1: targetRotation = Quaternion.Euler(0, 180, 0); break; // 1 -> 6'nın rotasyonu
            case 2: targetRotation = Quaternion.Euler(0, -90, 0); break; // 2 -> 5'in rotasyonu
            case 3: targetRotation = Quaternion.Euler(-90, 0, 0); break; // 3 düzgün
            case 4: targetRotation = Quaternion.Euler(90, 0, 0); break; // 4 düzgün
            case 5: targetRotation = Quaternion.Euler(0, 90, 0); break; // 5 -> 2'nin rotasyonu
            case 6: targetRotation = Quaternion.Euler(0, 0, 0); break; // 6 -> 1'in rotasyonu
        }

        Quaternion startRotation = dice3D.rotation;
        float elapsed = 0f;

        while (elapsed < diceRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / diceRotationDuration);
            dice3D.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        dice3D.rotation = targetRotation;
    }

    private IEnumerator PlayAITurn()
    {
        isAIPlaying = true;
        yield return new WaitForSeconds(0.5f); // kısa bekleme

        // Roll dice
        OnRollDiceButtonClicked();
        yield return new WaitForSeconds(1.5f); // zar sonucunu göster ve dice animasyonu için

        // movable pawns varsa seç ve hamle yap
        if (movablePawns.Count > 0)
        {
            // rastgele bir movable pawn seç
            int randomIndex = UnityEngine.Random.Range(0, movablePawns.Count);
            selectedPawnIndex = randomIndex;
            
            // select görsel gösterimi için
            OnSelectPawnButtonClicked();
            yield return new WaitForSeconds(0.5f);

            // hamleyi yap
            OnMakeAMoveButtonClicked();
            
            // Wait for movement to complete
            while (isMoving)
            {
                yield return null;
            }
            
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            // No movable pawns, wait for auto-pass to complete
            yield return new WaitForSeconds(1.5f);
        }

        // Reset AI flag
        isAIPlaying = false;
        
        // Check if next turn is also AI (not Blue), if so continue playing
        if (turnOrder[currentTurnIndex] != PlayerColor.Blue)
        {
            StartCoroutine(PlayAITurn());
        }
    }

    private IEnumerator FloatPawn(Pawn pawn)
    {
        if (pawn == null) yield break;
        floatingStartPos = pawn.transform.position;
        float timeOffset = 0f;

        while (currentFloatingPawn == pawn)
        {
            timeOffset += Time.deltaTime * pawnFloatSpeed;
            float yOffset = Mathf.Sin(timeOffset) * pawnFloatHeight;
            pawn.transform.position = floatingStartPos + Vector3.up * yOffset;
            yield return null;
        }
    }

    private void StopFloatingAnimation()
    {
        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
            floatingCoroutine = null;
        }

        if (currentFloatingPawn != null)
        {
            // reset pawn to start position before clearing reference
            currentFloatingPawn.transform.position = floatingStartPos;
            currentFloatingPawn = null;
        }
    }

    private bool CanPawnMoveWithRoll(Pawn p, int roll)
    {
        int homeLen = 4;

        if (p.IsAtHome)
        {
            return roll == 6;
        }

        if (p.IsOnMain)
        {
            int homeEntry = GetHomeEntryIndex(p.Color);
            int distanceToEntry = (homeEntry - p.MainIndex + 40) % 40;
            if (roll <= distanceToEntry) return true;
            int remaining = roll - distanceToEntry;
            if (remaining - 1 < homeLen) return true;
            return false;
        }

        if (p.IsOnHome)
        {
            return p.HomeIndex + roll < homeLen;
        }

        return false;
    }
    #endregion

    #region Pawn Interaction & Movement
    public void OnPawnClicked(Pawn pawn)
    {
        // pawn clicking disabled in new 3-button system
        // user must use Select A Pawn button instead
        Debug.Log("Use the Select A Pawn button to select pawns.");
    }

    private IEnumerator TryMovePawn(Pawn pawn, int roll)
    {
        // prevent overlapping moves
        if (isMoving) yield break;
        isMoving = true;
        currentRoll = roll; // just to keep the value during coroutine

        try
        {

        // At home
        if (pawn.IsAtHome)
        {
            if (roll == 6)
            {
                int startIndex = GetStartMainIndex(pawn.Color);
                Transform dest = boardTiles != null && boardTiles.Length > startIndex ? boardTiles[startIndex] : null;
                if (dest != null)
                {
                    yield return StartCoroutine(MovePawnStep(pawn, dest));
                    pawn.SetOnMain(startIndex, 0);
                    HandleCaptureAtMain(startIndex, pawn);
                    AfterMoveConsumeRoll(roll);
                }
            }
            else
            {
                Debug.Log("Need a 6 to leave home.");
            }

            yield break;
        }

        // On main path
        if (pawn.IsOnMain)
        {
            // determine home entry as the tile BEFORE the start tile
            int homeEntry = GetHomeEntryIndex(pawn.Color);
            int distanceToEntry = (homeEntry - pawn.MainIndex + 40) % 40; // steps needed to reach the homeEntry tile

            if (roll <= distanceToEntry)
            {
                // move only on main path
                for (int s = 1; s <= roll; s++)
                {
                    int nextIndex = (pawn.MainIndex + 1) % 40;
                    Transform dest = boardTiles[nextIndex];
                    yield return StartCoroutine(MovePawnStep(pawn, dest));
                    pawn.MainIndex = nextIndex;
                    pawn.StepsSinceStart += 1;
                }

                HandleCaptureAtMain(pawn.MainIndex, pawn);
                AfterMoveConsumeRoll(roll);
            }
            else
            {
                // will enter home run
                int stepsToEntry = distanceToEntry; // number of steps to reach homeEntry
                // move to entry first
                for (int s = 0; s < stepsToEntry; s++)
                {
                    int nextIndex = (pawn.MainIndex + 1) % 40;
                    Transform dest = boardTiles[nextIndex];
                    yield return StartCoroutine(MovePawnStep(pawn, dest));
                    pawn.MainIndex = nextIndex;
                    pawn.StepsSinceStart += 1;
                }

                int remaining = roll - stepsToEntry; // remaining steps after reaching entry tile
                // now remaining >=1 means we step into home tiles: remaining==1 -> homeIndex 0
                int homeStart = GetHomeRunStartIndex(pawn.Color);
                int targetHomeIndex = remaining - 1; // 0-based
                if (targetHomeIndex < 4)
                {
                    Transform homeDest = boardTiles[homeStart + targetHomeIndex];
                    yield return StartCoroutine(MovePawnStep(pawn, homeDest));
                    pawn.SetOnHome(targetHomeIndex);
                    AfterMoveConsumeRoll(roll);
                }
                else
                {
                    Debug.Log("Can't move: roll would overshoot home.");
                }
            }

            yield break;
        }

        // On home path

        // On home path
        if (pawn.IsOnHome)
        {
            int targetHome = pawn.HomeIndex + roll;
            int homeStart = GetHomeRunStartIndex(pawn.Color);
            if (targetHome < 4)
            {
                for (int s = 1; s <= roll; s++)
                {
                    int nextHome = pawn.HomeIndex + 1;
                    Transform dest = boardTiles[homeStart + nextHome];
                    yield return StartCoroutine(MovePawnStep(pawn, dest));
                    pawn.HomeIndex = nextHome;
                }

                if (pawn.HomeIndex == 3)
                {
                    pawn.Finish();
                    Debug.Log($"Pawn finished: {pawn.Color}#{pawn.LocalIndex}");
                }

                AfterMoveConsumeRoll(roll);
            }
            else
            {
                Debug.Log("Can't move: would overshoot final home tile.");
            }

            yield break;
        }

        }
        finally
        {
            isMoving = false;
            UpdateButtonStates();
        }
    }

    private IEnumerator MovePawnStep(Pawn pawn, Transform dest)
    {
        if (pawn == null || dest == null)
            yield break;

        float duration = 0.25f; // per step
        Vector3 start = pawn.transform.position;
        Vector3 end = dest.position;
        float elapsed = 0f;

        // detach from parent while moving
        pawn.transform.SetParent(null, true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // arc using sine
            float yOffset = Mathf.Sin(t * Mathf.PI) * pawnJumpHeight;
            pawn.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            yield return null;
        }

        pawn.transform.position = end;
        
        // Set global Y position to -0.010439 when on tiles (not at spawn points)
        Vector3 finalPos = pawn.transform.position;
        finalPos.y = -0.00799f;
        pawn.transform.position = finalPos;
        
        yield return null;
    }

    private void HandleCaptureAtMain(int mainIndex, Pawn mover)
    {
        if (safeMainIndices.Contains(mainIndex)) return;

        foreach (var p in allPawns)
        {
            if (p == mover) continue;
            if (p.IsOnMain && p.MainIndex == mainIndex && p.Color != mover.Color)
            {
                // send p back to its spawn (first free slot for its color)
                int spawnIdx = GetFreeSpawnIndexForColor(p.Color);
                Transform spawn = spawnIdx >= 0 && spawnIdx < spawnSlots.Length ? spawnSlots[spawnIdx] : null;
                if (spawn != null)
                {
                    p.SendHome(spawn.position);
                    Debug.Log($"Captured pawn {p.Color}#{p.LocalIndex} -> sent home.");
                }
            }
        }
    }

    private int GetFreeSpawnIndexForColor(PlayerColor color)
    {
        int baseIdx = GetSpawnBaseIndex(color);
        for (int i = 0; i < 4; i++)
        {
            int idx = baseIdx + i;
            bool occupied = false;
            foreach (var p in allPawns)
            {
                if (p.IsAtHome && p.SpawnSlotIndex == idx) { occupied = true; break; }
            }
            if (!occupied) return idx;
        }
        return baseIdx; // fallback
    }

    private int GetSpawnBaseIndex(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Blue: return 0;
            case PlayerColor.Green: return 4;
            case PlayerColor.Yellow: return 8;
            case PlayerColor.Red: return 12;
        }
        return 0;
    }

    private int GetStartMainIndex(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Blue: return 0;   // tile 1 in user's numbering
            case PlayerColor.Green: return 10; // tile 11
            case PlayerColor.Yellow: return 20; // tile 21
            case PlayerColor.Red: return 30;   // tile 31
        }
        return 0;
    }

    private int GetHomeRunStartIndex(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Blue: return 40;
            case PlayerColor.Green: return 44;
            case PlayerColor.Yellow: return 48;
            case PlayerColor.Red: return 52;
        }
        return 40;
    }

    private int GetHomeEntryIndex(PlayerColor color)
    {
        // home entry is the tile before the player's start tile
        int start = GetStartMainIndex(color);
        return (start - 1 + 40) % 40;
    }

    private bool HasAnyValidMoveForCurrentPlayer(int roll)
    {
        PlayerColor current = turnOrder[currentTurnIndex];
        int homeLen = 4; // number of home run tiles per color

        foreach (var p in allPawns)
        {
            if (p.Color != current) continue;
            if (p.IsAtHome)
            {
                if (roll == 6) return true; // can leave home
                continue;
            }

            if (p.IsOnMain)
            {
                int homeEntry = GetHomeEntryIndex(p.Color);
                int distanceToEntry = (homeEntry - p.MainIndex + 40) % 40;
                if (roll <= distanceToEntry) return true; // simple main move
                int remaining = roll - distanceToEntry;
                if (remaining - 1 < homeLen) return true; // can enter home without overshoot
                continue;
            }

            if (p.IsOnHome)
            {
                if (p.HomeIndex + roll < homeLen) return true;
            }
        }

        return false;
    }

    private void AfterMoveConsumeRoll(int usedRoll)
    {
        bool extraTurn = usedRoll == 6;
        currentRoll = 0;
        movablePawns.Clear();
        selectedPawnIndex = -1;
        StopFloatingAnimation();

        if (!extraTurn)
        {
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Length;
        }
        UpdateRollText(0);
        UpdateTurnText();
        UpdateButtonStates();
    }

    private Color GetUnityColor(PlayerColor c)
    {
        switch (c)
        {
            case PlayerColor.Blue: return Color.cyan; // use cyan for visibility
            case PlayerColor.Green: return Color.green;
            case PlayerColor.Yellow: return Color.yellow;
            case PlayerColor.Red: return Color.red;
        }
        return Color.white;
    }

    private void Update()
    {
        // Debug helper: Raycast on left click and log hit target to diagnose click problems
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.Log("No main camera found for raycast debug.");
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var go = hit.collider.gameObject;
                var pawn = go.GetComponent<Pawn>();
                Debug.Log($"Raycast hit: {go.name}, hasPawn={(pawn!=null)}, collider={hit.collider.GetType().Name}");
            }
            else
            {
                // nothing hit in 3D
                Debug.Log("Raycast hit nothing.");
            }
        }
    }

    #endregion
}
