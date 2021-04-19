using UnityEngine;
using HexaFall.Basics;

public class BombHexagon : HexagonBasics
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

    protected override void SetHexaParameter(int _parameter1)
    {
        this.InitializeBomb(_parameter1);
    }

    protected override int  GetHexaParameter()
    {
        return this.bombCounter;
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
