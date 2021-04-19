using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of plants and its flowers
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //The diameter of the area where the agent and flowers can be used for observing relative distance from agent to flower
    public const float AREA_DIAMETER = 20f;

    //The list of all plants within this area
    private List<GameObject> _flowerPlants;

    private Dictionary<Collider, Flower> _nectarFlowerDictionary;

    /// <summary>
    /// The list containing all flowers in the area.
    /// </summary>
    public List<Flower> Flowers { get; private set; }


    public void ResetFlowers()
    {
        //Sets rotation for plants
        _flowerPlants.ForEach(c => c.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f),
                                                                                UnityEngine.Random.Range(-180f, 180f),
                                                                                UnityEngine.Random.Range(-5f, 5f)));
        //Resets all flowers
        Flowers.ForEach(c => c.ResetFlower());
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to.
    /// </summary>
    /// <param name="collider">The nectar collider</param>
    /// <returns>The matching flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return _nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        _flowerPlants = new List<GameObject>();
        _nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        FindChildFlowers(transform);
    }

    /// <summary>
    /// Recursively finds all flowers and plants that are children of a paraent transform
    /// </summary>
    /// <param name="parent">The parent of the children to look at</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                _flowerPlants.Add(child.gameObject);

                FindChildFlowers(child);
            }
            else
            {
                if(child.TryGetComponent(out Flower flower))
                {
                    Flowers.Add(flower);

                    _nectarFlowerDictionary.Add(flower.NectarCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);
                }
            }
        }
    }
}
