using System.Collections.Generic;
using UnityEngine;
using HexaFall.Basics;

public class HexaGridSystem : MonoBehaviour
{
    [SerializeField]
    private GameObject vertexMaster;

    private RectTransform hexaGridSystem;

    private HexaGridVertex[,] vertices;

    public static HexaGridSystem Instance { private set; get; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        this.vertices = null;
    }

    public void SetGridSystem(int _nRows, int _nColumns, float _cellWidth, float _cellHeight, float _mapWidth, float _mapHeight, float _mapScale = 1f)
    {

        this.hexaGridSystem = this.transform.GetComponent<RectTransform>();

        if (this.hexaGridSystem == null)
            this.hexaGridSystem = this.gameObject.AddComponent<RectTransform>();

        this.hexaGridSystem.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _mapWidth);
        this.hexaGridSystem.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _mapHeight);

        this.hexaGridSystem.localScale = Vector3.one * _mapScale;

        this.vertices = new HexaGridVertex[_nRows, _nColumns];

        bool _isOdd = false;

        float _hOffSet = _cellWidth / 2f;
        float _hStep = 3f * _hOffSet;

        float _vStep = _cellHeight / 2f;
        float _rowIndex;
        float _rowOffSet = 0f;

        for (int i = 0; i < _nRows; i++)
        {
            _isOdd = (i % 2) == 1;

            _rowOffSet = _isOdd ? 0f : (_hStep / 2f);

            _rowIndex = (i + 1) * _vStep;

            for (int j = 0, x = _isOdd ? 0 : 1; x < _nColumns; x += 2, j++)
            {
                GameObject _hexaVertex = GameObject.Instantiate(this.vertexMaster, this.hexaGridSystem);
                _hexaVertex.transform.localScale = Vector3.one;

                this.vertices[i, j] = _hexaVertex.GetComponent<HexaGridVertex>();

                if (this.vertices[i, j] != null)
                    this.vertices[i, j].SetMapPosition(i, j, _hStep, _hOffSet, _vStep, _rowOffSet);

            }
        }

    }

    public void FillGridSystem()
    {
        // rasgele renklerle doldur, satır sayısın tek/çift'e göre renkleri iki fakrlı gruba ayır, oyun kurgulanırken üçleme olmamasını garantile
        List<int> _colorGroup1 = new List<int>();
        List<int> _colorGroup2 = new List<int>();

        int _nRows = this.vertices.GetLength(0);
        int _nColumns = this.vertices.GetLength(1);

        for (int i = 0; i < _nRows; i++)
        {
            bool _isOdd = (i % 2) == 1;

            // her tek satıra geçtiğinde renk gruplarını güncelle
            if (_isOdd == false)
            {
                if (_colorGroup1 != null)
                    _colorGroup1.Clear();

                if (_colorGroup2 != null)
                    _colorGroup2.Clear();

                HexaFunctions.RandomColorGroups(out _colorGroup1, out _colorGroup2);
            }

            for (int j = 0; j < _nColumns; j++)
            {
                if (this.vertices[i, j] == null)
                    continue;

                // boş değilse bir şey yapma
                if (this.vertices[i, j].IsEmpty == false)
                    continue;

                // havuzdan bir hexagon al
                GameObject _go = HexaPooling.Instance.PullObject();

                if (_go != null)
                {
                    _go.SetActive(true);

                    HexagonBasics _hex = _go.GetComponent<HexagonBasics>();

                    if (_hex != null)
                    {
                        int _clrIndex = _isOdd ? _colorGroup1[Random.Range(0, _colorGroup1.Count)] : _colorGroup2[Random.Range(0, _colorGroup2.Count)];

                        // bu hexagona sadece rasgele renk ata 
                        _hex.SetHexagonData(new HexagonData(int.MinValue, _clrIndex, int.MinValue));

                        // oyuna girerken collider'ların aktif olduğunda emin ol
                        _hex.SetEdgeCollidersEnableState(true);
                    }
                }


                if (this.vertices[i, j].PlaceObject(_go) == false)
                    Debug.Log(i.ToString() + "_" + j.ToString() + " -> grid'e nesne yerleştirilemedi ");

            }
        }

    }

    // Sahne yerleşimi dosyadan okunduğunda çağrılacak
    public void FillGridSystem(List<HexagonData> _hexagons)
    {
        if(_hexagons == null)
        {
            this.FillGridSystem();
            return;
        }

        int _nRows = this.vertices.GetLength(0);
        int _nColumns = this.vertices.GetLength(1);

        int _dataIndex = -1;

        for (int i=0; i<_nRows; i++)
        {
            for (int j=0; j<_nColumns; j++)
            {
                _dataIndex++;

                if (this.vertices[i, j] == null)
                    continue;

                // boş değilse bir şey yapma
                if (this.vertices[i, j].IsEmpty == false)
                    continue;

                // tanımlı türde bir hexa getir ve tanımlı rengi 
                GameObject _go = HexaPooling.Instance.PullObject( (HexaType) _hexagons[_dataIndex].type);

                if (_go != null)
                {
                   

                    HexagonBasics _hex = _go.GetComponent<HexagonBasics>();
                    if (_hex != null)
                    {
                        
                        _hex.SetHexagonData(_hexagons[_dataIndex]);

                        _hex.SetEdgeCollidersEnableState(true);
                    }
                    _go.SetActive(true);

                }


                if (this.vertices[i, j].PlaceObject(_go) == false)
                    Debug.Log( i.ToString() + "_" + j.ToString() +  " -> grid'e nesne yerleştirilemedi ");

            }
        }

    }

    // sol-aşağıdan'dan sağ-üste doğru sıra ile tarayarak boş gridleri doldurur.
    public void FillEmptyGridsOnly(bool _useBomb, out HexaGroup _hexagroup)
    {

        _hexagroup = new HexaGroup();

        int _nRows = this.vertices.GetLength(0);
        int _nColumns = this.vertices.GetLength(1);

        for (int i = 0; i < _nRows; i++)
        {
            for (int j = 0; j < _nColumns; j++)
            {
                if (this.vertices[i, j] == null)
                    continue;

                // boş değilse bir şey yapma
                if (this.vertices[i, j].IsEmpty == false)
                    continue;

                GameObject _go = this.GetFromMap(i, j);

                // haritadan nesne bulamazsan pool'dan iste, pool'dan gelenin rengini rasgele seç
                if (_go == null)
                {
                    HexaType _htype = HexaType.NotDefined;
                    if(_useBomb == true)
                    {
                        _htype = HexaType.BombHexagon;
                        _useBomb = false;
                    }

                    _go = HexaPooling.Instance.PullObject(_htype);

                    if(_go != null)
                    {
           
                        _go.GetComponent<HexagonBasics>()?.SetHexagonData(new HexagonData(int.MinValue, HexaFunctions.GetRandomColorIndex, int.MinValue));
                        
                        _go.transform.position = this.vertices[i, j].transform.position + (new Vector3(0.0f, this.vertices[_nRows - 2, j].transform.position.y + 128f, 0.0f));

                        _go.SetActive(true);
                    }


                }

                _hexagroup.AddHexagon(_go.transform);

                this.vertices[i, j].PlaceObject(_go, true);
            }
        }

    }

    // Oyunda hamle kalıp kalmadığını anlamak için kullanılır, eğer bir hexagonun, kendisi hariç, etrafında en az 3 tane aynı renkte hexagon varsa
    // (öyle ki) bu aynı renkte hexagonlar doğrusal / hepsi çift indisli kenarlarda / hepsi tek indisli kenarlarda olmayacak
    public bool IsGameOver()
    {

        // bütün gridleri tara, ilk hamleyi bulduğunda false ile geri dön

        int _nRows = this.vertices.GetLength(0);
        int _nColumns = this.vertices.GetLength(1);

        for (int i = 0; i < _nRows; i++)
        {
            for (int j = 0; j < _nColumns; j++)
            {
                if ( (this.vertices[i, j] == null))
                    continue;

                if (this.vertices[i, j].IsEmpty == true)
                    continue;

                if( this.vertices[i, j].transform.GetComponentInChildren<HexagonBasics>()?.IsTripletCandidate() == true)
                    return false;
                
            }
        }


        return true;
    }

    private GameObject GetFromMap(int _rowIndex, int _colIndex)
    {

        // olusturulan grid sistemde bir nesnenin üzerindeki grid _rowIndex + 2 'de duracaktır
        int _nRows = this.vertices.GetLength(0);

        for (int _upIndex = _rowIndex + 2; _upIndex < _nRows; _upIndex += 2)
        {

            if(this.vertices[_upIndex, _colIndex] == null)
                continue;

            // boşsa atla
            if (this.vertices[_upIndex, _colIndex].IsEmpty == true)
                continue;

            // bunu boşalt
            GameObject _go = this.vertices[_upIndex, _colIndex].ReleaseGridObject();

            if (_go == null)
                continue;

            // çağırana git
            return _go;
        }

        return null;
    }

    public void GetAllHexagons(out List<HexagonData> _hexagons)
    {
        _hexagons = new List<HexagonData>();

        int _nRows = this.vertices.GetLength(0);
        int _nColumns = this.vertices.GetLength(1);

        for (int i = 0; i < _nRows; i++)
        {
            for (int j = 0; j < _nColumns; j++)
            {
                if (this.vertices[i, j] == null)
                {
                    _hexagons.Add(new HexagonData());
                    continue;
                }

                if (this.vertices[i, j].IsEmpty == true)
                {
                    _hexagons.Add(new HexagonData());
                    continue;
                }

                _hexagons.Add(this.vertices[i, j].GetHexaData());

            }
        }

    }

}
