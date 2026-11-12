using System.Data;
using System.Data.Common;

using Looplex.Foundation.Helpers;

namespace Looplex.Foundation.UnitTests.Helpers;

/// <summary>
/// <c>Dbs.QueryAsync</c> returns exactly the declared number of result sets, none of them null,
/// even when a result set has no rows.
/// </summary>
[TestClass]
public class DbsQueryAsyncTests
{
  [TestMethod]
  public async Task EmptyResultSet_IsNotNull_AndKeepsItsColumns()
  {
    using FakeCommand command = new(Tables(rows: [2, 0, 1]));
    ResultSetInfo[] infos = [new(typeof(int), "a"), new(typeof(int), "b"), new(typeof(int), "c")];

    ResultSet[] result = await command.QueryAsync(infos, CancellationToken.None);

    Assert.AreEqual(3, result.Length);
    Assert.IsTrue(result.All(r => r != null && r.Info != null && r.Result != null));
    Assert.AreEqual(0, result[1].Result!.Rows.Count);
    Assert.AreEqual(1, result[1].Result!.Columns.Count);
    Assert.AreEqual("b", result[1].Result!.TableName);
  }

  [TestMethod]
  public async Task MoreResultSetsThanExpected_Throws()
  {
    using FakeCommand command = new(Tables(rows: [1, 1, 1]));

    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
      command.QueryAsync([new(typeof(int)), new(typeof(int))], CancellationToken.None));
  }

  [TestMethod]
  public async Task FewerResultSetsThanExpected_Throws()
  {
    using FakeCommand command = new(Tables(rows: [1, 1]));

    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
      command.QueryAsync([new(typeof(int)), new(typeof(int)), new(typeof(int))], CancellationToken.None));
  }

  private static DataSet Tables(int[] rows)
  {
    DataSet set = new();
    foreach (int count in rows)
    {
      DataTable table = set.Tables.Add();
      table.Columns.Add("v", typeof(int));
      for (int i = 0; i < count; i++) table.Rows.Add(i);
    }

    return set;
  }

  private sealed class FakeCommand(DataSet data) : DbCommand
  {
    public override string CommandText { get; set; } = "";
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
    protected override DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery() => throw new NotSupportedException();
    public override object? ExecuteScalar() => throw new NotSupportedException();
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => data.CreateDataReader();
  }
}
