using System;
using UnityEngine;

public class RatingConfigCollection : ScriptableObject
{
    public RatingConfigModel dataGroup;
}

[Serializable]
public class RatingConfigModel
{
    public long timeNextRating;
}