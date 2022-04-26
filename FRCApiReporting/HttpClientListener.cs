using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

public class HttpClientListener : IObserver<DiagnosticListener>
{
    private readonly HttpClientInterceptor _interceptor = new HttpClientInterceptor();

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(DiagnosticListener listener)
    {
        listener.Subscribe(_interceptor);
    }

    private class HttpClientInterceptor : IObserver<KeyValuePair<string, object>>
    {
        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (value.Key == "System.Net.Http.Desktop.HttpRequestOut.Start")
            {
                Debug.WriteLine($"Request: {JsonSerializer.Serialize(value.Value)}");
            }
            else if (value.Key == "System.Net.Http.Desktop.HttpRequestOut.Stop")
            {
                Debug.WriteLine($"Response: {JsonSerializer.Serialize(value.Value)}");
            }
        }
    }
}
