using UnityEngine;
using HexaFall.Basics;

[RequireComponent(typeof(RectTransform))]
public class HexaGridVertex : MonoBehaviour
{
    [SerializeField]
    public bool IsEmpty { get; private set; } = true;

    public GameObject PlacedObject { get {

            if(this.transform.childCount > 0)
                return this.transform.GetChild(0).gameObject;

            return null;
        } }

    public void SetMapPosition(int _rowIndex, int _columnIndex, float _hStepSize, float _hOffSet, float _vStepSize ,float _vOffSet)
    {
        this.name = "GridVertex_" + _columnIndex.ToString() + "_" + _rowIndex.ToString();
        this.GetComponent<RectTransform>().anchoredPosition = new Vector2((_columnIndex * _hStepSize) + _hOffSet + _vOffSet, (_rowIndex+1)* _vStepSize);
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
          _object.GetComponent<HexagonBasics>()?.MoveTo(this.transform.position, 6.66f);
        }

        this.IsEmpty = false;
        return true;
    }

    public GameObject ReleaseGridObject()
    {
        this.IsEmpty = true;
        return this.transform.GetChild(0)?.gameObject;
    }

    public HexagonData GetHexaData() => this.GetComponentInChildren<HexagonBasics>()?.GetHexagonData();
    
}
