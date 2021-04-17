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

        Hexagon = 0,
        StarredHexagon,
        Bomb
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

    public interface IHexagon
    {
        Transform GetTransform();

        void OnSelected(Vector2 _localSelecPosition);

        bool AddOnWantedList();

        void SetEdgeCollidersEnableState(bool _state);

        HexaEdgeCollider GetEdgeCollider(int _edgeId);

        HexaType GetHexaType();

        int GetClosestActiveEdge(int _edgeId);

        void SetColor(int _colorId, Color _color);

        int GetColorId();

        void MoveTo(Vector3 _target, float _moveSpeed);

        int TripletState();

        bool IsTripletCandidate(); // bir hamle ile bu grid'de üçlü grup oluşabilir mi? (kendisi üçlü içinde olmak zorunda değil)

    }


    // oyun verileri kaydedilirken kullanılır
    [Serializable]
    public class HexagonData
    {
        public int type;
        public int colorId;

        public HexagonData(int _color = int.MinValue, int _type = int.MinValue)
        {
            type = _type;
            colorId = _color;
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

    public class HexagonBasics : MonoBehaviour, IHexagon
    {
        public SpriteRenderer spriteRenderer; // renk ataması yapılacak sprite

        private int colorId = int.MinValue; // renk id, renk karşılaştırmalarda kullanılır

        [SerializeField]
        private HexaType hexaType = HexaType.Hexagon; // bomba, starred ya da normal

        // saat yönü tersinde atama yap
        [SerializeField]
        private List<HexaEdgeCollider> edgeColliders; // kenarların etkileşim kontrolleri için

        private bool isOnWantedList = false; // yok edilecekler listesinde mi

        private Coroutine moveCoroutine = null; // konum değiştirirken bu coroutine çalışır

        private void OnEnable()
        {
            // bombalar bunu tetikleyebilir
            HexaFunctions.KillAllHexagons += this.Kill;

            this.HexaEnabled();

        }

        protected virtual void HexaEnabled()
        {

        }

        private void OnDisable()
        {
            HexaFunctions.KillAllHexagons -= this.Kill;
            HexaFunctions.DestroyWantedList -= this.Kill;

            this.HexaDisabled();

            this.Reset();
        }

        protected virtual void HexaDisabled()
        {

        }

        private void OnDestroy()
        {
            HexaFunctions.KillAllHexagons -= this.Kill;
            HexaFunctions.DestroyWantedList -= this.Kill;

            this.HexaDestroyer();

            this.Reset();
        }

        protected virtual void HexaDestroyer()
        {

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


        // verilen kenlara en yakın aktif diğer kenear hangisi
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
                    return (int) HexagonEdge.NotDefined;


                //buraya geldiyse saat yönündekine de bak
                _tempEdge = _edgeId - _step;
                if (_tempEdge < 0)
                    _tempEdge += this.edgeColliders.Count; // negatif değer düşme

                if (this.edgeColliders[_tempEdge].ActiveEdge)
                    return _tempEdge;

            }

            // bulamadın :(
            return (int) HexagonEdge.NotDefined;
        }



        // bu hexagnonun mevcut konumumda üçlü olma durumu ve eğer üçlü ise patlatıldığında kaç puan gelir
        public int TripletState()
        {
            bool _isTriplet = false; // en az üçlü ise  puan hesaplanacak
            int _totalPoint = 0; // puan

            // bu hexagon ile elde edilebilecek toplam puanı hesaplayacak, komşuların türlerini tut
            Dictionary<HexaType, int> _pointCounter = new Dictionary<HexaType, int>();

            // bütün edge colliderlara git bağlı olduklarının rengine bak
            for (int i = 0; i < this.edgeColliders.Count; i++)
            {

                if (this.edgeColliders[i].ActiveEdge == false)
                    continue;

                // i. kenarından bağlı olan diper altıgen
                LinkedObject _edgeLinked = this.edgeColliders[i].LinkedHexagon;

                // bağlı olduğu altıgen ile renk uyumu yoksa dikkate alma
                if (_edgeLinked.hexagon.GetColorId() == this.colorId)
                {
                 
                    // saat yönü tersindeki kenara bak, üçleme oluyorsa işaretle
                    int _ccwEdge = (int)_edgeLinked.edge + 1;
                    _ccwEdge %= 6;

                    // komşusu ayno renkte, peki komşunun üçlü oluşturabilecek komşuları aynı renkte mi

                    HexaEdgeCollider _hec = _edgeLinked.hexagon.GetEdgeCollider(_ccwEdge); // komşunun, ccw'deki komşusu

                    if(_hec != null)
                    {
                        if (_hec.ActiveEdge)
                        {
                            LinkedObject _ccwLinked = _hec.LinkedHexagon;

                            if (_ccwLinked.hexagon.GetColorId() == this.colorId) // komşunun ccw'dekş komşusu aynı renk mi
                            {
 
                                // listeye eklenebiliyorsa ödlünü al, daha önce listeye girdiyse ödülünü almak için geç kaldın

                                // üçlü oluşturuldu, yok edilecekler listesine ekle
                                if( this.AddOnWantedList() ) // KENDİSİNİ EKLE
                                {
                                    // listeye eklenebildi, ödülü al
                                    
                                    if (_pointCounter.ContainsKey(this.hexaType) == false)
                                        _pointCounter.Add(this.hexaType, 1);
                                    else
                                        _pointCounter[this.hexaType]++;
                                }

                                if(_edgeLinked.hexagon.AddOnWantedList()) // KOMŞUSUNU EKLE
                                {
                                    HexaType _linkedType = _edgeLinked.hexagon.GetHexaType();
                                    if (_pointCounter.ContainsKey(_linkedType) == false)
                                        _pointCounter.Add(_linkedType, 1);
                                    else
                                        _pointCounter[_linkedType]++;
                                }

                                if(_ccwLinked.hexagon.AddOnWantedList()) // KOMŞUSUNUN KOMŞUSUNU EKLE
                                {
                                    HexaType _linkedType = _ccwLinked.hexagon.GetHexaType();

                                    if (_pointCounter.ContainsKey(_linkedType) == false)
                                        _pointCounter.Add(_linkedType, 1);
                                    else
                                        _pointCounter[_linkedType]++;
                                }

                                _isTriplet = true;

                            }

                        }
                    }

                   
                    // YUKARIDA YAPILANLARI CW YONDE TEKRARLA -> DONGU YAPILABILIR
                    // saat yönğndeki komşu kenara bak, üçleme oluyorsa işaretle
                    int _cwEdge = (int)_edgeLinked.edge - 1;
                    if (_cwEdge < 0)
                        _cwEdge += 6;

                    _hec = _edgeLinked.hexagon.GetEdgeCollider(_cwEdge);

                    if(_hec != null)
                    {
                        if (_hec.ActiveEdge)
                        {
                            LinkedObject _cwLinked = _hec.LinkedHexagon;

                            if (_cwLinked.hexagon.GetColorId() == this.colorId)
                            {
                                // listeye eklenebiliyorsa ödlünü al
                                if (this.AddOnWantedList())
                                {

                                    if (_pointCounter.ContainsKey(this.hexaType) == false)
                                        _pointCounter.Add(this.hexaType, 1);
                                    else
                                        _pointCounter[this.hexaType]++;
                                }


                                if (_edgeLinked.hexagon.AddOnWantedList())
                                {
                                    HexaType _linkedType = _edgeLinked.hexagon.GetHexaType();
                                    if (_pointCounter.ContainsKey(_linkedType) == false)
                                        _pointCounter.Add(_linkedType, 1);
                                    else
                                        _pointCounter[_linkedType]++;
                                }

                                if (_cwLinked.hexagon.AddOnWantedList())
                                {
                                    HexaType _linkedType = _cwLinked.hexagon.GetHexaType();

                                    if (_pointCounter.ContainsKey(_linkedType) == false)
                                        _pointCounter.Add(_linkedType, 1);
                                    else
                                        _pointCounter[_linkedType]++;
                                }

                                _isTriplet = true;
                            }

                        }
                    }

                   
                }
            }


            //  En az üçlütane hexa aynı renkte grup olduysa, hepsinin puanını birlikte burada hesapla
            if(_isTriplet)
            {
                // puanı hesapla
                int _nHexagons = 0; // listeye giren hexagonların sayısı, bomba ve yıldızlı olanları da say
                int _multiplier = 1; // yıldızlı/bomba başına *2

                foreach(KeyValuePair<HexaType, int> _keyPair in _pointCounter)
                {
                    if (_keyPair.Key != HexaType.NotDefined)
                        _nHexagons += _keyPair.Value;

                    if ((_keyPair.Key == HexaType.Bomb) | (_keyPair.Key == HexaType.StarredHexagon))
                        _multiplier *= ((int) Mathf.Pow(2, _keyPair.Value));
                }

                _totalPoint = _nHexagons * _multiplier * HexaFunctions.HexaKillPoint;
            }


            _pointCounter.Clear();

            return _totalPoint;
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
            Dictionary<int, bool> _odds = new Dictionary<int, bool> ();


            // kendi rengini ekleme
            //_adjacentColors.Add(this.colorIndex, 1);

            for (int i = 0; i < this.edgeColliders.Count; i++)
            {

                if (this.edgeColliders[i].ActiveEdge == false)
                    continue;

                int _colorKey = int.MinValue;

                if(this.edgeColliders[i].LinkedHexagon.hexagon != null)
                    _colorKey = this.edgeColliders[i].LinkedHexagon.hexagon.GetColorId();

                if (_colorKey < 0)
                    continue;

                // i .kenarda anlamlı bir renk id'si var, saymak için listeye ekle

                if (_adjacentColors.ContainsKey(_colorKey) == false)
                    _adjacentColors.Add(_colorKey, 1);
                else
                    _adjacentColors[_colorKey]++;


                // bu renk tek  indisli mi çift indisli mi kenara ait, ilgili değeri oluştur ya da artır
                if( (i%2) == 0 )
                {
                    if (_evens.ContainsKey(_colorKey) == false)
                        _evens.Add(_colorKey, true);
                }
                else
                {
                    if (_odds.ContainsKey(_colorKey) == false)
                        _odds.Add(_colorKey, true);
                }


                // koşu sağlanırsa bu hexagonun kendisi / komşuları TRINITIY :D
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
            HexaGridVertex _hgv = this.GetComponentInParent<HexaGridVertex>();

            if (_hgv != null)
                _hgv.ReleaseGridObject();

            this.Reset();

            HexaPooling.Instance.PushObject(this.hexaType, this.gameObject);

            this.KillingMeSoftly();
        }

        // havuza giderken ekstra bir şeyler yapmak istersen override et
        protected virtual void KillingMeSoftly()
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
        private void Reset()
        {
            // varsa hexagon ile ilgili tüm tanımlamaları ve initial değerleri kaldır
            this.isOnWantedList = false;
            this.colorId = int.MinValue;

            if (this.moveCoroutine != null)
            {
                StopCoroutine(this.moveCoroutine);
                this.moveCoroutine = null;

            }
        }

        public void SetColor(int _colorId, Color _color)
        {
            this.colorId = _colorId;

            if (this.spriteRenderer != null)
                this.spriteRenderer.color = _color;
        }

        public void SetEdgeCollidersEnableState(bool _state)
        {
            if (this.edgeColliders == null)
                return;

            for (int i = 0; i < this.edgeColliders.Count; i++)
                this.edgeColliders[i].SetColliderEnabledState(_state);

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

            _groupList.Add(_firstLinkedObject.hexagon.GetTransform());

            // bu hexagon için _ccw yönündeki kenara bak bağlantılı bir hexagon varsa onu al yoksa _cw 'dekine bak, yine yoksa false
            int _nextEdge = ((int)_firstLinkedObject.edge) + (_ccwSearch ? 1 : -1);

            if (_nextEdge < 0) _nextEdge += 6;
            else if (_nextEdge > 5) _nextEdge -= 6;

            HexaEdgeCollider _hec = _firstLinkedObject.hexagon.GetEdgeCollider(_nextEdge);
            if(_hec != null)
            {
                _secondLinkedObject = _hec.LinkedHexagon;

                if (_secondLinkedObject.hexagon != null)
                {
                    _groupList.Add(_secondLinkedObject.hexagon.GetTransform());
                    return true;
                }
            }
           
            _nextEdge = ((int)_firstLinkedObject.edge) + (_ccwSearch ? -1 : 1);

            if (_nextEdge < 0) _nextEdge += 6;
            else if (_nextEdge > 5) _nextEdge -= 6;

            _hec = _firstLinkedObject.hexagon.GetEdgeCollider(_nextEdge);
            if(_hec != null)
            {
                _secondLinkedObject = _hec.LinkedHexagon;

                if (_secondLinkedObject.hexagon != null)
                {
                    _groupList.Add(_secondLinkedObject.hexagon.GetTransform());
                    return true;
                }
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

        public int GetColorId()
        {
            return this.colorId;
        }

        public HexaEdgeCollider GetEdgeCollider(int _edgeId)
        {
            if (this.edgeColliders == null)
                return null;

            if ( (_edgeId < 0) | (_edgeId >= this.edgeColliders.Count))
                    return null;

            return this.edgeColliders[_edgeId];

        }

        public Transform GetTransform()
        {
            return this.transform;
        }

        public HexaType GetHexaType()
        {
            return this.hexaType;
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

        public void AddHexagon(Transform _transform)
        {
            if (this.hexagons != null)
                this.hexagons.Add(_transform);
        }

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

        public void SwapParents( bool _ccwOrder )
        {
            int _nHexagons = this.hexagons.Count;

            List<Transform> _initialParents = new List<Transform>();
            for (int i = 0; i < _nHexagons; i++)
                _initialParents.Add(this.hexagons[i].parent);

            int _nextIndexStep = _ccwOrder ? 1 : -1;

            for(int i=0; i < _nHexagons; i++)
            {
                int _nextIndex = i + _nextIndexStep;

                if (_nextIndex < 0)
                    _nextIndex += _nHexagons;
                else if (_nextIndex >= _nHexagons)
                    _nextIndex -= _nHexagons;

                this.hexagons[i].SetParent( _initialParents[_nextIndex] );

            }

            _initialParents.Clear();



        }

        public void SetGroupEdgeColliders(bool _enabled)
        {
            for (int i = 0; i < this.hexagons.Count; i++)
            {

                IHexagon _hex = this.hexagons[i].GetComponent<IHexagon>();

                if(_hex == null)
                    continue;

                _hex.SetEdgeCollidersEnableState(_enabled);

            }
        }

        public int CheckGroupState()
        {
            int _totalPoint = 0;

            for (int i = 0; i < this.hexagons.Count; i++)
            {

                IHexagon _hex = this.hexagons[i].GetComponent<IHexagon>();

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

            foreach(Transform _tr in this.hexagons)
            {
                SortingGroup _sg = _tr.GetComponent<SortingGroup>();

                if(_sg != null)
                    _sg.sortingLayerName = _sortingLayer.ToString();
                
            }

        }


        public void Ungroup()
        {
            if (this.hexagons != null)
                this.hexagons.Clear();

            this.pivotWPosition =  Vector2.zero;


        }

        ~HexaGroup()
        {
            if(this.hexagons != null)
                this.hexagons.Clear();
        }


    }

    public class LinkedObject
    {
        public HexagonEdge edge;
        public int uniqeId;
        public IHexagon hexagon;

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

            for(int i =0; i < _values.Length; i++)
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

}