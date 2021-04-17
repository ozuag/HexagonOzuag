using UnityEngine;
using HexaFall.Basics;

public class HexaEdgeCollider : MonoBehaviour
{
    [SerializeField]
    private HexagonEdge edgeId = HexagonEdge.NotDefined;

    [SerializeField]
    private LinkedObject linkedHexagon = new LinkedObject();

    [SerializeField]
    private bool isColliderActive = false;

    private Collider2D collider2d;

    public string linkedObjectName;

    public HexagonEdge Edge { get { return this.edgeId; } }

    public bool ActiveEdge { get { return this.isColliderActive; } }

    // Start is called before the first frame update
    void Start()
    {
        this.collider2d = this.GetComponent<Collider2D>();

        if (this.edgeId == HexagonEdge.NotDefined)
            this.edgeId = (HexagonEdge) this.transform.GetSiblingIndex();
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {

        // zaten aktif ise yeni bir etkileşime izin verme
        if (this.isColliderActive)
            return;

        // ilgilenmediği bir collider ise geç
        if (collision.gameObject.tag != "HexaEdge")
            return;

        //Debug.Log("OnTriggerEnter2D() " + this.name + " @ " + this.transform.parent.parent.name + " - With -> " + collision.gameObject.name + " @ " + collision.transform.parent.parent.name);

        this.isColliderActive = true;

        this.linkedObjectName = collision.gameObject.transform.name + " of " + collision.transform.parent.parent.name;

        HexaEdgeCollider _edgeCollider = collision.gameObject.GetComponent<HexaEdgeCollider>();

        if (_edgeCollider != null)
            this.linkedHexagon.edge = _edgeCollider.Edge;
        else
            this.linkedHexagon.edge = (HexagonEdge) collision.transform.GetSiblingIndex();

        this.linkedHexagon.uniqeId = collision.gameObject.GetInstanceID();
        this.linkedHexagon.hexagon = collision.gameObject.GetComponentInParent<IHexagon>();


    }

    private void OnTriggerExit2D(Collider2D collision)
    {

        //Debug.Log("OnTriggerExit2D() " + this.name + " @ " + this.transform.parent.parent.name + " - With -> " + collision.gameObject.name + " @ " + collision.transform.parent.parent.name);

        // ilgilenmediği bir collider ise geç
        if (collision.gameObject.tag != "HexaEdge")
            return;

        // mevcut bağlı edge ise bağlantıyı kaldır
        if ( collision.gameObject.GetInstanceID() == this.linkedHexagon.uniqeId)
        {
            this.isColliderActive = false;

            this.linkedObjectName = "";

            this.linkedHexagon.edge = HexagonEdge.NotDefined;
            this.linkedHexagon.uniqeId = int.MinValue;
            this.linkedHexagon.hexagon = null;
        }

    }

    public LinkedObject LinkedHexagon
    {
        get
        {
            LinkedObject _linkedObject = new LinkedObject();

            _linkedObject.edge = this.linkedHexagon.edge;
            _linkedObject.uniqeId = this.linkedHexagon.uniqeId;
            _linkedObject.hexagon = this.linkedHexagon.hexagon;

            return _linkedObject;

        }
    }

    public void SetColliderEnabledState(bool _state)
    {
        if (this.collider2d != null)
            this.collider2d.enabled = _state;
    }

}
