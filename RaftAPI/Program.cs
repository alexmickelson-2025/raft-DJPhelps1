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
logger.LogInformation("Node ID {name}", nodeId);
logger.LogInformation("Other nodes environment config: {}", otherNodesRaw);


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


logger.LogInformation("other nodes {nodes}", JsonSerializer.Serialize(otherNodes));


var node = new Node()
{
    Id = Guid.Parse(nodeId),
};

node.TimeoutMultiplier = int.Parse(nodeIntervalScalarRaw);

node.Start();

app.MapGet("/health", () => "healthy");

app.MapGet("/nodeData", () =>
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
    TimeoutMultiplier: node.TimeoutMultiplier
    );
});

app.MapPost("/request/appendEntries", async (AppendDeets request) =>
{
    logger.LogInformation("received append entries request {request}", request);
    await node.AppendEntriesRPC(request.id, request.ct);
});

app.MapPost("/request/vote", async (VoteDeets request) =>
{
    logger.LogInformation("received vote request {request}", request);
    await node.RequestVoteRPC(request.Id, request.Term);
});

app.MapPost("/response/appendEntries", async (AppendDeets response) =>
{
    logger.LogInformation("received append entries response {response}", response);
    await node.AppendResponseRPC(response.id, response.vlf, response.ct);
});

app.MapPost("/response/vote", async (VoteDeets response) =>
{
    logger.LogInformation("received vote response {response}", response);
    await node.ReceiveVoteRPC(response.Id, response.Term, response.Vote);
});

app.MapPost("/request/add", async (int data) =>
{
    await node.RequestAdd(data);
});

app.Run();