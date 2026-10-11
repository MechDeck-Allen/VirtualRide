using System;
using UnityEngine;

[Serializable]
public class RoutePoint
{
    public double latitude;
    public double longitude;
    public double elevation;
    public double distanceFromStart;
}

[Serializable]
public class RouteData
{
    public int version;
    public string routeName;
    public string source;
    public double distanceKm;
    public int pointCount;

    public ElevationData elevation;

    public RoutePoint[] points;
}

[Serializable]
public class ElevationData
{
    public double minimumMeters;
    public double maximumMeters;
}