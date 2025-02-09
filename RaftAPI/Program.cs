using System.Text.Json;
using System.Xml.Linq;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using raft_DJPhelps1;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");


var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

builder.Services.AddLogging();
var serviceName = "Node" + nodeId;
builder.Logging.AddOpenTelemetry(options =>
{
    options
      .SetResourceBuilder(
          ResourceBuilder
            .CreateDefault()
            .AddService(serviceName)
      )
      .AddOtlpExporter(options =>
      {
          options.Endpoint = new Uri("http://dashboard:18889");
      });
});
var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Node ID {name}", nodeId);
logger?.LogInformation("Other nodes environment config: {}", otherNodesRaw);


List<INode> otherNodes = new();


foreach (var n in otherNodesRaw.Split(";"))
{
    string[] instances = n.Split(",");
    otherNodes.Add(
        new NetworkClusterNode(
            Guid.Parse(instances[0]), 
            instances[1]
            )
        );
}


logger?.LogInformation("other nodes {nodes}", JsonSerializer.Serialize(otherNodes));


var node = new Node();
node.Id = Guid.Parse(nodeId);

node.TimeoutMultiplier = int.Parse(nodeIntervalScalarRaw);
foreach(var o in otherNodes)
{
    Console.WriteLine($"Node {o} added to Node {node.Id}'s list");
    node.Nodes.Add(o.Id, o);
}

node.Start();

app.MapGet("/health", () => "healthy");

app.MapGet("/nodedata", () =>
{
    return new NodeData(
    Node_Id: node.Id,
    Status: node.IsStarted,
    ElectionTimer: node.ElectionTimerCurr,
    Term: node.Term,
    CurrentTermLeader: node.CurrentLeader,
    NextIndex: node.NextIndex,
    LogIndex: node.LogIndex,
    Log: node.CommandLog,
    State: node.State,
    TimeoutMultiplier: node.TimeoutMultiplier,
    Heartbeat: node.Heartbeat,
    ElectionTimeout: node.ElectionTimerCurr,
    StateMachineVal: node.ImportantValue
    );
});

app.MapPost("/request/appendEntries", async (AppendDeets request) =>
{
    logger?.LogInformation("received append entries request {request}", request);
    await node.AppendEntriesRPC(request.ID, request.CT);
});

app.MapPost("/request/vote", async (VoteDeets request) =>
{
    logger?.LogInformation("received vote request {request}", request);
    await node.RequestVoteRPC(request.ID, request.TERM);
});

app.MapPost("/response/appendEntries", async (AppendDeets response) =>
{
    logger?.LogInformation("received append entries response {response}", response);
    await node.AppendResponseRPC(response.ID, response.VLF, response.CT);
});

app.MapPost("/response/vote", async (VoteDeets response) =>
{
    logger?.LogInformation("received vote response {response}", response);
    await node.RespondVoteRPC(response.ID, response.TERM, response.VOTE);
});

app.MapPost("/request/clockupdate", (ClockPacingToken token) =>
{
    logger?.LogInformation("Received request to update the clock.");
    node.TimeoutMultiplier = token.TimeScaleMultiplier;
    node.InternalDelay = token.DelayValue;
});

app.MapPost("/request/start", (bool startflag) =>
{
    logger?.LogInformation("Received request to toggle operation for {}", nodeId);
    if(startflag)
        node.Start();
    else node.Stop();
});

app.MapPost("/request/add", async (int data) =>
{
    await node.RequestAdd(data);
});

app.Run();