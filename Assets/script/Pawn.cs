using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    public PlayerColor Color { get; private set; }
    public int LocalIndex { get; private set; } // 0..3 for each color

    // spawn info
    public int SpawnSlotIndex { get; private set; } = -1;

    // state
    public bool IsAtHome { get; private set; } = true;
    public bool IsOnMain { get; private set; } = false;
    public bool IsOnHome { get; private set; } = false;

    // main path info
    public int MainIndex { get; set; } = -1; // 0..39
    public int StepsSinceStart { get; set; } = 0; // number of steps taken on main path

    // home path info
    public int HomeIndex { get; set; } = -1; // 0..3 where 3 is final

    public void Initialize(PlayerColor color, int localIndex)
    {
        this.Color = color;
        this.LocalIndex = localIndex;
        this.IsAtHome = true;
        this.IsOnMain = false;
        this.IsOnHome = false;
        this.MainIndex = -1;
        this.HomeIndex = -1;
        this.StepsSinceStart = 0;
    }

    // allow GameManager to record which spawn slot index this pawn was created at
    public void SetSpawnSlotIndex(int idx)
    {
        SpawnSlotIndex = idx;
    }

    private void OnMouseDown()
    {
        // forward to gamemanager
        if (GameManager.Instance != null)
        {
            Debug.Log($"Pawn clicked: {Color}#{LocalIndex} (IsAtHome={IsAtHome}, IsOnMain={IsOnMain}, IsOnHome={IsOnHome})");
            GameManager.Instance.OnPawnClicked(this);
        }
    }

    public void SetOnMain(int mainIndex, int stepsSinceStart)
    {
        IsAtHome = false;
        IsOnHome = false;
        IsOnMain = true;
        MainIndex = mainIndex;
        StepsSinceStart = stepsSinceStart;
        HomeIndex = -1;
    }

    public void SetOnHome(int homeIndex)
    {
        IsAtHome = false;
        IsOnMain = false;
        IsOnHome = true;
        HomeIndex = homeIndex;
    }

    public void SendHome(Vector3 spawnPos)
    {
        StopAllCoroutines();
        transform.position = spawnPos;
        // reset state
        IsAtHome = true;
        IsOnMain = false;
        IsOnHome = false;
        MainIndex = -1;
        StepsSinceStart = 0;
        HomeIndex = -1;
    }

    public void Finish()
    {
        // mark finished - keep at current pos for now
        IsAtHome = false;
        IsOnMain = false;
        IsOnHome = false;
    }
}
