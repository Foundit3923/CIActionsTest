using System.Collections;
using TMPro;
using UnityEngine;

public class SlideOpenClose : Tooltip
{
    [SerializeField] public GameObject DoorFrame;
    [SerializeField] public GameObject LeftDoor;
    [SerializeField] public GameObject RightDoor;
    [SerializeField] public float DoorDelta = 0.03985238f;
    [SerializeField] public float duration;
    [SerializeField] public TMP_Text toolTipText;
    [SerializeField] public string OpenText;
    [SerializeField] public string CloseText;
    [SerializeField] public bool isOpen;
    private Vector3 LeftDoorClosedPos;
    private Vector3 RightDoorClosedPos;
    private Vector3 LeftDoorOpenPos;
    private Vector3 RightDoorOpenPos;
    private bool openingLeft;
    private bool openingRight;

    public override void DoOnTriggerEnter(Collider other)
    {
        return;
    }

    public override void DoOnTriggerExit(Collider other)
    {
        return;
    }

    public override void DoOnTriggerStay(Collider other)
    {
        return;
    }

    public override void PlayerInteraction(string button, GameObject player)
    {
        float delta = DoorDelta;
        StartCoroutine(SlideLeftObject(LeftDoor, delta, duration));
        StartCoroutine(SlideRightObject(RightDoor, delta, duration));
        isOpen = isOpen ? false : true;
        return;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        openingLeft = false;
        openingRight = false;
        isOpen = false;

        LeftDoorClosedPos = LeftDoor.transform.localPosition;
        RightDoorClosedPos = RightDoor.transform.localPosition;

        LeftDoorOpenPos = new Vector3(LeftDoorClosedPos.x, LeftDoorClosedPos.y * DoorDelta, LeftDoorClosedPos.z);
        RightDoorOpenPos = new Vector3(RightDoorClosedPos.x, RightDoorClosedPos.y * DoorDelta, RightDoorClosedPos.z);

        //    Vector3 direction = Vector3.zero;

        //    Vector3 referencePos = DoorFrame.transform.localPosition; // new Vector3(DoorFrame.transform.localPosition.x, LeftDoor.transform.localPosition.y, DoorFrame.transform.localPosition.z);

        //    Vector3 heading = referencePos - LeftDoorClosedPos ;

        //    float distance = heading.magnitude;

        //    direction = heading / distance;

        //    Vector3 magnitude = direction * DoorDelta;

        //    direction += magnitude;

        //    LeftDoorOpenPos = RightDoorClosedPos + direction;

        //    direction = Vector3.zero;

        //    referencePos = new Vector3(DoorFrame.transform.localPosition.x, RightDoor.transform.localPosition.y, DoorFrame.transform.localPosition.z);

        //    heading = RightDoorClosedPos - referencePos;

        //    distance = heading.magnitude;

        //    direction = heading / distance;

        //    magnitude = direction * DoorDelta;

        //    direction += magnitude;

        //    RightDoorOpenPos = RightDoorClosedPos + direction;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator SlideLeftObject(GameObject objectToSlide, float delta, float duration)

    {
        if (openingLeft)
        {
            yield break;
        }

        openingLeft = true;

        Vector3 from = Vector3.zero;

        Vector3 to = Vector3.zero;

        if (isOpen)
        {
            from = LeftDoorOpenPos;

            to = LeftDoorClosedPos;
        }
        else
        {
            from = LeftDoorClosedPos;

            to = LeftDoorOpenPos;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            objectToSlide.transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        openingLeft = false;
    }

    private IEnumerator SlideRightObject(GameObject objectToSlide, float delta, float duration)

    {
        if (openingRight)
        {
            yield break;
        }

        openingRight = true;

        Vector3 from = Vector3.zero;

        Vector3 to = Vector3.zero;

        if (isOpen)
        {
            from = RightDoorOpenPos;

            to = RightDoorClosedPos;
        }
        else
        {
            from = RightDoorClosedPos;

            to = RightDoorOpenPos;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            objectToSlide.transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        openingRight = false;
    }
}
