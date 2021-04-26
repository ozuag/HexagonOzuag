using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexaFall.Basics;

public class HexaMap : MonoBehaviour
{
    public GameObject vertexmaster;

    [SerializeField]
    private RectTransform hexaGridSystem;

    private int maxHexagonCellWidth = 256;

    private RectTransform hexaMapRect;

    public static HexaMap Instance { get; private set; }

    #region Singleton
    private void Awake()
    {
        Instance = this;
    }
    #endregion


    public void InitialzieMap(int _nVertical, int _nHorizontal)
    {
        this.hexaMapRect = this.GetComponent<RectTransform>();

        // sahnenin ekran boyutuna gore olceklenmesini bekle

        float _mapRectWidth = Screen.width;
        float _mapRectHeight = Screen.height;

        // bunları prefab'dan al
        float _hexagonObjectWidth = 128f;
        float _hexagonObjectHeight = 110.8512f;

        if (this.hexaMapRect != null)
        {
            _mapRectWidth = this.hexaMapRect.rect.width;
            _mapRectHeight = this.hexaMapRect.rect.height;
        }

        // istenen sayı kadar hexagon yerleştirirsen ihityaç duyacağın alan
        float _requiredRectWidth = ((3f*_nHorizontal + 1f)/4f) * _hexagonObjectWidth;
        float _requiredRectHeight = ( (_nVertical + 1f) / 2f ) * _hexagonObjectHeight;


        float _tempMapScaleX = _mapRectWidth / _requiredRectWidth;
        float _tempMapScaleY = _mapRectHeight / _requiredRectHeight;

        float _mapScale = (_tempMapScaleX < _tempMapScaleY) ? _tempMapScaleX : _tempMapScaleY;

        if ( (_mapScale * _hexagonObjectWidth) > this.maxHexagonCellWidth)
            _mapScale = this.maxHexagonCellWidth / _hexagonObjectWidth;

       
        HexaGridSystem.Instance.SetGridSystem(_nVertical, _nHorizontal, _hexagonObjectWidth, _hexagonObjectHeight, _requiredRectWidth, _requiredRectHeight, _mapScale);

    }


   
   

}
