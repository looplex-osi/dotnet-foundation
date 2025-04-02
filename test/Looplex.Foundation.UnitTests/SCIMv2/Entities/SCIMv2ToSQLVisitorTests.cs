using Antlr4.Runtime;

using Looplex.Foundation.SCIMv2.Antlr;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities;

[TestClass]
public class SCIMv2ToSQLVisitorTests
{
  private string ConvertToSql(string input)
  {
    var inputStream = new AntlrInputStream(input);
    var speakLexer = new ScimFilterLexer(inputStream);
    var commonTokenStream = new CommonTokenStream(speakLexer);
    var parser = new ScimFilterParser(commonTokenStream);
    var tree = parser.parse();
    var visitor = new SCIMv2ToSQLVisitor();
    return visitor.Visit(tree);
  }

  [TestMethod]
  public void TestEqOperator()
  {
    var sql = ConvertToSql("userName eq \"john\"");
    Assert.AreEqual("userName = criteria.p1", sql);
  }

  [TestMethod]
  public void TestNeOperator()
  {
    var sql = ConvertToSql("userName ne \"john\"");
    Assert.AreEqual("userName != 'john'", sql);
  }

  [TestMethod]
  public void TestCoOperator()
  {
    var sql = ConvertToSql("email co \"example\"");
    Assert.AreEqual("email LIKE '%example%'", sql);
  }

  [TestMethod]
  public void TestSwOperator()
  {
    var sql = ConvertToSql("email sw \"admin\"");
    Assert.AreEqual("email LIKE 'admin%'", sql);
  }

  [TestMethod]
  public void TestEwOperator()
  {
    var sql = ConvertToSql("email ew \"com\"");
    Assert.AreEqual("email LIKE '%com'", sql);
  }

  [TestMethod]
  public void TestGtOperator()
  {
    var sql = ConvertToSql("age gt 30");
    Assert.AreEqual("age > 30", sql);
  }

  [TestMethod]
  public void TestGeOperator()
  {
    var sql = ConvertToSql("age ge 30");
    Assert.AreEqual("age >= 30", sql);
  }

  [TestMethod]
  public void TestLtOperator()
  {
    var sql = ConvertToSql("age lt 30");
    Assert.AreEqual("age < 30", sql);
  }

  [TestMethod]
  public void TestLeOperator()
  {
    var sql = ConvertToSql("age le 30");
    Assert.AreEqual("age <= 30", sql);
  }

  [TestMethod]
  public void TestPrOperator()
  {
    var sql = ConvertToSql("email pr");
    Assert.AreEqual("email IS NOT NULL", sql);
  }

  [TestMethod]
  public void TestNotExpression()
  {
    var sql = ConvertToSql("not (email eq \"admin@example.com\")");
    Assert.AreEqual("NOT (email = 'admin@example.com')", sql);
  }

  [TestMethod]
  public void TestAndExpression()
  {
    var sql = ConvertToSql("userName eq \"john\" and age gt 18");
    Assert.AreEqual("(userName = 'john' AND age > 18)", sql);
  }

  [TestMethod]
  public void TestOrExpression()
  {
    var sql = ConvertToSql("userName eq \"john\" or userName eq \"jane\"");
    Assert.AreEqual("(userName = 'john' OR userName = 'jane')", sql);
  }
}