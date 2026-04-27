using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using HttpRequestMonitor.Models;

namespace HttpRequestMonitor.Services;

public sealed class HttpServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConcurrentDictionary<Guid, Message> _messages = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private DateTime _startedAt;
    private long _totalRequests;
    private long _getRequests;
    private long _postRequests;
    private long _totalProcessingTicks;

    public event Func<LogEntry, Task>? RequestLogged;

    public bool IsRunning => _listener?.IsListening == true;

    public void Start(int port)
    {
        if (IsRunning)
        {
            return;
        }

        ResetState();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");

        try
        {
            listener.Start();
            _listener = listener;
            _cancellationTokenSource = new CancellationTokenSource();
            _startedAt = DateTime.Now;

            _ = Task.Run(() => ListenAsync(listener, _cancellationTokenSource.Token));
        }
        catch
        {
            listener.Close();
            throw;
        }
    }

    public void Stop()
    {
        if (_listener is null)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
        }

        _listener = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private async Task ListenAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            try
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context));
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var response = context.Response;
        var requestBody = string.Empty;
        var responseBody = string.Empty;
        var statusCode = (int)HttpStatusCode.OK;

        try
        {
            Interlocked.Increment(ref _totalRequests);
            if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _getRequests);
                responseBody = CreateStatusResponse();
            }
            else if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _postRequests);
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                requestBody = await reader.ReadToEndAsync();
                (statusCode, responseBody) = ProcessPostBody(requestBody);
            }
            else
            {
                statusCode = (int)HttpStatusCode.MethodNotAllowed;
                responseBody = JsonSerializer.Serialize(new { error = "Supported methods: GET, POST." }, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            responseBody = JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
        finally
        {
            stopwatch.Stop();
            Interlocked.Add(ref _totalProcessingTicks, stopwatch.ElapsedTicks);

            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json; charset=utf-8";

                var buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);
            }
            catch (HttpListenerException)
            {
                statusCode = 499;
            }
            catch (ObjectDisposedException)
            {
                statusCode = 499;
            }
            finally
            {
                try
                {
                    response.OutputStream.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            await PublishLogAsync(new LogEntry
            {
                Timestamp = DateTime.Now,
                Source = "Server",
                Method = request.HttpMethod,
                Url = request.Url?.ToString() ?? string.Empty,
                StatusCode = statusCode,
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Headers = FormatHeaders(request.Headers),
                RequestBody = requestBody,
                ResponseBody = responseBody
            });
        }
    }

    private string CreateStatusResponse()
    {
        var totalRequests = Interlocked.Read(ref _totalRequests);
        var getRequests = Interlocked.Read(ref _getRequests);
        var postRequests = Interlocked.Read(ref _postRequests);
        var totalTicks = Interlocked.Read(ref _totalProcessingTicks);
        var averageMs = totalRequests == 0
            ? 0
            : totalTicks * 1000.0 / Stopwatch.Frequency / totalRequests;

        return JsonSerializer.Serialize(new
        {
            status = "running",
            uptime = DateTime.Now - _startedAt,
            totalRequests,
            getRequests,
            postRequests,
            averageProcessingTimeMs = averageMs,
            storedMessages = _messages.Count
        }, JsonOptions);
    }

    private (int StatusCode, string ResponseBody) ProcessPostBody(string requestBody)
    {
        try
        {
            using var document = JsonDocument.Parse(requestBody);
            if (!document.RootElement.TryGetProperty("message", out var messageProperty)
                || messageProperty.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(messageProperty.GetString()))
            {
                return CreateBadRequest("JSON body must contain a non-empty string property 'message'.");
            }

            var message = new Message
            {
                Text = messageProperty.GetString()!,
                ReceivedAt = DateTime.Now
            };

            _messages[message.Id] = message;

            var response = JsonSerializer.Serialize(new
            {
                id = message.Id,
                receivedAt = message.ReceivedAt
            }, JsonOptions);

            return ((int)HttpStatusCode.Created, response);
        }
        catch (JsonException)
        {
            return CreateBadRequest("Request body must be valid JSON.");
        }
    }

    private static (int StatusCode, string ResponseBody) CreateBadRequest(string error)
    {
        return ((int)HttpStatusCode.BadRequest, JsonSerializer.Serialize(new { error }, JsonOptions));
    }

    private async Task PublishLogAsync(LogEntry entry)
    {
        var handler = RequestLogged;
        if (handler is not null)
        {
            await handler(entry);
        }
    }

    private static string FormatHeaders(NameValueCollection headers)
    {
        var builder = new StringBuilder();
        foreach (var key in headers.AllKeys)
        {
            if (key is null)
            {
                continue;
            }

            builder.Append(key).Append(": ").AppendLine(headers[key]);
        }

        return builder.ToString().TrimEnd();
    }

    private void ResetState()
    {
        _messages.Clear();
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _getRequests, 0);
        Interlocked.Exchange(ref _postRequests, 0);
        Interlocked.Exchange(ref _totalProcessingTicks, 0);
    }
}
