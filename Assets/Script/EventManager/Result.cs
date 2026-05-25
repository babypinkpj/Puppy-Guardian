using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result : MonoBehaviour
{
    public static List<int> itemlist = new List<int>();
    public static List<int> itemstolen = new List<int>();
    public static List<int> itembroken = new List<int>();

    public static void RestartResult()
    {
        itemlist.Clear(); itembroken.Clear(); itemstolen.Clear();
    }
}
