using System.Text.Json;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using raft_DJPhelps1;
using RaftRESTAPI;

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


List<INode> otherNodes;


foreach (var n in otherNodesRaw.Split(";");
{
    string[] instances = n.Split(",");
    otherNodes.Add(new NetworkClusterNode(Guid.Parse(instances[0]), instances[1]);
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
      Id: node.Id,
      Status: node.Status,
      ElectionTimeout: node.ElectionTimeout,
      Term: node.CurrentTerm,
      CurrentTermLeader: node.CurrentTermLeader,
      CommittedEntryIndex: node.CommittedEntryIndex,
      Log: node.Log,
      State: node.State,
      TimeoutMultiplier: Node.NodeIntervalScalar
    );
});

app.MapPost("/request/appendEntries", async (AppendDeets request) =>
{
    logger.LogInformation("received append entries request {request}", request);
    await node.RequestAppendEntries(request);
});

app.MapPost("/request/vote", async (VoteDeets request) =>
{
    logger.LogInformation("received vote request {request}", request);
    await node.RequestVote(request);
});

app.MapPost("/response/appendEntries", async (AppendDeets response) =>
{
    logger.LogInformation("received append entries response {response}", response);
    await node.RespondAppendEntries(response);
});

app.MapPost("/response/vote", async (VoteDeets response) =>
{
    logger.LogInformation("received vote response {response}", response);
    await node.ResponseVote(response);
});

app.MapPost("/request/add", async (int data) =>
{
    await node.SendCommand(data);
});

app.Run();