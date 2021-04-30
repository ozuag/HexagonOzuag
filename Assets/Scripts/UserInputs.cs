using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using HexaFall.Basics;

public class UserInputs : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{

    public static UserInputs Instance { private set; get; }

    [SerializeField]
    private Transform groupIndicator;

    private Vector2 dragStartPosition;

    private Coroutine groupRotateCoroutine = null;

    private bool isHexagonSelectionActive = true;

    private HexaGroup tripleSet;

    private void Awake()
    {
        Instance = this;

        this.tripleSet = new HexaGroup();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (this.isHexagonSelectionActive == false)
            return;

        this.dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (this.isHexagonSelectionActive == false)
            return;

        if (this.groupIndicator == null)
            return;

        // eğer drag belirli bir değeri aştıysa varsa gruba bilgi ver
        float _dragDistance = Vector2.Distance(this.dragStartPosition, eventData.position);

        if(_dragDistance > 13.333f)
        {
            // dönme açısını hesapla
            Vector3 _point1 = Camera.main.ScreenToWorldPoint(this.dragStartPosition);
            _point1 = this.groupIndicator.InverseTransformPoint(_point1);

            Vector3 _point2 = Camera.main.ScreenToWorldPoint(eventData.position);
            _point2 = this.groupIndicator.InverseTransformPoint(_point2);

            // local space'e geçtiğin için noktalar orijine gore konumlandı, doğrudan açı hesaplayabilirsin    
            float _angle = Vector2.SignedAngle(_point1, _point2);

            // anlamlı bir derecede çevir
            if(Mathf.Abs(_angle) > 6.66f)
            {
                this.CheckGroup(_angle > 0f);
                this.dragStartPosition = eventData.position;
            }
               

            
        }


    }

    public void OnEndDrag(PointerEventData eventData)
    {
        this.dragStartPosition = eventData.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {

        // Gruplama oluştur
        if (this.isHexagonSelectionActive == false)
            return;

        RaycastHit2D _hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(eventData.position));

        if(_hit)
            _hit.transform.GetComponent<HexagonBasics>()?.OnSelected(_hit.transform.InverseTransformPoint(_hit.point));

        // if (_hit.transform != null)
        // {

        //     HexagonBasics _hexa = _hit.transform.GetComponent<HexagonBasics>();

        //     if (_hexa != null)
        //         _hexa.OnSelected(_hit.transform.InverseTransformPoint(_hit.point));

        // }

    }

    // BUNLARIN YERINI DEGISTIR
    public void SetGroup(List<Transform> _groupObjects)
    {

        if (this.tripleSet == null)
            this.tripleSet = new HexaGroup();

        this.tripleSet.SetGroup(_groupObjects);


        if (this.groupIndicator == null)
            return;

        Vector3 _pos = this.tripleSet.pivotWPosition;
        _pos.z = this.groupIndicator.position.z;
        this.groupIndicator.position = this.tripleSet.pivotWPosition;

        this.groupIndicator.localPosition = new Vector3(this.groupIndicator.localPosition.x, this.groupIndicator.localPosition.y, 0.0f);

        this.groupIndicator.gameObject.SetActive(true);

    }

    public void CheckGroup(bool _ccw = true)
    {
        if (this.groupIndicator == null)
            return;

        if (this.groupRotateCoroutine != null)
            return;

        this.groupRotateCoroutine = StartCoroutine(this.CheckGroupPositions(_ccw));

    }

    private IEnumerator CheckGroupPositions(bool _ccw = true)
    {
        this.isHexagonSelectionActive = false;

        float _direciton = _ccw ? 1f : -1f;

        float _deltaAngle = 6.66f;

        for (int _ite = 0; _ite < 3; _ite++)
        {
           
            // harekete başlamadan önce karışıklık olmasın diye collider'ları pasif yap
            this.tripleSet.SetGroupEdgeColliders(false);

            yield return new WaitForEndOfFrame();

            float _rotation = 0.0f;
            for(_rotation = 0.0f; _rotation <= 120f; _rotation += _deltaAngle)
            {
                this.tripleSet.RotateTripleSet(_direciton *_deltaAngle);
                yield return new WaitForFixedUpdate();
            }

            // eksik açı kaldıysa tamamla
            this.tripleSet.RotateTripleSet(_direciton * (120f - _rotation));

            this.tripleSet.SwapParents(_ccw);

            // üçleme var mı kontrolü için collider'ları aktif et
            this.tripleSet.SetGroupEdgeColliders(true);

            // tetiklenmeler resetlenmeli
            for(int i=0; i<8; i++)
                yield return new WaitForEndOfFrame();

            // başlangıç konumuna dönersen eşleşme var mı diye bakmana gerek yok
            if ( _ite >= 2 )
                break;


            // bu konumda doğru eşleşme varsa patlat
            int _points = this.tripleSet.CheckGroupState();
            if (_points > 0)
            {
                // patlama olacak grubu dağıt
                this.tripleSet.Ungroup();

                if(this.groupIndicator != null)
                    this.groupIndicator.gameObject.SetActive(false);

                HexaFunctions.DestroyWantedList?.Invoke();

                yield return new WaitForSeconds(0.13f);

                // kullanıcı hamlesi ile oluştu -< true
                bool _isNewLevel = User.Instance.AddPoint(_points, true);

                // hareketler sonrası yeni patlama olmayana kadar hareket edenleri check et
                while (true)
                {
                    HexaGridSystem.Instance.FillEmptyGridsOnly(_isNewLevel, out HexaGroup _hexaGroup);

                    //_hexaGroup.SetGroupEdgeColliders(false);

                    yield return new WaitForSeconds(0.333f);

                    int __points = _hexaGroup.CheckGroupState();

                    _hexaGroup.Ungroup(); // patlama olsun ya da olması geçici bir gruplamaydı

                    // yeni üçlemeler olmayana kadar bunu sürdür
                    if (__points <= 0)
                        break;

                    // boşluklar doldurulurken puan geldi, -< default val -> false
                    _isNewLevel = User.Instance.AddPoint(__points);

                    HexaFunctions.DestroyWantedList?.Invoke();

                    yield return new WaitForSeconds(0.13f);

                }

                
                // artık deneme
                break;
            }

            // bekle ve tık sesi ver istersen
            yield return new WaitForSeconds(0.13f);
        }


        // Yapılabilecek hamle var mı?
        if(HexaGridSystem.Instance.IsGameOver())
        {
            HexaFunctions.GameOver?.Invoke("Yapılabilecek hamle kalmadı!!");

            Debug.Log("************GAME OVER*******************");
        }

        if(this.groupIndicator != null)
            this.groupIndicator.localEulerAngles = Vector3.zero;

        this.isHexagonSelectionActive = true;
        this.groupRotateCoroutine = null;
        yield return null;
    }

    // oyun bitti eventi geldiğinde varsa aktif coroutineleri durdur
    public void StopCurrentGame(string _message = "")
    {
        if(this.groupRotateCoroutine != null)
        {
            StopCoroutine(this.groupRotateCoroutine);
            this.groupRotateCoroutine = null;
        }

        if(this.tripleSet != null)
            this.tripleSet.Ungroup();

        if(this.groupIndicator != null)
            this.groupIndicator.gameObject.SetActive(false);

        this.isHexagonSelectionActive = true;

    }

   
}
