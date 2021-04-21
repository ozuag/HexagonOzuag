using UnityEngine;
using HexaFall.Basics;

public class BombHexagon : ColorHexagon, IBombHexagon
{

    [SerializeField]
    private SpriteRenderer counterSpriteRenderer;

    [SerializeField]
    private Sprite[] countdownSprites;

    private int bombCounter = 13;



    protected override void HexaEnabled()
    {
        Debug.Log("SAHNEDE BOMBA VAR");

        HexaFunctions.HexagonMoved += this.MoveListener;

    }

    protected override void HexaDisabled()
    {
        HexaFunctions.HexagonMoved -= this.MoveListener;

    }

    protected override void HexaDestroyer()
    {
        HexaFunctions.HexagonMoved -= this.MoveListener;

    }


    public override HexagonData GetHexagonData()
    {
        return new HexagonData((int)this.hexaType, this.colorId, this.bombCounter);
    }

    public override void SetHexagonData(HexagonData _data)
    {
        this.SetColor(_data.colorId);
        this.SetParameter(_data.parameter1);
    }

    private void InitializeBomb(int _counter = -1)
    {

        if (_counter < 0)
        {
            if (this.countdownSprites != null)
                this.bombCounter = (this.countdownSprites.Length - 1);
        }
        else
        {
            this.bombCounter = _counter;
        }


        this.UpdateCounterSprite();

    }



    public void SetParameter(int _par1 = -666)
    {
        this.InitializeBomb(_par1);
    }

    public int GetParameter()
    {
        return this.bombCounter;
    }

    private void MoveListener()
    {

        this.CountDown();
    }


    private void CountDown()
    {
        this.bombCounter--;

        if(this.bombCounter < 0)
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
            if (this.bombCounter >= this.countdownSprites.Length)
                this.bombCounter = this.countdownSprites.Length - 1;

            this.counterSpriteRenderer.sprite = this.countdownSprites[this.bombCounter];

        }
    }

}
