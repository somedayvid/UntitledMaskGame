using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempLevelManager : MonoBehaviour
{
    [SerializeField] private GameManager levelSelector;
    // Start is called before the first frame update
    void Start()
    {
        levelSelector = GameManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Lose()
    {
        GameManager.instance.GameOver();
    }

    public void Win()
    {
        GameManager.instance.LevelWon();
    }

    
}
