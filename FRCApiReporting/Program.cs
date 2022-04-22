using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace FRCApiReporting
{

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string[] states = { "Minnesota", "North Dakota", "South Dakota" };
        private static readonly string apiKey = "";
        private const int MAX_PAGETOTAL = 1000;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private const int Season = 2022;
        private static readonly string[] DesiredEventTypes = { EventTypes.ChampionshipSubdivision.ToString() };

        static Program() 
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", apiKey);

            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };
        }

        static async Task Main(string[] args)
        {
            var teams = await GetTeams();
            var teamEvents = new Dictionary<Team, List<Event>>();
            var allEvents = new HashSet<Event>();
            foreach (var team in teams.Values)
            {
                teamEvents[team] = new List<Event>(
                    (await GetEventsForTeam(team))
                        .Where(e => DesiredEventTypes.Contains(e.type)));
                foreach (var ev in teamEvents[team])
                {
                    allEvents.Add(ev);
                }
            }

            var allRankings = new Dictionary<Event, List<Ranking>>();
            foreach (var evt in allEvents)
            {
                allRankings[evt] = await GetEventRankings(evt);
            }

            var teamEventRankings = new Dictionary<Team, Dictionary<Event, Ranking>>();
            foreach (var team in teams.Values)
            {
                teamEventRankings[team] = GetTeamRankingForEvents(team, teamEvents[team], allRankings);
            }

            foreach (var team in teams.Values.OrderBy(t => t.teamNumber))
            {
                Console.WriteLine($"Team {team.teamNumber}");
                foreach (var evt in teamEventRankings[team].Keys)
                {
                    var ranking = teamEventRankings[team][evt];
                    Console.WriteLine($"  Event {evt.name}: ranked {ranking.rank} matchesPlayed {ranking.matchesPlayed} {ranking.wins}-{ranking.losses}-{ranking.ties} qual average {ranking.qualAverage}");
                }
            }
        }

        static Dictionary<Event, Ranking> GetTeamRankingForEvents(Team team, List<Event> teamEvents, Dictionary<Event, List<Ranking>> rankings)
        {
            var result = new Dictionary<Event, Ranking>();
            
            foreach (var evt in teamEvents)
            {
                result[evt] = rankings[evt].Where(r => r.teamNumber == team.teamNumber).Single();
            }

            return result;
        }

        static async Task<List<Ranking>> GetEventRankings(Event evt)
        {
            var eventsResult = (RankingsResult)await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/rankings/{evt.code}", typeof(EventsResult), cancellationTokenSource.Token);

            return eventsResult.Rankings.ToList<Ranking>();
        }

        static async Task<Dictionary<int, Team>> GetTeams()
        {
            var result = new Dictionary<int, Team>();

            foreach (string state in states)
            {
                var pageTotal = MAX_PAGETOTAL;
                for (int currentPage = 1; currentPage < pageTotal; currentPage++)
                {
                    var teamsResult = (TeamsResult)await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/teams?page={currentPage}&state={state}", typeof(EventsResult), cancellationTokenSource.Token);
                    if (pageTotal == MAX_PAGETOTAL)
                    {
                        pageTotal = teamsResult.pageTotal;
                    }

                    foreach(var team in teamsResult.teams)
                    {
                        result[team.teamNumber] = team;
                    }
                }
            }

            return result;
        }

        static async Task<List<Event>> GetEventsForTeam(Team team)
        {
            var eventsResult = (EventsResult)await client.GetFromJsonAsync($"https://frc-api.firstinspires.org/v3.0/{Season}/events?teamNumber={team.teamNumber}", typeof(EventsResult), cancellationTokenSource.Token);

            return eventsResult.Events.ToList<Event>();
        }
    }
}
