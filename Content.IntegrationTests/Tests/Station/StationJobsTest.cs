using System.Collections.Generic;
using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Station;

[TestFixture]
[TestOf(typeof(StationJobsSystem))]
public sealed class StationJobsTest
{
    private const string StationMapId = "FooStation";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: playTimeTracker
  id: PlayTimeDummyAssistant

- type: playTimeTracker
  id: PlayTimeDummyMime

- type: playTimeTracker
  id: PlayTimeDummyClown

- type: playTimeTracker
  id: PlayTimeDummyCaptain

- type: playTimeTracker
  id: PlayTimeDummyChaplain

- type: gameMap
  id: {StationMapId}
  minPlayers: 0
  mapName: {StationMapId}
  mapPath: /Maps/Test/empty.yml
  stations:
    Station:
      mapNameTemplate: {StationMapId}
      stationProto: StandardNanotrasenStation
      components:
        - type: StationJobs
          availableJobs:
            TMime: [0, -1]
            TAssistant: [-1, -1]
            TCaptain: [5, 5]
            TClown: [5, 6]

- type: job
  id: TAssistant
  playTimeTracker: PlayTimeDummyAssistant

- type: job
  id: TMime
  weight: 20
  playTimeTracker: PlayTimeDummyMime

- type: job
  id: TClown
  weight: -10
  playTimeTracker: PlayTimeDummyClown

- type: job
  id: TCaptain
  weight: 10
  playTimeTracker: PlayTimeDummyCaptain

- type: job
  id: TChaplain
  playTimeTracker: PlayTimeDummyChaplain
";

    private const int StationCount = 100;
    private const int CaptainCount = StationCount;
    private const int PlayerCount = 2000;
    private const int TotalPlayers = PlayerCount + CaptainCount;

    [Test]
    public async Task AssignJobsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var fooStationProto = prototypeManager.Index<GameMapPrototype>(StationMapId);
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();
        var logmill = server.ResolveDependency<ILogManager>().RootSawmill;

        List<EntityUid> stations = new();
        await server.WaitPost(() =>
        {
            for (var i = 0; i < StationCount; i++)
            {
                stations.Add(stationSystem.InitializeNewStation(fooStationProto.Stations["Station"], null, $"Foo {StationCount}"));
            }
        });

        await server.WaitAssertion(() =>
        {
            var fakePlayers = new Dictionary<NetUserId, HumanoidCharacterProfile>()
                .AddJob("TAssistant", JobPriority.Medium, PlayerCount)
                .AddPreference("TClown", JobPriority.Low)
                .AddPreference("TMime", JobPriority.High)
                .WithPlayers(
                    new Dictionary<NetUserId, HumanoidCharacterProfile>()
                    .AddJob("TCaptain", JobPriority.High, CaptainCount)
                );
            Assert.That(fakePlayers, Is.Not.Empty);

            var start = new Stopwatch();
            start.Start();
            var assigned = stationJobs.AssignJobs(fakePlayers, stations);
            Assert.That(assigned, Is.Not.Empty);
            var time = start.Elapsed.TotalMilliseconds;
            logmill.Info($"Took {time} ms to distribute {TotalPlayers} players.");

            Assert.Multiple(() =>
            {
                foreach (var station in stations)
                {
                    var assignedHere = assigned
                        .Where(x => x.Value.Item2 == station)
                        .ToDictionary(x => x.Key, x => x.Value);

                    // Each station should have SOME players.
                    Assert.That(assignedHere, Is.Not.Empty);
                    // And it should have at least the minimum players to be considered a "fair" share, as they're all the same.
                    Assert.That(assignedHere, Has.Count.GreaterThanOrEqualTo(TotalPlayers / stations.Count), "Station has too few players.");
                    // And it shouldn't have ALL the players, either.
                    Assert.That(assignedHere, Has.Count.LessThan(TotalPlayers), "Station has too many players.");
                    // And there should be *A* captain, as there's one player with captain enabled per station.
                    Assert.That(assignedHere.Where(x => x.Value.Item1 == "TCaptain").ToList(), Has.Count.EqualTo(1));
                }

                // All clown players have assistant as a higher priority.
                Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Not.Contain("TClown"));
                // Mime isn't an open job-slot at round-start.
                Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Not.Contain("TMime"));
                // All players have slots they can fill.
                Assert.That(assigned.Values, Has.Count.EqualTo(TotalPlayers), $"Expected {TotalPlayers} players.");
                // There must be assistants present.
                Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Contain("TAssistant"));
                // There must be captains present, too.
                Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Contain("TCaptain"));
            });
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AdjustJobsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var fooStationProto = prototypeManager.Index<GameMapPrototype>(StationMapId);
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();

        var station = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            station = stationSystem.InitializeNewStation(fooStationProto.Stations["Station"], null, $"Foo Station");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            // Verify jobs are/are not unlimited.
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.IsJobUnlimited(station, "TAssistant"), "TAssistant is expected to be unlimited.");
                Assert.That(stationJobs.IsJobUnlimited(station, "TMime"), "TMime is expected to be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TCaptain"), "TCaptain is expected to not be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TClown"), "TClown is expected to not be unlimited.");
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TClown", 0), "Could not set TClown to have zero slots.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TClown", out var clownSlots), "Could not get the number of TClown slots.");
                Assert.That(clownSlots, Is.EqualTo(0));
                Assert.That(!stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999), "Was able to adjust TCaptain by -9999 without clamping.");
                Assert.That(stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999, false, true), "Could not adjust TCaptain by -9999.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TCaptain", out var captainSlots), "Could not get the number of TCaptain slots.");
                Assert.That(captainSlots, Is.EqualTo(0));
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TChaplain", 10, true), "Could not create 10 TChaplain slots.");
                stationJobs.MakeJobUnlimited(station, "TChaplain");
                Assert.That(stationJobs.IsJobUnlimited(station, "TChaplain"), "Could not make TChaplain unlimited.");
            });
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task InvalidRoundstartJobsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();
        var name = compFact.GetComponentName<StationJobsComponent>();

        await server.WaitAssertion(() =>
        {
            // invalidJobs contains all the jobs which can't be set for preference:
            // i.e. all the jobs that shouldn't be available round-start.
            var invalidJobs = new HashSet<string>();
            foreach (var job in prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (!job.SetPreference)
                    invalidJobs.Add(job.ID);
            }

            Assert.Multiple(() =>
            {
                foreach (var gameMap in prototypeManager.EnumeratePrototypes<GameMapPrototype>())
                {
                    foreach (var (stationId, station) in gameMap.Stations)
                    {
                        if (!station.StationComponentOverrides.TryGetComponent(name, out var comp))
                            continue;

                        foreach (var (job, array) in ((StationJobsComponent) comp).SetupAvailableJobs)
                        {
                            Assert.That(array.Length, Is.EqualTo(2));
                            Assert.That(array[0] is -1 or >= 0);
                            Assert.That(array[1] is -1 or >= 0);
                            Assert.That(invalidJobs, Does.Not.Contain(job), $"Station {stationId} contains job prototype {job} which cannot be present roundstart.");
                        }
                    }
                }
            });
        });
        await pair.CleanReturnAsync();
    }

    private static readonly string[] GameMaps =
    [
        "Bagel",
        "Box",
        "Elkridge",
        "Fland",
        "Marathon",
        "Oasis",
        "Packed",
        "Plasma",
        "Relic",
        "Snowball",
        "Reach",
        "Exo",
    ];

    private Dictionary<ProtoId<JobPrototype>, ProtoId<DepartmentPrototype>> DepartmentsByJob(IPrototypeManager proto)
    {
        var departmentsByJob = new Dictionary<ProtoId<JobPrototype>, ProtoId<DepartmentPrototype>>();
        foreach (var department in proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (!department.Primary)
                continue;

            foreach (var job in department.Roles)
            {
                departmentsByJob[job] = department.ID;
            }
        }

        return departmentsByJob;
    }

    [Test]
    [TestCaseSource(nameof(GameMaps))]
    public async Task JobInterleavingTest(string mapId)
    {
        await using var pair = await PoolManager.GetServerClient();

        await pair.Server.WaitIdleAsync();

        var proto = pair.Server.ResolveDependency<IPrototypeManager>();
        var componentFactory = pair.Server.ResolveDependency<IComponentFactory>();
        var departmentsByJob = DepartmentsByJob(proto);

        var map = proto.Index<GameMapPrototype>(mapId);
        foreach (var station in map.Stations.Values)
        {
            if (!station.StationComponentOverrides.TryGetComponent<StationJobsComponent>(componentFactory, out var stationJobs))
                continue;

            var jobCountsByDepartment = new Dictionary<ProtoId<DepartmentPrototype>, int>();
            var jobsToSelect = stationJobs.SetupAvailableJobs.Keys;
            foreach (var job in jobsToSelect)
            {
                if (!departmentsByJob.TryGetValue(job, out var department))
                    continue;

                jobCountsByDepartment[department] = jobCountsByDepartment.GetValueOrDefault(department) + 1;
            }

            await pair.Server.WaitAssertion(() =>
            {
                var jobs = pair.Server.System<StationJobsSystem>();
                jobs.InitializeRoundStart();
                var interleavedJobs = jobs.Interleaved(jobsToSelect).ToList();
                TestContext.Out.WriteLine($"Station {station.StationPrototype} has the following jobs: {string.Join(", ", interleavedJobs.Select(it => it.Id))}");

                for (var i = 0; i < interleavedJobs.Count; i++)
                {
                    var job = interleavedJobs[i];
                    if (!departmentsByJob.TryGetValue(job, out var department))
                        continue;

                    var expectedLastIndex = jobCountsByDepartment[department] * jobCountsByDepartment.Count;
                    Assert.That(expectedLastIndex, Is.GreaterThanOrEqualTo(i), $"all jobs in department {department} should have been seen before {expectedLastIndex}, but {job} was found at {i}");
                }
            });
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    [TestCaseSource(nameof(GameMaps))]
    public async Task PigeonholeTest(string mapId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var proto = server.ResolveDependency<IPrototypeManager>();
        var random = server.ResolveDependency<IRobustRandom>();
        var map = proto.Index<GameMapPrototype>(mapId);
        var stationSystem = server.System<StationSystem>();
        var stationJobsSystem = server.System<StationJobsSystem>();
        var componentFactory = pair.Server.ResolveDependency<IComponentFactory>();
        var departmentsByJob = DepartmentsByJob(proto);

        var stations = new List<(StationConfig, EntityUid)>();
        await server.WaitPost(() =>
        {
            foreach (var config in map.Stations.Values)
            {
                stations.Add((config, stationSystem.InitializeNewStation(config, null)));
            }
        });

        await server.WaitAssertion(() =>
        {
            foreach (var (station, entity) in stations)
            {
                if (!station.StationComponentOverrides.TryGetComponent<StationJobsComponent>(componentFactory, out var stationJobs))
                    continue;

                var jobsByDepartment = new Dictionary<ProtoId<DepartmentPrototype>, List<ProtoId<JobPrototype>>>();
                foreach (var job in stationJobs.SetupAvailableJobs.Keys)
                {
                    if (!departmentsByJob.TryGetValue(job, out var department))
                        continue;

                    // We can only enforce the pigeonhole invariant for jobs that are all the same weight.
                    // Most jobs are 0-weighted, so...
                    if (proto.Index(job).Weight != 0)
                        continue;

                    jobsByDepartment[department] = jobsByDepartment.GetValueOrDefault(department) ?? [];
                    jobsByDepartment[department].Add(job);
                }

                var fakePlayers = new Dictionary<NetUserId, HumanoidCharacterProfile>();

                var allJobs = new HashSet<ProtoId<JobPrototype>>();
                foreach (var departmentJobs in jobsByDepartment.Values)
                {
                    var expectedJob = random.Pick(departmentJobs);
                    var anotherJob = random.Pick(random.Pick(jobsByDepartment).Value);
                    allJobs.Add(expectedJob);
                    allJobs.Add(anotherJob);
                    fakePlayers[new NetUserId(Guid.NewGuid())] = HumanoidCharacterProfile.Random()
                        .WithJobPriority(expectedJob, JobPriority.High)
                        .WithJobPriority(anotherJob, JobPriority.Medium)
                        .WithJobPriority(SharedGameTicker.FallbackOverflowJob, JobPriority.Never)
                        .WithPreferenceUnavailable(PreferenceUnavailableMode.StayInLobby);
                }

                var assigned = stationJobsSystem.AssignJobs(fakePlayers, [entity]);

                TestContext.Out.WriteLine($"Station {station.StationPrototype} on map {mapId} wants to evenly balance the following departments: {string.Join(", ", jobsByDepartment.Keys.OrderBy(it => it.Id))}");
                TestContext.Out.WriteLine($"Station {station.StationPrototype} on map {mapId} is allocating to the following jobs: {string.Join(", ", allJobs.OrderBy(it => it.Id))}");
                TestContext.Out.WriteLine($"Station {station.StationPrototype} on map {mapId} has the following jobs assigned: {string.Join(", ", assigned.Values.Select(it => it.Item1?.Id ?? "BadJob").Order())}");

                foreach (var (department, jobs) in jobsByDepartment)
                {
                    Assert.That(assigned.Values.Any(assignment => assignment.Item1 is { } job && jobs.Contains(job)), $"Department {department} was not assigned a job in {station.StationPrototype} station on {mapId} map even though it should've been possible to");
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}

internal static class JobExtensions
{
    public static Dictionary<NetUserId, HumanoidCharacterProfile> AddJob(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp, string jobId, JobPriority prio = JobPriority.Medium,
        int amount = 1)
    {
        for (var i = 0; i < amount; i++)
        {
            inp.Add(new NetUserId(Guid.NewGuid()), HumanoidCharacterProfile.Random().WithJobPriority(jobId, prio));
        }

        return inp;
    }

    public static Dictionary<NetUserId, HumanoidCharacterProfile> AddPreference(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp, string jobId, JobPriority prio = JobPriority.Medium)
    {
        return inp.ToDictionary(x => x.Key, x => x.Value.WithJobPriority(jobId, prio));
    }

    public static Dictionary<NetUserId, HumanoidCharacterProfile> WithPlayers(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp,
        Dictionary<NetUserId, HumanoidCharacterProfile> second)
    {
        return new[] { inp, second }.SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);
    }
}
