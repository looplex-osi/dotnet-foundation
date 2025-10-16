using Looplex.SCIMv2.Entities;

namespace Looplex.SCIMv2.UnitTests;

[TestClass]
public class UserTests
{
  [TestMethod]
  public void User_ShouldHaveDefaultValues()
  {
    // Arrange & Act
    var user = new User();

    // Assert
    Assert.IsNotNull(user);
    Assert.IsNotNull(user.Id);
    Assert.IsTrue(user.UserName == null || user.UserName == string.Empty);
    Assert.IsNotNull(user.Name);
    Assert.AreEqual(string.Empty, user.DisplayName);
    Assert.AreEqual(string.Empty, user.NickName);
    Assert.AreEqual(string.Empty, user.ProfileUrl);
    Assert.AreEqual(string.Empty, user.Title);
    Assert.AreEqual(string.Empty, user.UserType);
    Assert.AreEqual(string.Empty, user.PreferredLanguage);
    Assert.AreEqual(string.Empty, user.Locale);
    Assert.AreEqual(string.Empty, user.Timezone);
    Assert.IsFalse(user.Active);
    Assert.IsNotNull(user.Emails);
    Assert.IsNotNull(user.PhoneNumbers);
    Assert.IsNotNull(user.Addresses);
    Assert.IsNotNull(user.Groups);
  }

  [TestMethod]
  public void User_ShouldSetProperties()
  {
    // Arrange
    var user = new User();
    var userId = Guid.NewGuid().ToString();
    var userName = "test@example.com";

    // Act
    user.Id = userId;
    user.UserName = userName;
    user.DisplayName = "Test User";
    user.Active = true;

    // Assert
    Assert.AreEqual(userId, user.Id);
    Assert.AreEqual(userName, user.UserName);
    Assert.AreEqual("Test User", user.DisplayName);
    Assert.IsTrue(user.Active);
  }

  [TestMethod]
  public void User_ShouldHaveScimName()
  {
    // Arrange
    var user = new User();

    // Act
    user.Name.Formatted = "John Doe";
    user.Name.GivenName = "John";
    user.Name.FamilyName = "Doe";

    // Assert
    Assert.IsNotNull(user.Name);
    Assert.AreEqual("John Doe", user.Name.Formatted);
    Assert.AreEqual("John", user.Name.GivenName);
    Assert.AreEqual("Doe", user.Name.FamilyName);
  }

  [TestMethod]
  public void User_ShouldHaveEmails()
  {
    // Arrange
    var user = new User();
    var email = new ScimEmail
    {
      Value = "test@example.com",
      Type = "work",
      Primary = true
    };

    // Act
    user.Emails.Add(email);

    // Assert
    Assert.IsTrue(user.Emails.Count == 1);
    Assert.AreEqual("test@example.com", user.Emails[0].Value);
    Assert.AreEqual("work", user.Emails[0].Type);
    Assert.IsTrue(user.Emails[0].Primary);
  }

  [TestMethod]
  public void User_ShouldHavePhoneNumbers()
  {
    // Arrange
    var user = new User();
    var phone = new ScimPhoneNumber
    {
      Value = "+1234567890",
      Type = "work"
    };

    // Act
    user.PhoneNumbers.Add(phone);

    // Assert
    Assert.IsTrue(user.PhoneNumbers.Count == 1);
    Assert.AreEqual("+1234567890", user.PhoneNumbers[0].Value);
    Assert.AreEqual("work", user.PhoneNumbers[0].Type);
  }

  [TestMethod]
  public void User_ShouldHaveAddresses()
  {
    // Arrange
    var user = new User();
    var address = new ScimAddress
    {
      Formatted = "123 Main St, City, State 12345",
      StreetAddress = "123 Main St",
      Locality = "City",
      Region = "State",
      PostalCode = "12345",
      Country = "US",
      Type = "work",
      Primary = true
    };

    // Act
    user.Addresses.Add(address);

    // Assert
    Assert.IsTrue(user.Addresses.Count == 1);
    Assert.AreEqual("123 Main St, City, State 12345", user.Addresses[0].Formatted);
    Assert.AreEqual("123 Main St", user.Addresses[0].StreetAddress);
    Assert.AreEqual("City", user.Addresses[0].Locality);
    Assert.AreEqual("State", user.Addresses[0].Region);
    Assert.AreEqual("12345", user.Addresses[0].PostalCode);
    Assert.AreEqual("US", user.Addresses[0].Country);
    Assert.AreEqual("work", user.Addresses[0].Type);
    Assert.IsTrue(user.Addresses[0].Primary);
  }

  [TestMethod]
  public void User_ShouldHaveGroups()
  {
    // Arrange
    var user = new User();
    var group = new ScimGroupRef
    {
      Value = "group123",
      Display = "Test Group",
      Type = "Group",
      Ref = "/Groups/group123"
    };

    // Act
    user.Groups.Add(group);

    // Assert
    Assert.IsTrue(user.Groups.Count == 1);
    Assert.AreEqual("group123", user.Groups[0].Value);
    Assert.AreEqual("Test Group", user.Groups[0].Display);
    Assert.AreEqual("Group", user.Groups[0].Type);
    Assert.AreEqual("/Groups/group123", user.Groups[0].Ref);
  }
}
