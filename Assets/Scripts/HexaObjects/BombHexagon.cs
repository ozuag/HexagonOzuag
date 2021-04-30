using UnityEngine;
using HexaFall.Basics;

public class BombHexagon : ColorHexagon, IBombHexagon
{
    public override HexaType HexagonType => HexaType.BombHexagon;

    [SerializeField]
    private SpriteRenderer counterSpriteRenderer;

    [SerializeField]
    private Sprite[] countdownSprites;

    public int BombCounter {get; private set; } = 13;

    protected override void OnEnable()
    {
        base.OnEnable();
        HexaFunctions.HexagonMoved += this.MoveListener;

    }

    protected override void OnDisable()
    {
        base.OnDisable();
        HexaFunctions.HexagonMoved -= this.MoveListener;

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        HexaFunctions.HexagonMoved -= this.MoveListener;

    }

    public override HexagonData GetHexagonData()
    {
        return new HexagonData((int) this.HexagonType, this.ColorId, this.BombCounter);
    }

    public override void SetHexagonData(HexagonData _data)
    {
        this.SetColor(_data.colorId);
        this.InitializeBomb(_data.parameter1);
    }

    private void InitializeBomb(int _counter = -1)
    {

        if (_counter < 0)
        {
            if (this.countdownSprites != null)
                this.BombCounter = (this.countdownSprites.Length - 1);
        }
        else
        {
            this.BombCounter = _counter;
        }


        this.UpdateCounterSprite();

    }

    private void MoveListener()
    {
        this.CountDown();
    }

    private void CountDown()
    {
        this.BombCounter--;

        if(this.BombCounter < 0)
        {
            Debug.Log("BOMBAA PATLADI");
            HexaFunctions.KillAllHexagons?.Invoke();

            HexaFunctions.GameOver?.Invoke("Bomba patladı, oyun bitti");

            Debug.Log("********GAME OVER*********");
        }
        else
        {
            this.UpdateCounterSprite();
        }
    }

    private void UpdateCounterSprite()
    {
        if ((this.countdownSprites != null) & (this.counterSpriteRenderer != null))
        {
            if (this.BombCounter >= this.countdownSprites.Length)
                this.BombCounter = this.countdownSprites.Length - 1;

            this.counterSpriteRenderer.sprite = this.countdownSprites[this.BombCounter];

        }
    }

}
