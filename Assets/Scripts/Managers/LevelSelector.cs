using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public struct LevelData
{
    public int key;
    public string name;
    public GameObject btn;
    public int height;
}

public class LevelSelector : MonoBehaviour
{
    public static LevelSelector instance;

    private void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        levelList = new Dictionary<int, string>()
        {
            {1, "Enemy Level" },
            {2, "Enemy Level" },
            {3, "Enemy Level" },
            {4, "Card Shop" },
            {5, "Card Shop"},
            {6, "Mask Level" },
            {7, "Mask Level" }
        };

        map = new Dictionary<int, List<LevelData>>();
        completedLevels = new List<int>();

        LoadRandomOrder(levelList);
        Debug.Log(string.Join(",", randomLevels));

        currentLevelIndex = 0;
        maxheight = 0;
        UpdateLevelSelector(btnList, map);

    }



    private Dictionary<int, string> levelList;

    [SerializeField] private GameObject[] btnList;

    private int[] randomLevels;

    /// <summary>
    /// int is level Index, List is available levels for that index
    /// </summary>
    private Dictionary<int, List<LevelData> > map;
    private int currentLevelIndex;
    private List<int> completedLevels; // keys, not indexes
    private int maxheight;
    [SerializeField] private GameObject levelCanvas;

    // Start is called before the first frame update
    void Start()
    {
       

    }

    /// <summary>
    /// Sets the active state of the levelCanvas object.
    /// </summary>
    /// <param name="toggle">If true, makes the levelCanvas visible; otherwise, hides it.</param>
    public void ToggleVisibility( bool toggle)
    {
        //This is for game manager to call when loading level selector scene after resetting
        levelCanvas.SetActive(toggle);
    }

    // if using singleton
    public void ResetLevelSelector()
    {
        //initialize/reset level list

        if (map != null)
        {
            map.Clear();
        }
        if (randomLevels != null)
        {
            randomLevels = null;
        }
        if (completedLevels != null)
        {
            completedLevels.Clear();
        }

        currentLevelIndex = 1;
        maxheight = 0;

        map = new Dictionary<int, List<LevelData>>();

        LoadRandomOrder(levelList);
        UpdateLevelSelector(btnList, map);
        
    }

    /// <summary>
    /// Update current level index and update level selector UI
    /// </summary>
    public void LevelCompleted()
    {
        //enable ui
        levelCanvas.SetActive(true);

        // get max height
        if (currentLevelIndex == 6)
        {
            GameManager.instance.WinScreen();
        }
        else
        {
            maxheight = map[currentLevelIndex][0].height + 1;
            Debug.Log(maxheight);
            completedLevels.Add(randomLevels[currentLevelIndex]);

            UpdateLevelSelector(btnList, map);
        }
            
    }

    
    /// <summary>
    /// Generates a random order of level keys from the provided dictionary, ensuring specific keys are placed at
    /// designated positions, and updates the map accordingly.
    /// </summary>
    /// <param name="dict">A dictionary mapping level indices to their corresponding names.</param>
    private void LoadRandomOrder(Dictionary<int, string> dict)
    {

        int count = dict.Count;
        List<int> keys = new List<int>(dict.Keys);
        int[] randomOrder = new int[count];
        System.Random rand = new System.Random();

        int lastBossKey = keys[UnityEngine.Random.Range(0, 2)];
        int lastShopKey = keys[UnityEngine.Random.Range(3, 6)];

        keys.RemoveAt(keys.IndexOf(lastBossKey));
        keys.RemoveAt(keys.IndexOf(lastShopKey));

        count -= 2;

        for (int i = 0; i < count; i++)
        {
            int randomIndex = rand.Next(keys.Count);
            randomOrder[i] = keys[randomIndex];
            keys.RemoveAt(randomIndex);
        }

        randomOrder[5] = lastShopKey;
        randomOrder[6] = lastBossKey;

        randomLevels = randomOrder;

        CreateMap(btnList, randomLevels);

    }

    /// <summary>
    /// Initializes the level selection map by assigning level names, configuring button states and click events, and
    /// mapping level progression based on the provided level order.
    /// </summary>
    /// <param name="sList">An array of GameObjects representing the level selection buttons.</param>
    /// <param name="randLevels">An array of integers specifying the randomized order of level keys.</param>
    private void CreateMap(GameObject[] sList, int[] randLevels)
    {
        TMPro.TextMeshProUGUI textComponent;

        for (int i = 0; i < sList.Length; i++)
        {
            GameObject btn = sList[i];
            textComponent = sList[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();

            int levelKey = randLevels[i];

            string levelName = levelList[levelKey]; //the level name

            if (textComponent != null && levelList.ContainsKey(levelKey) )
            {

                textComponent.text = levelName;
                btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>SelectLevel(levelKey) );

                if (i != 0)
                {
                    ColorBlock cb = btn.GetComponent<UnityEngine.UI.Button>().colors;
                    cb.disabledColor = new Color(0.404f, 0.247f, 0.247f);
                    btn.GetComponent<UnityEngine.UI.Button>().colors = cb;
                    btn.GetComponent<UnityEngine.UI.Button>().interactable = false; //disable all buttons at start except first

                }

                List<LevelData> dataList = new List<LevelData>();

                switch (i)
                {
                    case 0: //1 => go to 2 or 3
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[i+1],
                            name = levelList[randLevels[i + 1] ],
                            btn = sList[i + 1],
                            height = 1
                        } );

                        dataList.Add(new LevelData()
                        {
                            key = randLevels[i + 2],
                            name = levelList[randLevels[i + 2]],
                            btn = sList[i + 2],
                            height = 1
                        });

                        break;

                    case 1: //2 => go to 4 or 5
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[3],
                            name = levelList[randLevels[3]],
                            btn = sList[3],
                            height =2
                        });

                        dataList.Add(new LevelData()
                        {
                            key = randLevels[4],
                            name = levelList[randLevels[4]],
                            btn = sList[4],
                            height =2
                        });
                        break;

                    case 2: //3 => go to 5
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[4],
                            name = levelList[randLevels[4]],
                            btn = sList[4],
                            height = 2
                        });
                        break;

                    case 3: //4 => go to 6
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[5],
                            name = levelList[randLevels[5]],
                            btn = sList[5],
                            height = 3
                        });
                        break;

                    case 4: //5 => go to 6
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[5],
                            name = levelList[randLevels[5]],
                            btn = sList[5],
                            height = 3
                        });
                        break;

                    case 5: //6 => go to 7
                        dataList.Add(new LevelData()
                        {
                            key = randLevels[6],
                            name = levelList[randLevels[6]],
                            btn = sList[6],
                            height = 4,
                        });
                        break;

                    case 6: //7
                        // well, nothing after this
                        break;
                }

                map.Add(i, dataList);
            }
        }
    }

    /// <summary>
    /// This only gets called after winning a level or at start/after each level
    /// </summary>
    /// <param name="btnList"></param>
    /// <param name="m"></param>
    private void UpdateLevelSelector(GameObject[] btnList, Dictionary<int, List<LevelData>> m)
    {
        // if not the frist level
        //it means you should've won the first/previous level

        Debug.Log(btnList);

        

        //** currentLevelIndex is updated should be updated at this point already unless it's first level/START **//
        if (maxheight != 0)
        {
            Debug.Log("Current Level: " + currentLevelIndex);
            //disbale previous (already won) level button (you already won it)


            //for (int i = 0; i < completedLevels.Count; i++)
            //{
            //    GameObject btn = btnList[completedLevels[i] -1];
            //    if (btn.activeSelf)
            //    {
            //        btn.GetComponent<UnityEngine.UI.Button>().interactable = false;
            //    }
            //}



            //disable all completed levels 
            //all add to the list

            //include first level too
            GameObject firstBtn = btnList[currentLevelIndex];
            ColorBlock firstCb = firstBtn.GetComponent<UnityEngine.UI.Button>().colors;
            firstCb.disabledColor = Color.gray;
            firstBtn.GetComponent<UnityEngine.UI.Button>().colors = firstCb;
            firstBtn.GetComponent<UnityEngine.UI.Button>().interactable = false;

            for (int i = 0; i <= currentLevelIndex; i++)
            {

                //I want to make completed levels a different color, but it is what it is for now

                List<LevelData> levelDatas = m[i];
                Debug.Log(levelDatas);
                foreach(LevelData ld in levelDatas)
                {
                    GameObject tempBtn = ld.btn;
                    ColorBlock cb = tempBtn.GetComponent<UnityEngine.UI.Button>().colors;
                    //if (ld.height < maxheight )
                    //{
                    //    cb.disabledColor = Color.green;
                    //}
                    //else
                    //{
                    //    cb.disabledColor = Color.gray;
                    //}

                    cb.disabledColor = Color.gray;
                    tempBtn.GetComponent<UnityEngine.UI.Button>().colors = cb;
                    tempBtn.GetComponent<UnityEngine.UI.Button>().interactable = false;
                }
            }

            


            //enable only the available next levels
            //get previous level index's available levels as current available levels to go to
            List<LevelData> availableLevels = m[currentLevelIndex];
            foreach (LevelData ld in availableLevels)
            {
                ld.btn.GetComponent<UnityEngine.UI.Button>().interactable = true;
            }
        }
        else if(maxheight == 0)
        {
            Debug.Log("First Level Start");
            //enable only the first level button
            btnList[0].GetComponent<UnityEngine.UI.Button>().interactable = true;





        }
    }


    /// <summary>
    /// Loads the specified level by key, updates the current level index, hides the level selector UI, and transitions
    /// to the selected scene.
    /// </summary>
    /// <param name="levelKey">The key identifying the level to load.</param>
    public void SelectLevel(int levelKey)
    {
        //note:
        //You don't really need to change this


        string levelName = levelList[levelKey];

        currentLevelIndex = randomLevels.ToList().IndexOf(levelKey);

        Debug.Log("Loading Level: " + levelName + " -- LevelIndex: " + currentLevelIndex );


        //Debug.Log("Updated Current Level Index to: " + currentLevelIndex);

        //UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
        string name = "Level " + (currentLevelIndex+1);


        //hide level selector UI because this is a singleton
        levelCanvas.SetActive(false);

        SceneManager.LoadScene(name);
    }
}
