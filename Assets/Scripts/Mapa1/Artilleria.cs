using System.Collections;
using UnityEngine;

public class Artilleria : MonoBehaviour
{
    public GameObject[] artPoint;

    //tiempo de activo que tiene un objeto
    public float aftExplosion;

    //Entre tiempo de cada explosion
    public float TimeBetween = 1f;

    //Pueden detonar dos explosiones simultaneas
    public bool allowRepeat = false;

    private int lastIndex = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (artPoint.Length == 0)
        {
            Debug.LogWarning("No hay objetos en el array.");
            return;
        }
        StartCoroutine(ActivateLoop());
    }

    IEnumerator ActivateLoop()
    {
        while (true)
            {
            yield return new WaitForSeconds(TimeBetween);

            int index = GetRandomIndex();
            lastIndex = index;

            GameObject obj = artPoint[index];
            obj.SetActive(true);

            yield return new WaitForSeconds(aftExplosion);

            obj.SetActive(false);
            }
    }
    int GetRandomIndex()
    {
        if (artPoint.Length == 1 || allowRepeat)
            return Random.Range(0, artPoint.Length);

        int index = 0;
        do
        {
            index = Random.Range(0, artPoint.Length);
        }
        while (index == lastIndex);

        return index;
    }
}
