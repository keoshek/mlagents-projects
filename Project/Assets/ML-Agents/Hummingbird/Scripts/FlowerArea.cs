using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    public const float AreaDiameter = 20f;


    private List<GameObject> flowerPlants;
    private Dictionary<Collider, Flower> nectarFlowerDictionary;
    
    
    public List<Flower> Flowers { get; private set; }


    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }


    private void Start()
    {
        FindChildFlowers(transform);
    }


    public void ResetFlowers()
    {
        // rotate flower plants
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        // reset each flower
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }


    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }


    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant")) {
                flowerPlants.Add(child.gameObject);

                FindChildFlowers(child);
            } else {
                if (child.TryGetComponent(out Flower flower)) {
                    Flowers.Add(flower);

                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else {
                    FindChildFlowers(child);
                }
            }
        }
    }
}
