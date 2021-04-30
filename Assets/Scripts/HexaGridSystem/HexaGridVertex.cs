using UnityEngine;
using HexaFall.Basics;


// ECS İLE YAPMALI
public class HexaGridVertex : MonoBehaviour
{
    [SerializeField]
    RectTransform rectTransfom;

    [SerializeField]
    private Vector2Int gridIndex;

    [SerializeField]
    private bool isEmpty = true;

    public bool IsEmpty { get { return this.isEmpty; } }

    public Vector2Int SetGridIndex { get { return gridIndex; } }

    public GameObject PlacedObject { get {

            if(this.transform.childCount > 0)
                return this.transform.GetChild(0).gameObject;

            return null;
        } }

    public void SetMapPosition(int _rowIndex, int _columnIndex, float _hStepSize, float _hOffSet, float _vStepSize ,float _vOffSet)
    {
        if (this.rectTransfom == null)
            return;

        this.gridIndex.x = _columnIndex;
        this.gridIndex.y = _rowIndex;

        this.name = "GridVertex_" + this.gridIndex.x.ToString() + "_" + this.gridIndex.y.ToString();


        this.rectTransfom.anchoredPosition = new Vector2((_columnIndex * _hStepSize) + _hOffSet + _vOffSet, (_rowIndex+1)* _vStepSize);

    }

    public bool PlaceObject(GameObject _object, bool _animate = false)
    {
        
        if (_object == null)
            return false;

        _object.transform.SetParent(this.transform);
        _object.transform.localScale = Vector3.one;

        if(_animate == false)
        {
            _object.transform.localPosition = Vector3.zero;
            _object.transform.localScale = Vector3.one;
        }
        else
        {
            ColorHexagon _hex = _object.GetComponent<ColorHexagon>();
            if (_hex != null)
                _hex.MoveTo(this.transform.position, 6.66f);
        }

        this.isEmpty = false;

        return true;
    }

    public GameObject ReleaseGridObject()
    {
        GameObject _go = null;

        if (this.transform.childCount > 0)
        {
            Transform _child = this.transform.GetChild(0);

            if(_child != null)
            {
                _go = _child.gameObject;
                //_child.SetParent(null, true);
            }
                
        }

        this.isEmpty = true;
        return _go;
    }


    public HexagonData GetHexaData() => this.GetComponentInChildren<HexagonBasics>()?.GetHexagonData();
    
}
