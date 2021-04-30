using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexaFall.Basics;
 public abstract class HexagonBasics : MonoBehaviour
    {
        [SerializeField]
        protected SpriteRenderer spriteRenderer; // renk ataması yapılacak sprite

        public virtual HexaType HexagonType { get; protected set; } // bomba, starred ya da normal

        // saat yönü tersinde atama yap
        [SerializeField]
        protected List<HexaEdgeCollider> edgeColliders; // kenarların etkileşim kontrolleri için

        private bool isOnWantedList = false; // yok edilecekler listesinde mi

        private Coroutine moveCoroutine = null; // konum değiştirirken bu coroutine çalışır

        public Transform GetTransform => this.transform;

        public virtual HexagonData GetHexagonData() => new HexagonData((int)this.HexagonType);

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

         // Hexagonu havuza gönder
        public void Kill()
        {
            this.BreakHexagon();

            this.GetComponentInParent<HexaGridVertex>()?.ReleaseGridObject();

            this.Reset();

            HexaPooling.Instance.PushObject(this.HexagonType, this.gameObject);

        }

        protected virtual void BreakHexagon()
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
