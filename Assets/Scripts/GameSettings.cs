using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    private static GameSettings _instance;

    public static GameSettings Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(_instance);
        }
    }

    //private count of AIs when starting the game
    private int _AICount = 0;

    //Keeps the number of AIs within a range of [0,2]
    public int AICount {
        get { return _AICount; }
        set { _AICount = value >= 0 && value <= 2 ? value : _AICount; }
    }
}
