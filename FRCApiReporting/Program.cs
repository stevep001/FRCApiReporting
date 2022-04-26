using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace FRCApiReporting
{

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string[] states = { "Minnesota", "North Dakota", "South Dakota" };
        //private static readonly string[] states = { "North Dakota", "South Dakota" };
        private static readonly string apiKey = "";
        private const int MAX_PAGETOTAL = 1000;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private const int Season = 2022;
        private static readonly string[] DesiredEventTypes = { EventTypes.ChampionshipDivision.ToString(), EventTypes.Championship.ToString()};

        static Program() 
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"secrets.json");
            using (var input = new FileStream(path, FileMode.Open)) 
            {
                var secrets = (SecretsConfig)JsonSerializer.Deserialize(input, typeof(SecretsConfig));
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", secrets.AsBase64Token());
            }

            DiagnosticListener.AllListeners.Subscribe(new HttpClientListener());
            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };
        }

        static async Task Main(string[] args)
        {
            var teams = await GetTeams();
            var teamEvents = new Dictionary<int, List<Event>>();
            var allEvents = new List<Event>();
            foreach (var team in teams.Values)
            {
                var events = await GetEventsForTeam(team);
                teamEvents[team.teamNumber] = new List<Event>(events.Where(e => DesiredEventTypes.Contains(e.type)));
                foreach (var ev in teamEvents[team.teamNumber])
                {
                    if (!allEvents.Where(e => e.code == ev.code).Any())
                    {
                        allEvents.Add(ev);
                    }
                }
            }

            var allRankings = new Dictionary<string, List<Ranking>>();
            foreach (var evt in allEvents)
            {
                allRankings[evt.code] = await GetEventRankings(evt);
            }

            var teamEventRankings = new Dictionary<int, Dictionary<Event, Ranking>>();
            foreach (var team in teams.Values)
            {
                teamEventRankings[team.teamNumber] = GetTeamRankingForEvents(team, teamEvents[team.teamNumber], allRankings);
            }

            var teamAwards = new Dictionary<int, List<Award>>();
            foreach (var evt in allEvents)
            {
                var awards = await GetAwardsForEvent(evt);
                foreach (var award in awards)
                {
                    if (award.teamNumber.HasValue)
                    {
                        if (teams.ContainsKey(award.teamNumber.Value))
                        {
                            Team teamWithAward;
                            if (teams.TryGetValue(award.teamNumber.Value, out teamWithAward))
                            {
                                if (!teamAwards.ContainsKey(teamWithAward.teamNumber))
                                {
                                    teamAwards[teamWithAward.teamNumber] = new List<Award>();
                                }
                                teamAwards[teamWithAward.teamNumber].Add(award);
                            }
                        }
                    }
                }
            }

            var allAlliances = new Dictionary<string, List<Alliance>>();
            foreach (var evt in allEvents)
            {
                allAlliances[evt.code] = await GetAlliancesForEvent(evt);
            }

            foreach (var team in teams.Values.OrderBy(t => t.teamNumber))
            {
                var showedTeam = false;
                foreach (var evt in teamEvents[team.teamNumber])
                {
                    if (!showedTeam)
                    {
                        Console.WriteLine($"Team {team.teamNumber} - {team.nameShort} ({team.schoolName} in {team.city}, {team.stateProv})");
                        showedTeam = true;
                    }

                    Ranking ranking;
                    if (teamEventRankings[team.teamNumber].TryGetValue(evt, out ranking))
                    {
                        Console.Write($"  {evt.name}: ranked {ranking.rank} with a {ranking.wins}-{ranking.losses}-{ranking.ties} record");
                        var alliance = GetAllianceForTeam(team, allAlliances[evt.code]);
                        if (alliance.HasValue)
                        {
                            Console.Write($", and member of Alliance {alliance.Value.Item1} as {alliance.Value.Item2}");
                        }

                        Console.WriteLine(".");
                    }


                    if (teamAwards.ContainsKey(team.teamNumber))
                    {
                        foreach (var award in teamAwards[team.teamNumber])
                        {
                            if (award.eventCode == evt.code)
                            {
                                Console.WriteLine($"    Award: {award.name} {award.person} {award.series}");
                            }
                        }
                    }
                }
            }
        }

        static (int, string)? GetAllianceForTeam(Team team, List<Alliance> alliances)
        {
            foreach (var alliance in alliances)
            {
                if (alliance.captain == team.teamNumber)
                {
                    return (alliance.number, "captain");
                }
                else if (alliance.round1 == team.teamNumber)
                {
                    return (alliance.number, "1st pick");
                }
                else if (alliance.round2 == team.teamNumber)
                {
                    return (alliance.number, "2nd pick");
                }
                else if (alliance.round3 == team.teamNumber)
                {
                    return (alliance.number, "3rd pick");
                }
            }

            return null;
        }

        static Dictionary<Event, Ranking> GetTeamRankingForEvents(Team team, List<Event> teamEvents, Dictionary<string, List<Ranking>> rankings)
        {
            var result = new Dictionary<Event, Ranking>();
            
            foreach (var evt in teamEvents)
            {
                // Exclude Einstein for rankings since no teams this year
                if (rankings.ContainsKey(evt.code))
                {
                    var teamRanking = rankings[evt.code].Where(r => r.teamNumber == team.teamNumber).SingleOrDefault();
                    if (teamRanking != null)
                    {
                        result[evt] = teamRanking;
                    }
                }
            }

            return result;
        }

        static async Task<List<Ranking>> GetEventRankings(Event evt)
        {
            var eventsResult = (RankingsResult) await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/rankings/{evt.code}", typeof(RankingsResult), cancellationTokenSource.Token);

            return eventsResult.Rankings.ToList<Ranking>();
        }

        static async Task<Dictionary<int, Team>> GetTeams()
        {
            var result = new Dictionary<int, Team>();

            var expectedTeamsCount = 0;
            foreach (string state in states)
            {
                var pageTotal = MAX_PAGETOTAL;
                for (int currentPage = 1; currentPage <= pageTotal; currentPage++)
                {
                    var teamsResult = (TeamsResult)await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/teams?page={currentPage}&state={state}", typeof(TeamsResult), cancellationTokenSource.Token);
                    if (pageTotal == MAX_PAGETOTAL)
                    {
                        pageTotal = teamsResult.pageTotal;
                        expectedTeamsCount += teamsResult.teamCountTotal;
                    }

                    foreach(var team in teamsResult.teams)
                    {
                        result[team.teamNumber] = team;
                    }
                }
            }

            if (expectedTeamsCount != result.Count)
            {
                throw new Exception($"Team count inconsistency:  expected {expectedTeamsCount}, actual {result.Count}");
            }

            return result;
        }

        static async Task<List<Event>> GetEventsForTeam(Team team)
        {
            var eventsResult = (EventsResult) await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/events?teamNumber={team.teamNumber}", typeof(EventsResult), cancellationTokenSource.Token);

            return eventsResult.Events.ToList<Event>();
        }

        static async Task<List<Award>> GetAwardsForEvent(Event evt)
        {
            var awardsResult = (AwardsResult) await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/awards/event/{evt.code}", typeof(AwardsResult), cancellationTokenSource.Token);

            return awardsResult.Awards.ToList<Award>();
        }

        static async Task<List<Alliance>> GetAlliancesForEvent(Event evt)
        {
            var alliancesResult = (AlliancesResult)await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/alliances/{evt.code}", typeof(AlliancesResult), cancellationTokenSource.Token);

            return alliancesResult.Alliances.ToList<Alliance>();
        }
    }
}
