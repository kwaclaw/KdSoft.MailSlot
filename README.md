Small class that exposes the Windows MailSlot API as an asynchronous FileStream.

## Writing to a mailslot:
This is basically like writing to a file.
### Using the command line:
```cli
echo "This is a test" > .\tmp1.txt & copy .\tmp1.txt \\.\mailslot\test1
```
### Using C#:
```csharp
var buffer = new byte[1024];
using (var client = MailSlot.CreateClient("test1")) {
    var bytes = Encoding.UTF8.GetBytes($"Writing test message.\n");
    await client.WriteAsync(bytes);
}

```

## Receiving mailslot messages
Just read asynchronously from the mailslot stream. This will not block a thread.
```csharp
var buffer = new byte[16384];
using (var server = MailSlot.CreateServer("test1")) {
    while (true) {
        var count = await server.ReadAsync(buffer, 0, buffer.Length);
        var msg = Encoding.UTF8.GetString(buffer, 0, count);
        Console.WriteLine(msg);
    }
}
```
