using System.Diagnostics;
using System.Collections.Concurrent;

using var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5251")
};

const int requests = 4000;
int errCount = 0;
var elapsedTimes = new ConcurrentBag<long>();

var stopwatch = Stopwatch.StartNew();

var tasks = new List<Task>();

for (var i = 0; i < requests; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        var eachRequestSW = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/products");
        eachRequestSW.Stop();

        if ((int)response.StatusCode >= 500)
        {
            Interlocked.Increment(ref errCount);
        }
        else
        {
            elapsedTimes.Add(eachRequestSW.ElapsedMilliseconds);
        }
    }));
}

await Task.WhenAll(tasks);

stopwatch.Stop();

Console.WriteLine();
Console.WriteLine($"Requests: {requests}");
Console.WriteLine($"Error requests: {errCount}");
Console.WriteLine($"Time: {stopwatch.Elapsed}");
Console.WriteLine($"Avg request time: {TimeSpan.FromMilliseconds(elapsedTimes.Average())}");
Console.WriteLine(
    $"RPS: {requests / stopwatch.Elapsed.TotalSeconds:F2}"
);
