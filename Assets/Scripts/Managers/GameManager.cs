using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

enum GameState
{
    MainMenu,
    LevelSelector,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        //LevelSelector.instance.ResetLevelSelector();
        

        //LevelList = new Dictionary<int, string>()
        //{
        //    {1, "Enemy Level" },
        //    {2, "Enemy Level" },
        //    {3, "Enemy Level" },
        //    {4, "Card Shop" },
        //    {5, "Card Shop"},
        //    {6, "Mask Level" },
        //    {7, "Mask Level" }
        //};

        //RandomLevels = new int[LevelList.Count];

        

        //levelSelector.LoadRandomOrder(LevelList, RandomLevels, Map);
    }



    // variables
    //[SerializeField]private LevelSelector levelSelector;

    //public Dictionary<int, string> LevelList { get; set; }


    
    //public int[] RandomLevels { get; set; }

    //public Dictionary<int, List<LevelData>> Map { get; set; }



    // Start is called before the first frame update
    void Start()
    {
        
    }


    
    public void MainMenu()
    {
        Debug.Log("Loading Main Menu...");
        SceneManager.LoadScene("StartMenu");

           
    }

    public void StartTutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }


    public void StartGame()
    {
        Debug.Log("Loading Game...");
        SceneManager.LoadScene("LevelSelector");

        if (lvlSelectExists())
        {
            LevelSelector.instance.ToggleVisibility(true);
            //LevelSelector.instance.ResetLevelSelector();
        }

    }

    //do not use currently unless you want to reset everything
    public void Restart()
    {
        Debug.Log("Restarting...");
        LevelSelector.instance.ResetLevelSelector();
        LevelSelector.instance.ToggleVisibility(false);
        MainMenu();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }


    public void Quit()
    {
        Debug.Log("Quitting...");

        #if UNITY_EDITOR
                // Exit play mode in the Unity Editor
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                // Quit the application in a built game
                Application.Quit();
        #endif

        //Application.Quit();
    }

    public void GameOver()
    {
        Debug.Log("Game Over!");
        SceneManager.LoadScene("GameOver");

        LevelSelector.instance.ToggleVisibility(false);
        LevelSelector.instance.ResetLevelSelector();


    }

    public void WinScreen()
    {
        SceneManager.LoadScene("VictoryScene");

        LevelSelector.instance.ToggleVisibility(false);
        LevelSelector.instance.ResetLevelSelector(); //adjust as needed.
    }

    public void LevelWon()
    {
        Debug.Log("Level Won!");
        SceneManager.LoadScene("LevelSelector");
        LevelSelector.instance.LevelCompleted();
        
    }

    private bool lvlSelectExists()
    {
        return LevelSelector.instance != null;
    }
}
