
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HexaFall.Basics;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;


public class User : MonoBehaviour
{
    [SerializeField]
    private Text moveCounterText;

    [SerializeField]
    private Text userScoreText;

    [SerializeField]
    private Text bestScoreText;

    [SerializeField]
    private Text userLevelText;

    [SerializeField]
    private bool readSavedData = true;  // oyuncu kaldığı yerden devam etsin

    [SerializeField, Range(3, 15)]
    private int nHorizontalHexagons = 8;

    [SerializeField, Range(5, 32)]
    private int nVerticalHexagons = 9;

    [SerializeField, Range(3, 13)]
    private int nDefinedColors = 5;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private GameObject playAgainPanel;

    [SerializeField]
    private Text playAgainMessageText;


    private int moveCounter = 0;
    private int userScore = 0;
    private int userBestScore = 0;
    private int userLevel = 0;


    private readonly string bestScorePrefKey = "BestScore";

    private Coroutine resetGameCoroutine = null;

    public static User Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        HexaFunctions.GameOver += this.GameOverListener;

        StartCoroutine(this.InitializeGame());
    }

    private void OnDestroy()
    {
        HexaFunctions.GameOver -= this.GameOverListener;

    }

    private IEnumerator InitializeGame()
    {
        bool _isDataLoaded = false;

        GameData _data = null;

        if (this.readSavedData)
            _isDataLoaded = this.LoadGame(out _data);

        yield return new WaitForEndOfFrame();

        if (_isDataLoaded)
        {
            this.nVerticalHexagons = _data.nVerticalHexagons;
            this.nHorizontalHexagons = _data.nHorizontalHexagons;
            this.nDefinedColors = _data.nDefinedColors;

        }
           
        HexaMap.Instance.InitialzieMap(this.nVerticalHexagons, this.nHorizontalHexagons, this.nDefinedColors);

        yield return new WaitForEndOfFrame();

        HexaGridSystem.Instance.FillGridSystem(_data?.hexagons);


        // varsa verileri oku
        if (PlayerPrefs.HasKey(this.bestScorePrefKey))
            this.userBestScore = PlayerPrefs.GetInt(this.bestScorePrefKey);


        this.UpdateBoard();

    }


    public bool AddPoint(int _point = 0, bool _isNewMove = false)
    {
        if(_isNewMove)
        {
            this.moveCounter++;
            HexaFunctions.HexagonMoved?.Invoke();
        }
        

        bool _isLevelUpdated = false;

        if(_point > 0)
        {
            Debug.Log("PUAN ALDIN: " + _point.ToString());

            this.userScore += _point;

            if (this.userScore > this.userBestScore)
                this.userBestScore = this.userScore;

            int _level = Mathf.FloorToInt((float) this.userBestScore / HexaFunctions.LevelUpdatePoint);


            if (_level > this.userLevel)
            {
                _isLevelUpdated = true;
                this.userLevel = _level;
            }


        }

        this.UpdateBoard();

        return _isLevelUpdated;

    }

    private void UpdateBoard()
    {
        if (this.moveCounterText != null)
            this.moveCounterText.text = this.moveCounter.ToString();

        if (this.userScoreText != null)
            this.userScoreText.text = this.userScore.ToString();

        if (this.bestScoreText != null)
            this.bestScoreText.text = (this.userBestScore > 0) ? "Best score: " + this.userBestScore.ToString() : "";

        if (this.userLevelText != null)
            this.userLevelText.text = (this.userLevel > 0) ? "Level: " + this.userLevel.ToString() : "";



    }

    private void SaveGameData(List<HexagonData> _hexagons)
    {
        GameData _data = new GameData();

        _data.score = this.userScore;
        _data.moveCount = this.moveCounter;
        _data.nVerticalHexagons = this.nVerticalHexagons;
        _data.nHorizontalHexagons = this.nHorizontalHexagons;
        _data.nDefinedColors = this.nDefinedColors;

        _data.hexagons = new List<HexagonData>();

        for (int i=0; i< _hexagons.Count; i++)
            _data.hexagons.Add(_hexagons[i]);
            
        string _dir = Application.persistentDataPath + Path.DirectorySeparatorChar +
                                "SavedGames";

        //eğer bu kullanıcı için directory yoksa aç
        if (!Directory.Exists(_dir))
            Directory.CreateDirectory(_dir);


        string _path = _dir + Path.DirectorySeparatorChar + "DefaultUser" + ".dat";


        BinaryFormatter bf = new BinaryFormatter();
        FileStream saveFile = File.Create(_path);
        bf.Serialize(saveFile, _data);
        saveFile.Close();


        PlayerPrefs.SetInt(this.bestScorePrefKey, this.userBestScore);

        Debug.Log("Game saved: " + _path);

        if (this.saveButton != null)
            this.saveButton.interactable = true;


    }

    private bool LoadGame(out GameData _data)
    {
        _data = null;

        string _dir = Application.persistentDataPath + Path.DirectorySeparatorChar +
                                "SavedGames";

        string _path = _dir + Path.DirectorySeparatorChar + "DefaultUser" + ".dat";

        if (!File.Exists(_path))
        {
            Debug.Log(_path + " : Dosya bulunamadı");
            return false;
        }

        BinaryFormatter _bf = new BinaryFormatter();
        FileStream _savedFile = File.Open(_path, FileMode.Open);
        GameData _loadedData = (GameData)_bf.Deserialize(_savedFile);
        _savedFile.Close();

        if(_loadedData == null)
            return false;

        if (_loadedData.hexagons == null)
            return false;

        _data = new GameData
        {
            score = _loadedData.score,
            moveCount = _loadedData.moveCount,
            nVerticalHexagons = _loadedData.nVerticalHexagons,
            nHorizontalHexagons = _loadedData.nHorizontalHexagons,
            nDefinedColors = _loadedData.nDefinedColors,

            hexagons = new List<HexagonData>(),
        };


        for (int i = 0; i < _loadedData.hexagons.Count; i++)
            _data.hexagons.Add(_loadedData.hexagons[i]);

        this.userScore = _data.score;
        this.moveCounter = _data.moveCount;

        this.userLevel = Mathf.FloorToInt( (float) _data.score / HexaFunctions.LevelUpdatePoint);

        return true;
    }

    public void SaveGame()
    {
        
        // sol alttan - sağ uste doğru sıralı bir şekilde hexagonları getir
        HexaGridSystem.Instance.GetAllHexagons(out List<HexagonData> _hexagons);

        this.SaveGameData(_hexagons);
    }

    private void GameOverListener(string _message)
    {
        UserInputs.Instance.StopCurrentGame();

        if (this.playAgainMessageText != null)
            this.playAgainMessageText.text = _message;

        if (this.playAgainPanel != null)
            this.playAgainPanel.SetActive(true);
    }

    public void ResetGame()
    {
        if (this.resetGameCoroutine != null)
            return;

        this.resetGameCoroutine = StartCoroutine(this.ResetGameCoroutine());
    }

    private IEnumerator ResetGameCoroutine()
    {
        UserInputs.Instance.StopCurrentGame();

        this.moveCounter = 0;
        this.userScore = 0;
        this.userBestScore = 0;
        this.userLevel = 0;

        this.UpdateBoard();


        HexaFunctions.KillAllHexagons?.Invoke();

        yield return new WaitForSeconds(1.33f);

        HexaGridSystem.Instance.FillGridSystem();


        this.resetGameCoroutine = null;
    }


}
