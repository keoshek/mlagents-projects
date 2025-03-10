using UnityEngine;

public class Flower : MonoBehaviour
{
    public Color fullFlowerColor = new(1f, 0, 0.3f);
    public Color emptyFlowerColor = new(0.5f, 0, 1f);


    [HideInInspector] public Collider nectarCollider;


    private Collider flowerCollider;
    private Material flowerMaterial;


    public Vector3 FlowerUpVector { get { return nectarCollider.transform.up; } }
    public Vector3 FlowerCenterPosition { get { return nectarCollider.transform.position; } }
    public float NectarAmount { get; private set; }
    public bool HasNectar { get { return NectarAmount > 0; } }


    private void Awake()
    {
        flowerMaterial = GetComponent<MeshRenderer>().material;

        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }


    public float Feed(float amount)
    {
         float nectarTaken = Mathf.Clamp(amount, 0, NectarAmount);

        NectarAmount -= nectarTaken;

        if (NectarAmount <= 0) {
            NectarAmount = 0;

            // collider and trigger
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            // flower color
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        return nectarTaken;
    }


    public void ResetFlower()
    {
        NectarAmount = 1f;

        // collider and trigger
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        // flower color
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }
}
