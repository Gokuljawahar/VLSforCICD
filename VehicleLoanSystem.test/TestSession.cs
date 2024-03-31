using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class TestSession : ISession
{
    private readonly Dictionary<string, object> _sessionData = new Dictionary<string, object>();
    private string _id = Guid.NewGuid().ToString();

    public string Id => _id;

    public IEnumerable<string> Keys => _sessionData.Keys;

    public byte[] Get(string key)
    {
        _sessionData.TryGetValue(key, out object value);
        return value as byte[];
    }

    public void Set(string key, byte[] value)
    {
        _sessionData[key] = value;
    }

    public void Commit()
    {
        // No-op for testing purposes
    }

    public bool IsAvailable => true;

    public bool TryGetValue(string key, out byte[] value)
    {
        object objValue;
        if (_sessionData.TryGetValue(key, out objValue))
        {
            value = objValue as byte[];
            return true;
        }

        value = null;
        return false;
    }

    public void Remove(string key)
    {
        _sessionData.Remove(key);
    }

    public void Clear()
    {
        _sessionData.Clear();
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // No-op for testing purposes
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        // No-op for testing purposes
        return Task.CompletedTask;
    }
}
