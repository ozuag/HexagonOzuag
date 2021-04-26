using UnityEngine;

public class HexaMap : MonoBehaviour
{
    public GameObject vertexmaster;

    [SerializeField]
    private RectTransform hexaGridSystem;

    private int maxHexagonCellWidth = 256;

    public static HexaMap Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void InitialzieMap(int _nVertical, int _nHorizontal)
    {

        float _mapRectWidth = Screen.width;
        float _mapRectHeight = Screen.height;

        // bunları prefab'dan al
        float _hexagonObjectWidth = 128f;
        float _hexagonObjectHeight = 110.8512f;

        RectTransform _hexaMapRect = this.GetComponent<RectTransform>();
        if (_hexaMapRect != null)
        {
            _mapRectWidth = _hexaMapRect.rect.width;
            _mapRectHeight = _hexaMapRect.rect.height;
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
