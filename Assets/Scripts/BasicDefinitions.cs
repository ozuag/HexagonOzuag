using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexaFall.Basics
{
    public enum SortingLayers
    {
        Bottom,
        Mid,
        Top,
        TopMost
    }

    public enum HexaType
    {
        NotDefined = -666,

        ColorHexagon = 0,
        StarredHexagon,
        BombHexagon,

        HexaParticles = 666
    }

    public enum HexagonEdge
    {
        NotDefined = -666,

        TopRight = 0, // (0 - 60]
        Top = 1, // (60 - 120]
        TopLeft = 2, // (120 - 180]
        BottomLeft = 3, // (180 - 240] 
        Bottom = 4, // (240 - 300] 
        BottomRight = 5, // (300 - 360] 

    }

    // Bomba (parametersi olan hexagonlar için)
    public interface IBombHexagon
    {
        void SetParameter(int _parameter1 = -666);

        int GetParameter();
    }

    // rengi olmayan hexagonlar da gelebilir (elmas, star vb -> HexaFall'da varlar)
    public interface IColorHexagon
    {
        void SetColor(int _colorId);

        int GetColorId();

        // kendisi ile aynı renkte 3'lü grup oluşturabildi mi
        int TripletState();

    }

    // rengi 
    public interface IHexagon
    {

        void OnSelected(Vector2 _localSelecPosition);

        bool AddOnWantedList();

        void SetEdgeCollidersEnableState(bool _state);

        int GetClosestActiveEdge(int _edgeId);

        void MoveTo(Vector3 _target, float _moveSpeed);

        // kendi rengi önemsiz olduğu için en alt katmandaki interface'de, tüm hexagonlarda bu sorug yapıabilir
        bool IsTripletCandidate(); // bir hamle ile bu grid'de üçlü grup oluşabilir mi? (kendisi üçlü içinde olmak zorunda değil)

    }

    public abstract class BasicHexagon : MonoBehaviour, IHexagon
    {
        [SerializeField]
        protected SpriteRenderer spriteRenderer; // renk ataması yapılacak sprite

        [SerializeField]
        protected HexaType hexaType = HexaType.NotDefined; // bomba, starred ya da normal

        // saat yönü tersinde atama yap
        [SerializeField]
        protected List<HexaEdgeCollider> edgeColliders; // kenarların etkileşim kontrolleri için

        private bool isOnWantedList = false; // yok edilecekler listesinde mi

        private Coroutine moveCoroutine = null; // konum değiştirirken bu coroutine çalışır

        public Transform GetTransform => this.transform;

        public HexaType GetHexaType => this.hexaType;

        public virtual HexagonData GetHexagonData() => new HexagonData((int)this.hexaType);

        public virtual void SetHexagonData(HexagonData _data) { }

        protected virtual void OnEnable()
        {
            // bombalar bunu tetikleyebilir
            HexaFunctions.KillAllHexagons += this.Kill;
        }

        protected virtual void OnDisable()
        {
            HexaFunctions.KillAllHexagons -= this.Kill;
            HexaFunctions.DestroyWantedList -= this.Kill;

            this.Reset();
        }

        protected virtual void OnDestroy()
        {
            HexaFunctions.KillAllHexagons -= this.Kill;
            HexaFunctions.DestroyWantedList -= this.Kill;

            this.Reset();
        }

        public bool AddOnWantedList()
        {
            // birden fazla hexagon/edge/group bunu listeye almak isteyebilir, sadece bir defa alınsın
            if (this.isOnWantedList)
                return false; // listeye ekleyemezsen haber ver

            HexaFunctions.DestroyWantedList += this.Kill;

            this.isOnWantedList = true;
            return true;
        }    

        // a en yakın aktif diğer kenear hangisi
        public int GetClosestActiveEdge(int _edgeId)
        {
            // en fazla 3 adımda yakından uzağa komşualara bakabiliriz
            for (int _step = 1; _step <= 3; _step++)
            {
                // ccw, önce saat yönü tersindekine bak
                int _tempEdge = _edgeId + _step;
                _tempEdge %= this.edgeColliders.Count; // sınır aşımı olmasın

                // eğer aktif ise ilk adımda buldun
                if (this.edgeColliders[_tempEdge].ActiveEdge)
                    return _tempEdge;

                // eğer son adımdaysan ve buraya geldiyse aşağıya bakmana gerek yok, +3, -3 aynı
                if (_step == 3)
                    return (int)HexagonEdge.NotDefined;


                //buraya geldiyse saat yönündekine de bak
                _tempEdge = _edgeId - _step;
                if (_tempEdge < 0)
                    _tempEdge += this.edgeColliders.Count; // negatif değer düşme

                if (this.edgeColliders[_tempEdge].ActiveEdge)
                    return _tempEdge;

            }

            // bulamadın :(
            return (int)HexagonEdge.NotDefined;
        }

        // Oyunda hamle kalıp kalmadığını anlamak için kullanılır, eğer bir hexagonun kendisi hariç etrafında en az 3 tane aynı renkte hexagon varsa
        // (öyle ki) bu aynı renkte hexagonlar doğrusal / hepsi çift indisli kenarlarda / hepsi tek indisli kenarlarda olmayacak
        public bool IsTripletCandidate()
        {

            bool _result = false;

            // belirli bir renkte kaç adet komşu olduğunu tutacak
            Dictionary<int, int> _adjacentColors = new Dictionary<int, int>();


            // ilgili renkteki komşuların kaçar tanesi tek / çift indisli bir kenarda
            Dictionary<int, bool> _evens = new Dictionary<int, bool>();
            Dictionary<int, bool> _odds = new Dictionary<int, bool>();


            // kendi rengini ekleme
            //_adjacentColors.Add(this.colorIndex, 1);

            for (int i = 0; i < this.edgeColliders.Count; i++)
            {

                if (this.edgeColliders[i].ActiveEdge == false)
                    continue;

                int _colorKey = (int) this.edgeColliders[i].LinkedHexagon.hexagon?.GetHexagonData()?.colorId;

                if (_colorKey < 0)
                    continue;

                // i .kenarda anlamlı bir renk id'si var, saymak için listeye ekle

                if (_adjacentColors.ContainsKey(_colorKey) == false)
                    _adjacentColors.Add(_colorKey, 1);
                else
                    _adjacentColors[_colorKey]++;


                // bu renk tek  indisli mi çift indisli mi kenara ait, ilgili değeri oluştur
                if ((i % 2) == 0)
                {
                    if (_evens.ContainsKey(_colorKey) == false)
                        _evens.Add(_colorKey, true);
                }
                else
                {
                    if (_odds.ContainsKey(_colorKey) == false)
                        _odds.Add(_colorKey, true);
                }


                // koşul sağlanırsa bu hexagonun kendisi / komşuları TRINITIY :D
                if (_adjacentColors[_colorKey] >= 3)
                {
                    if (_evens.ContainsKey(_colorKey) & _odds.ContainsKey(_colorKey))
                    {
                        _result = true;
                        break;
                    }

                }

            }

            _adjacentColors.Clear();
            _evens.Clear();
            _odds.Clear();
            return _result;
        }

        // Hexagonu havuza gönder
        public void Kill()
        {
            this.BreakHexagon();

            this.GetComponentInParent<HexaGridVertex>()?.ReleaseGridObject();

            this.Reset();

         

            HexaPooling.Instance.PushObject(this.hexaType, this.gameObject);

        }

        protected virtual void BreakHexagon()
        {

        }

        // hedefe doğru gönder
        public void MoveTo(Vector3 _target, float _moveSpeed)
        {
            if (this.moveCoroutine != null)
                StopCoroutine(this.moveCoroutine);

            this.moveCoroutine = StartCoroutine(this.Move(_target, _moveSpeed));
        }

        // bir hexagon seçilirken hangi kenar ve komşularda hangi yöne öncelikli bakılacağı dokunulan noktası ile belirlenir
        public void OnSelected(Vector2 _localSelecPosition)
        {
            if ((this.edgeColliders == null) | (this.edgeColliders.Count < 3))
                return;

            // seçim hangi yöndeki kenar için geldi
            float _angle = Vector2.SignedAngle(Vector3.right, _localSelecPosition.normalized);
            float _angleStep = _angle / 60f;
            int _edgeIndex = Mathf.FloorToInt(_angleStep);

            // Gruplamayı öncelikle hangi yönde yapmayı deneyeceğine karar ver
            bool _ccwSearch = (_angleStep - _edgeIndex) <= 0.5f;

            // eksi yöndeki yönelimi düzelt
            if (_edgeIndex < 0)
                _edgeIndex += this.edgeColliders.Count;

            // seçilen kenar boşluktaysa onun yerine kendisine en yakın dolu olan kenar ile işleme devam et
            if (this.edgeColliders[_edgeIndex].ActiveEdge == false)
                _edgeIndex = GetClosestActiveEdge(_edgeIndex);

            if (_edgeIndex < 0)
                return;

            // dokunduğun noktaya göre group oluşturabiliryorsan oluştur
            if (this.SearchGroup(_edgeIndex, _ccwSearch, out List<Transform> _group))
                UserInputs.Instance.SetGroup(_group);

        }

        // havuza giderken ya da tekrar oyuna girerken reset
        protected virtual void Reset()
        {
            // varsa hexagon ile ilgili tüm tanımlamaları ve initial değerleri kaldır
            this.isOnWantedList = false;
           
            if (this.moveCoroutine != null)
            {
                StopCoroutine(this.moveCoroutine);
                this.moveCoroutine = null;

            }

        }

        public void SetEdgeCollidersEnableState(bool _state)
        {
            if (this.edgeColliders == null)
                return;

            for (int i = 0; i < this.edgeColliders.Count; i++)
                this.edgeColliders[i].SetColliderEnabledState(_state);

        }

        public LinkedObject GetEdgeNeighbor(int _edgeId, bool _ccwSearch = true, bool _mustBeSameColor = true)
        {

            int _preferredStep = _ccwSearch ? 1 : -1;

            // önce istenen yödeki kenara bak, aynı renkte ise bunu gönder
            int _adjacentEdgeId = _edgeId + _preferredStep;

            if(_adjacentEdgeId < 0)
                _adjacentEdgeId += this.edgeColliders.Count;
            else if(_adjacentEdgeId >= this.edgeColliders.Count)
                _adjacentEdgeId -= this.edgeColliders.Count;

            int? _colorId = this.edgeColliders[_adjacentEdgeId]?.LinkedHexagon?.hexagon?.GetHexagonData()?.colorId;

            if ((_colorId == this.GetHexagonData()?.colorId) | !_mustBeSameColor)
                return this.edgeColliders[_adjacentEdgeId]?.LinkedHexagon;

            // buraya gelirsen saat yönündekine bak
            // saat yönğndeki komşu kenara bak, üçleme oluyorsa işaretle
            _adjacentEdgeId = _edgeId - _preferredStep;

            if (_adjacentEdgeId < 0)
                _adjacentEdgeId += this.edgeColliders.Count;
            else if (_adjacentEdgeId >= this.edgeColliders.Count)
                _adjacentEdgeId -= this.edgeColliders.Count;

            _colorId = this.edgeColliders[_adjacentEdgeId]?.LinkedHexagon?.hexagon?.GetHexagonData()?.colorId;

            if ((_colorId == this.GetHexagonData()?.colorId) | !_mustBeSameColor)
                return this.edgeColliders[_adjacentEdgeId]?.LinkedHexagon;


            return null;

        }

        // _ccwSearch = true ise öncelikli arama yönü saat yönü tersi, false ise öncelikli arama yönü saat yönü
        private bool SearchGroup(int _startEdge, bool _ccwSearch, out List<Transform> _groupList)
        {
            _groupList = new List<Transform>();

            // ilk eleman kendisi
            _groupList.Add(this.transform);

            // bu kenar ile bağlantılı olan hexagon
            LinkedObject _firstLinkedObject = new LinkedObject();
            LinkedObject _secondLinkedObject = new LinkedObject();

            _firstLinkedObject = this.edgeColliders[_startEdge].LinkedHexagon;

            if (_firstLinkedObject.hexagon == null)
            {
                _groupList.Clear();
                return false;
            }

            _groupList.Add(_firstLinkedObject.hexagon.GetTransform);


            _secondLinkedObject = _firstLinkedObject.hexagon.GetEdgeNeighbor((int) _firstLinkedObject.edge, _ccwSearch, false);

            if (_secondLinkedObject.hexagon != null)
            {
                _groupList.Add(_secondLinkedObject.hexagon.GetTransform);
                return true;
            }

            _groupList.Clear();
            return false; // grup bulamadı
        }

        private IEnumerator Move(Vector3 _targetPosition, float _moveSpeed = 1.33f)
        {

            float _timer = 0f;

            Vector3 _startPosition = this.transform.position;

            while (_timer <= 1f)
            {
                this.transform.position = Vector3.Lerp(_startPosition, _targetPosition, _timer);

                _timer += (Time.deltaTime * _moveSpeed);
                yield return new WaitForEndOfFrame();
            }
            this.transform.position = _targetPosition;

            this.moveCoroutine = null;
            yield return null;
        }

    }

    public class HexaGroup
    {
        public List<Transform> hexagons;
        public Vector2 pivotWPosition;

        public HexaGroup()
        {
            this.hexagons = new List<Transform>();
            this.pivotWPosition = Vector3.zero;
        }

        public void AddHexagon(Transform _transform) => this.hexagons?.Add(_transform);

        public void SetGroup(List<Transform> _hexagons)
        {
            if (this.hexagons == null)
            {
                this.hexagons = new List<Transform>();
            }
            else
            {
                this.SetGroupSorting(SortingLayers.Mid);
                this.hexagons.Clear();
            }

            this.pivotWPosition = Vector2.zero;

            for (int i = 0; i < _hexagons.Count; i++)
                this.pivotWPosition += new Vector2(_hexagons[i].position.x, _hexagons[i].position.y);

            this.pivotWPosition /= _hexagons.Count;

            // giriş değeri olark bu hexagonları ccw yönünde sırala, swap'da bu bilgiyi kullanacağız
            float[] _angles = new float[_hexagons.Count];

            for (int i = 0; i < _hexagons.Count; i++)
            {
                _angles[i] = Vector2.SignedAngle(Vector2.right, new Vector2(_hexagons[i].position.x, _hexagons[i].position.y) - this.pivotWPosition);

                if (_angles[i] < 0)
                    _angles[i] += 360f;
            }

            // saat yönü tersi düzende transformları buraya ekle
            for (int i = 0; i < _hexagons.Count; i++)
            {
                int _minIndex = HexaFunctions.IndexOfMinVal(_angles);

                if (_minIndex < 0)
                    continue;

                this.hexagons.Add(_hexagons[_minIndex]);

                _angles[_minIndex] = int.MaxValue;
            }


            this.SetGroupSorting(SortingLayers.Top);

            _angles = null;
        }

        public void RotateTripleSet(float _angle)
        {
            for (int i = 0; i < this.hexagons.Count; i++)
                this.hexagons[i].RotateAround(this.pivotWPosition, this.hexagons[i].forward, _angle);

        }

        public void SwapParents(bool _ccwOrder)
        {
            int _nHexagons = this.hexagons.Count;

            List<Transform> _initialParents = new List<Transform>();
            for (int i = 0; i < _nHexagons; i++)
                _initialParents.Add(this.hexagons[i].parent);

            int _nextIndexStep = _ccwOrder ? 1 : -1;

            for (int i = 0; i < _nHexagons; i++)
            {
                int _nextIndex = i + _nextIndexStep;

                if (_nextIndex < 0)
                    _nextIndex += _nHexagons;
                else if (_nextIndex >= _nHexagons)
                    _nextIndex -= _nHexagons;

                this.hexagons[i].SetParent(_initialParents[_nextIndex]);

            }

            _initialParents.Clear();



        }

        public void SetGroupEdgeColliders(bool _enabled)
        {
            for (int i = 0; i < this.hexagons.Count; i++)
                this.hexagons[i].GetComponent<BasicHexagon>()?.SetEdgeCollidersEnableState(_enabled); ;
        }

        public int CheckGroupState()
        {
            int _totalPoint = 0;

            for (int i = 0; i < this.hexagons.Count; i++)
            {

                ColorHexagon _hex = this.hexagons[i].GetComponent<ColorHexagon>();

                if (_hex == null)
                    continue;

                int _point = _hex.TripletState();

                _totalPoint += _point;

            }


            return _totalPoint;
        }

        private void SetGroupSorting(SortingLayers _sortingLayer)
        {
            if (this.hexagons == null)
                return;

            foreach (Transform _tr in this.hexagons)
            {
                SortingGroup _sg = _tr.GetComponent<SortingGroup>();

                if (_sg != null)
                    _sg.sortingLayerName = _sortingLayer.ToString();

            }

        }

        public void Ungroup()
        {
            if (this.hexagons != null)
            {
                this.SetGroupSorting(SortingLayers.Mid);

                this.hexagons.Clear();

            }

            this.pivotWPosition = Vector2.zero;


        }

        ~HexaGroup()
        {
            if (this.hexagons != null)
            {
                this.Ungroup();
                this.hexagons.Clear();
            }

        }

    }

    public class LinkedObject
    {
        public HexagonEdge edge;
        public int uniqeId;
        public BasicHexagon hexagon;

        public LinkedObject()
        {
            this.edge = HexagonEdge.NotDefined;
            this.hexagon = null;
            this.uniqeId = int.MinValue;
        }
    }

    public static class HexaFunctions
    {
        public static Action DestroyWantedList; // Arananlar listesindekileri yok et
        public static Action KillAllHexagons; // bütün hexagonları yok et
        public static Action HexagonMoved;

        public static Action<string> GameOver;

        public const int HexaKillPoint = 5;
        public const int LevelUpdatePoint = 1000;

        public static int IndexOfMinVal(float[] _values)
        {

            int _index = -1;

            float _tempVal = int.MaxValue;

            for (int i = 0; i < _values.Length; i++)
            {
                if (_tempVal > _values[i])
                {
                    _tempVal = _values[i];
                    _index = i;
                }

            }

            return _index;
        }

    }

    // oyun verileri kaydedilirken kullanılır
    [Serializable]
    public class HexagonData
    {
        public int type;
        public int colorId;
        public int parameter1; // rn: bomba sayaç

        public HexagonData(int _type = int.MinValue, int _color = int.MinValue, int _parameter1 = int.MinValue)
        {
            this.type = _type;
            this.colorId = _color;
            this.parameter1 = _parameter1;
        }
    }

    [Serializable]
    public class GameData
    {
        public int score;
        public int moveCount;

        public int nVerticalHexagons;
        public int nHorizontalHexagons;

        public int nDefinedColors;

        public List<HexagonData> hexagons;
    }

}