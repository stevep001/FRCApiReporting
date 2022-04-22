using System;

public class EventsResult
{
    public Event[] Events { get; set; }
    public int eventCount { get; set; }
}

public class Event
{
    public string address { get; set; }
    public string website { get; set; }
    public object[] webcasts { get; set; }
    public string timezone { get; set; }
    public string code { get; set; }
    public object divisionCode { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string districtCode { get; set; }
    public string venue { get; set; }
    public string city { get; set; }
    public string stateprov { get; set; }
    public string country { get; set; }
    public DateTime dateStart { get; set; }
    public DateTime dateEnd { get; set; }
}

public class TeamsResult
{
    public Team[] teams { get; set; }
    public int teamCountTotal { get; set; }
    public int teamCountPage { get; set; }
    public int pageCurrent { get; set; }
    public int pageTotal { get; set; }
}

public class Team
{
    public string schoolName { get; set; }
    public string website { get; set; }
    public string homeCMP { get; set; }
    public int teamNumber { get; set; }
    public string nameFull { get; set; }
    public string nameShort { get; set; }
    public string city { get; set; }
    public string stateProv { get; set; }
    public string country { get; set; }
    public int rookieYear { get; set; }
    public string robotName { get; set; }
    public object districtCode { get; set; }
}


public class RankingsResult
{
    public Ranking[] Rankings { get; set; }
}

public class Ranking
{
    public int rank { get; set; }
    public int teamNumber { get; set; }
    public float sortOrder1 { get; set; }
    public int sortOrder2 { get; set; }
    public int sortOrder3 { get; set; }
    public int sortOrder4 { get; set; }
    public int sortOrder5 { get; set; }
    public int sortOrder6 { get; set; }
    public int wins { get; set; }
    public int losses { get; set; }
    public int ties { get; set; }
    public float qualAverage { get; set; }
    public int dq { get; set; }
    public int matchesPlayed { get; set; }
}