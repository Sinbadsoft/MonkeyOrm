MonkeyOrm
=========

Small and powerful ORM for .NET

# Installation
```powershell
Install-Package MonkeyOrm.MySql
```

# Save anything
POCOs:

```csharp
connection.Save("Users", new User { Name = "Anne", Age = 31 });
```
Anonymous:

```csharp
connection.Save("Users", new { Name = "John", Age = 26 });
```
Hashes: [<code>IDictionary<></code>](http://msdn.microsoft.com/en-us/library/s4ys34ea), [<code>IDictionary</code>](http://msdn.microsoft.com/en-us/library/system.collections.idictionary), [<code>ExpandoObject</code>](http://msdn.microsoft.com/en-us/library/System.Dynamic.ExpandoObject.aspx) or [<code>NameValueCollection</code>](http://msdn.microsoft.com/en-us/library/System.Collections.Specialized.NameValueCollection.aspx):
```csharp
connection.Save("Users", new Dictionary<string, object>
                                {
                                    { "Name", "Fred" },
                                    { "Age", 22 }
                                });
```
Get back the auto generated serial id if any:

```csharp
int pabloId;
connection.Save("Users", new User { Name = "Pablo", Age = 49 }, out pabloId);
```

##### What the heck is `connection`?
MonkeyOrm Api consists mainly of extension methods, just like the <code>Save</code> method in the snippets above.
In order to make things easier for client code, several types are extended with the same methods. Actually, `connection` can be an [<code>IDbConnection</code>](http://msdn.microsoft.com/en-us/library/system.data.idbconnection.aspx), an [<code>IDbTransaction</code>](http://msdn.microsoft.com/en-us/library/system.data.idbtransaction.aspx), 
any connection factory function [`Func<IDbConnection>`](http://msdn.microsoft.com/en-us/library/bb534960.aspx), or the MonkeyOrm defined interface [<code>IConnectionFactory</code>](https://github.com/Sinbadsoft/MonkeyOrm/blob/master/MonkeyOrm/IConnectionFactory.cs).



# Read just one item

```csharp
var joe = connection.ReadOne("Select * From Users Where Name = @name", new { name = "Joe" });
```
Reads only the first element, if any, from the result set. You can also read computed data:

```csharp
var stats = connection.ReadOne("Select Max(Age) As Max, Min(Age) As Min From Users");
Console.WriteLine("Max {0} - Min {1}", stats.Max, stats.Min);
```

# Read'em All
```csharp
var users = connection.ReadAll("Select * From Users Where Age > @age", new { age = 30 });
```

Bulk fetches the whole result set in memory as a list.

# Update

```csharp
connection.Update("Users", new { CanBuyAlchohol = true }, "Age >= @age", new { age = 21 });
```

# Save or Update
```csharp
connection.SaveOrUpdate("Users", new User { Name = "Anne", Age = 32 });
```
Aka Upsert. Attempts to save first. If the insertion violates a key or unicity constraint, an update is performed instead.

# Delete
```csharp
connection.Delete("Users", "Name=@name", new { name = "Sauron" });
```

# Stream Read
Instead of bulk fetching query results in memory, they are wrapped in an enumerable for lazy evaluation. Items are loaded from the databse when the result is actually enumerated, one at a time.

Here is an example where results are streamed from the database to a file on disk:
```csharp
var users = connection.ReadStream("Select * From Users");

using(var file = new StreamWriter(Path.GetTempFileName()))
foreach (var user in users)
{
    file.WriteLine("{0} - {1}", user.Name, user.Age);
}
```

Two Bonus Points: (1) the result enumerable can be enumerated multiple times if data needs to be re-streamed from the database (the query will be executed again), (2) Linq queries can be used on the result as for any enumerable.

In addition, `ReadStream` has an overload that, instead of returning the result as enumerabke, accepts function of boolean that is called for each result item until it returns `false`. The snippet above would be equivalent to:
```csharp
using(var file = new StreamWriter(Path.GetTempFileName()))
connection.ReadStream("Select * From Users", user => 
{
    file.WriteLine("{0} - {1}", user.Name, user.Age);
    return true;
});
```

# Transactions
```csharp
int spockId = connection.InTransaction(autocommit: true).Do(t =>
{
    int id;
    t.Save("Users", new { Name = "Spock", Age = 55 }, out id);
    t.Save("Profiles", new { UserId = id, Bio = "Federation Ambassador" });
    return id;
});
```
The transaction block can be a function of any type and return a value or be an action and return nothing. The transaction can be manually committed at any point by invoking t.Commit(). Setting `autocommit` to `true` will simply insert a call to `Commit()` after the transaction block.

The transaction [isolation level](http://msdn.microsoft.com/en-us/library/system.data.isolationlevel.aspx) can be specified using the `isolation` parameter:
```csharp
connection.InTransaction(true, IsolationLevel.Serializable).Do(t =>
{
    var james = t.ReadOne("Select * From Users Where Name=@name", new { name = "James" });
    t.Update("Users", new { Age = james.Age + 15 }, "Id=@Id", new { james.Id });
});
```
# Batch insertion
Batch insertion enables insertion of enumerable data sets; whether this data set is held in memory or streamed from any other source (file, database, network etc.).

```csharp
connection.SaveBatch("Users", new[]
    {
        new User { Name = "Monica", Age = 34 },
        new User { Name = "Fred", Age = 58 },
        // ...
    });
```

By default, one object at a time is read from the provided set and inserted in the database. In order to tune performance/bandwidth more elements can be loaded and inserted at once through the `chunkSize` parameter.

In the following snippet, 100 objects are loaded and inserted at a time -- in the same query -- from the provided enumerable to the database.
```csharp
connection.SaveBatch("Users", LoadDataFromRemoteSource(), 100);
```

Batch insertion can also be wrapped in a transaction
```csharp
connection.InTransaction().SaveBatch("Users", users);
```

# Object Slicing
In some contexts, the object or hash we'd like to persist in the database has more properties what we need to persist in the database. This can be for security reasons: the object is automatically created from user input; by a [model binder](http://msdn.microsoft.com/en-us/library/system.web.mvc.imodelbinder.aspx) or a similar mechanism. This can lead to security vulnerabilities. Our dear github was [hacked](http://www.theregister.co.uk/2012/03/05/github_hack/) due to a similar issue (if you want to read more on [this](http://www.diaryofaninja.com/blog/2012/03/11/what-aspnet-mvc-developers-can-learn-from-githubrsquos-security-woes) ).

MonkeyOrm can filter the input object when calling `Save` (or `Update`) by specifying either a black list or a white list on object properties.

```csharp
connection.Save("Users", user, blacklist: new[] { "IsAdmin" });
```
This will prevent a hacker form forging a user input that would force `IsAdmin` column to true.

```csharp
connection.Update("Users", user, "Id=@id", new { id }, whitelist: new[] { "Name", "Age" });
```
Only allows `Name` and `Age`to be updated, nothing else.

# Interceptors and Blobbing
Interceptors are functions or actions you can set in order to be called back and to take control when some conditions are met. One interesting interceptor is the `UnknownValueType` that is called when the data to be inserted in a given column does not map directly to a database native type. Here is an example of this situation with the complex type being `ProfileData`:
```csharp
connection.Save("Users", new { Name="Joe", Age=67, new ProfileData { /* ... */ });
```

In order object, The `UnknownValueType` callback here have a chance to to "intercept" and transform the `ProfileData` complex object to a different format. A typical example would be to blob (serialize) it. Example:
```csharp
MonkeyOrm.Settings.Interceptors.UnknownValueType = o =>
{
    var writer = new StringWriter();
    new XmlSerializer(o.GetType()).Serialize(writer, o);
    return writer.ToString();
};
```

# Other Commands
todo

# Related Projects
* [Massive](https://github.com/robconery/massive)
* [PetaPoco](http://www.toptensoftware.com/petapoco/)
* [Dappler](http://code.google.com/p/dapper-dot-net/)
* [Simple.Data](https://github.com/markrendle/Simple.Data)
