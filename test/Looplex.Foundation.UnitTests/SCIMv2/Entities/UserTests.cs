using System.ComponentModel;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities;

[TestClass]
public class UserTests
{
    [TestMethod]
    public void User_MinimalPayload_ShouldDeserializeJson()
    {
        // Only userName is provided
        var minimalJson = @"{
              ""userName"": ""minimalUser""
            }";

        var user = minimalJson.JsonDeserialize<User>();

        Assert.IsNotNull(user, "Deserializing minimal user payload should not produce null.");
        Assert.AreEqual("minimalUser", user.UserName, "UserName must match minimal JSON payload.");
        Assert.IsNull(user.Id, "Id was not in JSON and should remain null.");
        Assert.AreEqual(0, user.Emails.Count, "Emails list should be empty if not provided.");
    }

    [TestMethod]
    public void User_MinimalPayload_ShouldDeserializeXml()
    {
        // Only userName is provided
        var minimalXml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<User xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <UserName>minimalXmlUser</UserName>
</User>";

        var user = minimalXml.XmlDeserialize<User>();

        Assert.IsNotNull(user, "Deserializing minimal user XML should not produce null.");
        Assert.AreEqual("minimalXmlUser", user.UserName, "UserName must match minimal XML payload.");
        Assert.IsNull(user.Id, "Id was not in XML and should remain null.");
        Assert.AreEqual(0, user.Emails.Count, "Emails list should be empty if not provided.");
    }

    [TestMethod]
    public void User_MinimalPayload_ShouldDeserializeProtobuf()
    {
        // Only userName is provided
        var minimalProtobuf = new User { UserName = "minimalUserProtobuf" }.ProtobufSerialize();

        var user = minimalProtobuf.ProtobufDeserialize<User>();

        Assert.IsNotNull(user, "Deserializing minimal user XML should not produce null.");
        Assert.AreEqual("minimalUserProtobuf", user.UserName, "UserName must match minimal XML payload.");
        Assert.IsNull(user.Id, "Id was not in XML and should remain null.");
        Assert.AreEqual(0, user.Emails.Count, "Emails list should be empty if not provided.");
    }

    [TestMethod]
    public void User_FullEnterprisePayload_ShouldDeserializeJsonRoundTrip()
    {
        var enterpriseJson = @"{
              ""id"": ""2819c223-7f76-453a-919d-413861904646"",
              ""userName"": ""bjensen@example.com"",
              ""name"": {
                ""formatted"": ""Ms. Barbara J Jensen III"",
                ""familyName"": ""Jensen"",
                ""givenName"": ""Barbara"",
                ""middleName"": ""Jane""
              },
              ""displayName"": ""Barbara J Jensen"",
              ""emails"": [
                {
                  ""value"": ""bjensen@example.com"",
                  ""type"": ""work"",
                  ""primary"": true
                },
                {
                  ""value"": ""babs@jensen.org"",
                  ""type"": ""home""
                }
              ],
              ""active"": true,
              ""urn:ietf:params:scim:schemas:extension:enterprise:2.0:User"": {
                ""employeeNumber"": ""123456"",
                ""manager"": { 
                    ""value"": ""Man-001"", 
                    ""displayName"": ""Bob Boss""
                }
              },
              ""meta"": {
                ""created"": ""2023-09-05T15:21:00Z"",
                ""lastModified"": ""2023-10-01T10:42:00Z"",
                ""location"": ""https://example.com/Users/2819c223-7f76-453a-919d-413861904646"",
                ""resourceType"": ""User""
              }
            }";

        var user = enterpriseJson.JsonDeserialize<User>();

        Assert.IsNotNull(user, "Deserializing full enterprise user payload should not produce null.");
        Assert.AreEqual("2819c223-7f76-453a-919d-413861904646", user.Id, "Id must match the JSON payload.");
        Assert.AreEqual("bjensen@example.com", user.UserName, "UserName must match the JSON payload.");
        Assert.IsNotNull(user.Name, "Name complex object should not be null when provided.");
        Assert.AreEqual("Barbara", user.Name.GivenName, "GivenName must match JSON payload.");
        Assert.IsTrue(user.Active, "Active is provided as 'true' in the payload.");
        Assert.AreEqual(2, user.Emails.Count, "Should deserialize two email objects.");

        // Check meta fields
        Assert.IsNotNull(user.Meta, "Meta should not be null.");
        Assert.AreEqual("User", user.Meta.ResourceType, "ResourceType must match JSON payload.");
        Assert.AreEqual("https://example.com/Users/2819c223-7f76-453a-919d-413861904646", user.Meta.Location,
            "Location must match JSON payload.");
        Assert.IsTrue(user.Meta.Created.HasValue, "Created date/time should be parsed into Meta.");
        Assert.IsTrue(user.Meta.LastModified.HasValue, "LastModified date/time should be parsed into Meta.");

        // Round-trip
        var reSerialized = user.JsonSerialize();
        var reHydrated = reSerialized.JsonDeserialize<User>();
        Assert.AreEqual(user.Id, reHydrated.Id, "Round-trip user ID must match.");
        Assert.AreEqual(user.Emails.Count, reHydrated.Emails.Count, "Round-trip emails count must match.");
    }

    [TestMethod]
    public void User_PropertyChanged_ShouldRaiseEvent()
    {
        var user = new User();
        Assert.IsTrue(user is INotifyPropertyChanged, "Fody should weave INotifyPropertyChanged into User.");

        string? changedPropertyName = null;
        ((INotifyPropertyChanged)user).PropertyChanged += (sender, e) => { changedPropertyName = e.PropertyName; };

        user.DisplayName = "A New Display Name";

        Assert.AreEqual("DisplayName", changedPropertyName,
            "Changing DisplayName property should raise PropertyChanged with 'DisplayName'.");
    }

    [TestMethod]
    public void User_CollectionProperties_ShouldRaiseEventOnReplaceCollection()
    {
        var user = new User();
        string? changedPropertyName = null;
        ((INotifyPropertyChanged)user).PropertyChanged += (sender, e) => { changedPropertyName = e.PropertyName; };

        user.Emails = new List<ScimEmail>
        {
            new ScimEmail { Value = "test@example.com", Type = "home" }
        };

        Assert.AreEqual("Emails", changedPropertyName,
            "Replacing the Emails list property should raise PropertyChanged with 'Emails'.");
    }

    [TestMethod]
    public void User_NullStrings_ShouldSerializeCleanly()
    {
        var user = new User
        {
            Id = null,
            UserName = null,
            Active = false
        };

        var json = user.JsonSerialize();
        Assert.IsNotNull(json, "Serialization should produce a valid JSON string even if some fields are null.");

        var reHydrated = json.JsonDeserialize<User>();
        Assert.IsNotNull(reHydrated, "We should be able to deserialize back from JSON with null fields.");
        Assert.IsNull(reHydrated.Id, "Id should remain null after round-trip.");
        Assert.IsNull(reHydrated.UserName, "UserName should remain null after round-trip.");
    }
}