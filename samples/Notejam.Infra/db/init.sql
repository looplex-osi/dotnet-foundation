-- MARK: Database

USE master
GO

DROP DATABASE IF EXISTS notejam
GO

IF EXISTS (SELECT * FROM master.sys.server_principals WHERE name = 'sanotejam')
BEGIN
  DROP LOGIN sanotejam
END

-- CREATE DATABASE notejam
-- GO

CREATE DATABASE notejam ON PRIMARY (
  NAME = notejam_DATA,
  SIZE = 10MB,
  FILEGROWTH = 10%
) LOG ON (
  NAME = notejam_LOG,
  SIZE = 5MB,
  FILEGROWTH = 10%
) COLLATE LATIN1_GENERAL_100_CI_AI_SC_UTF8
GO

ALTER DATABASE notejam SET ALLOW_SNAPSHOT_ISOLATION ON
ALTER DATABASE notejam SET READ_COMMITTED_SNAPSHOT ON
GO

EXEC sp_addlogin
  @loginame = N'sanotejam',
  @passwd = N'wNqbBoxjHdSXf3cUOV2K8m52BujC9zmev6FWqKkyYb',
  @defdb = N'notejam'

USE notejam

EXEC sp_adduser
  @loginame = N'sanotejam',
  @name_in_db = N'sanotejam',
  @grpname = N'db_owner'
GO

-- MARK: Helpers

CREATE OR ALTER FUNCTION dbo.fn_validateSortSpecificationList (@sortSpecList varchar(MAX), @table varchar(128), @default varchar(128)) RETURNS varchar(max) AS
BEGIN
  DECLARE @validated varchar(MAX) = '';

  -- check if table exists, otherwise return NULL to signal an error
  IF OBJECT_ID(@table) IS NULL
    RETURN NULL
  
  -- fetch allowed columns from sys.columns
  DECLARE @allowed TABLE (name varchar(128) PRIMARY KEY);
INSERT INTO @allowed(name) SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@table);

-- if @sortSpecList is NULL or EMPTY return @default ASC
IF @sortSpecList IS NULL OR LEN(LTRIM(RTRIM(@sortSpecList))) = 0
BEGIN
SELECT @validated = QUOTENAME(name) + ' ASC'
FROM @allowed
WHERE name = @default;
END

  DECLARE @xml XML = CAST('<ol><li>' + REPLACE(@sortSpecList, ',', '</li><li>') + '</li></ol>' AS XML);
  DECLARE @i int = 1;
  DECLARE @k int = @xml.value('count(/ol/li)', 'int');
  DECLARE @sortSpec varchar(192);
  DECLARE @sortKey varchar(128);
  DECLARE @orderingSpec varchar(4);
  DECLARE @spacePos int;

  WHILE @i <= @k
BEGIN
    SET @sortSpec = LTRIM(RTRIM(@xml.value('(/ol/li[position()=sql:variable("@i")])[1]', 'varchar(192)')));
    -- check if there is an optional ordering specification in the sort specification
    -- <sort specification> ::= <sort key> [ <ordering specification> ] [ <null ordering> ]
    SET @spacePos = CHARINDEX(' ', @sortSpec);
    IF @spacePos > 0
BEGIN
      SET @sortKey = LEFT(@sortSpec, @spacePos - 1);
      SET @orderingSpec = UPPER(LTRIM(RTRIM(SUBSTRING(@sortSpec, @spacePos + 1, LEN(@sortSpec)))));
END
ELSE
BEGIN
      SET @sortKey = @sortSpec;
      SET @orderingSpec = 'ASC'
END
    -- <ordering specification> ::= ASC | DESC
    IF @orderingSpec NOT IN ('ASC', 'DESC')
      SET @orderingSpec = 'ASC';
    -- check if sortKey is an allowed column
    IF EXISTS (SELECT 1 FROM @allowed WHERE name = @sortKey)
BEGIN
      IF LEN(@validated) > 0
        SET @validated = @validated + ', ';
      SET @validated = @validated + QUOTENAME(@sortKey) + ' ' + @orderingSpec;
END
ELSE
BEGIN
      -- invalid column name provided, return NULL to signal an error
RETURN NULL;
END
    SET @i = @i + 1;
END
RETURN @validated;
END;
GO

CREATE OR ALTER PROC [dbo].[USP_rowsFromTable]
  @table sysname = NULL,
  @schema sysname = NULL
AS
IF @table IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_rowsFromTable] (sysname)@table[, (sysname)@schema]', 1;
WITH cte AS (
    SELECT
        T.object_id,
   [schema] = SCHEMA_NAME(T.schema_id),
   [table] = T.name
FROM sys.tables T
WHERE T.name = @table
  AND (@schema IS NULL OR SCHEMA_NAME(T.schema_id) = @schema)
    )
SELECT
    [schema],
    [table],
    [rows] = SUM(P.rows)
FROM cte
    INNER JOIN sys.partitions P ON cte.object_id = P.object_id
WHERE P.index_id IN (0, 1)-- 0 := Table as Heap, 1 := Table Clustered Index
GROUP BY [schema], [table]
    GO

-- MARK: Entities

CREATE TABLE [dbo].[users] (
    [id] int IDENTITY(-2147483648, 1) NOT NULL CONSTRAINT PK_users_id PRIMARY KEY CLUSTERED,
    [uuid] uniqueidentifier NOT NULL CONSTRAINT DF_users_uuid DEFAULT NEWSEQUENTIALID(),
    [name] varchar(128) NOT NULL,
    [email] varchar(75) NOT NULL,
    [active] bit NOT NULL CONSTRAINT DF_users_active DEFAULT 1,
    [status] tinyint NOT NULL CONSTRAINT DF_users_status DEFAULT 1,
    [custom_fields] varchar(2000) NOT NULL CONSTRAINT DF_users_custom_fields DEFAULT '{}',
    [created_at] datetime2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
    [updated_at] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
    [__valid_until__] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([updated_at], [__valid_until__])
    ) ON [PRIMARY]
    GO
CREATE NONCLUSTERED INDEX IN_users_active ON [dbo].[users](active) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_users_uuid ON [dbo].[users](uuid) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_users_query_default ON [dbo].[users](active, name) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX IN_users_email ON [dbo].[users](email) ON [PRIMARY]
GO

CREATE TABLE [dbo].[groups] (
    [id] int IDENTITY(-2147483648, 1) NOT NULL CONSTRAINT PK_groups_id PRIMARY KEY CLUSTERED,
    [uuid] uniqueidentifier NOT NULL CONSTRAINT DF_groups_uuid DEFAULT NEWSEQUENTIALID(),
    [name] varchar(128) NOT NULL,
    [active] bit NOT NULL CONSTRAINT DF_groups_active DEFAULT 1,
    [status] tinyint NOT NULL CONSTRAINT DF_groups_status DEFAULT 1,
    [custom_fields] varchar(2000) NOT NULL CONSTRAINT DF_groups_custom_fields DEFAULT '{}',
    [created_at] datetime2 NOT NULL CONSTRAINT DF_groups_created_at DEFAULT SYSUTCDATETIME(),
    [updated_at] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
    [__valid_until__] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([updated_at], [__valid_until__])
    ) ON [PRIMARY]
    GO
CREATE NONCLUSTERED INDEX IN_groups_active ON [dbo].[groups](active) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_groups_uuid ON [dbo].[groups](uuid) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_groups_query_default ON [dbo].[groups](active, name) ON [PRIMARY]
GO

CREATE TABLE [dbo].[pads] (
    [id] int IDENTITY(-2147483648, 1) NOT NULL CONSTRAINT PK_pads_id PRIMARY KEY CLUSTERED,
    [uuid] uniqueidentifier NOT NULL CONSTRAINT DF_pads_uuid DEFAULT NEWSEQUENTIALID(),
    [user_id] int NOT NULL CONSTRAINT FK_pads_PK_users_id FOREIGN KEY REFERENCES [dbo].[users](id) ON DELETE CASCADE,
    [name] varchar(128) NOT NULL,
    [active] bit NOT NULL CONSTRAINT DF_pads_active DEFAULT 1,
    [status] tinyint NOT NULL CONSTRAINT DF_pads_status DEFAULT 1,
    [custom_fields] varchar(2000) NOT NULL CONSTRAINT DF_pads_custom_fields DEFAULT '{}',
    [created_at] datetime2 NOT NULL CONSTRAINT DF_pads_created_at DEFAULT SYSUTCDATETIME(),
    [updated_at] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
    [__valid_until__] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([updated_at], [__valid_until__])
    ) ON [PRIMARY]
    GO
CREATE NONCLUSTERED INDEX IN_pads_active ON [dbo].[pads](active) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_pads_uuid ON [dbo].[pads](uuid) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_pads_query_default ON [dbo].[pads](active, name) ON [PRIMARY]
GO

CREATE TABLE [dbo].[notes] (
    [id] int IDENTITY(-2147483648, 1) NOT NULL CONSTRAINT PK_notes_id PRIMARY KEY CLUSTERED,
    [uuid] uniqueidentifier NOT NULL CONSTRAINT DF_notes_uuid DEFAULT NEWSEQUENTIALID(),
    [pad_id] int NOT NULL CONSTRAINT FK_notes_pad_id FOREIGN KEY REFERENCES [dbo].[pads](id) ON DELETE CASCADE,
    [name] varchar(256) NOT NULL,
    [text] varchar(MAX) NOT NULL,
    [active] bit NOT NULL CONSTRAINT DF_notes_active DEFAULT 1,
    [status] tinyint NOT NULL CONSTRAINT DF_notes_status DEFAULT 1,
    [custom_fields] varchar(2000) NOT NULL CONSTRAINT DF_notes_custom_fields DEFAULT '{}',
    [created_at] datetime2 NOT NULL CONSTRAINT DF_notes_created_at DEFAULT SYSUTCDATETIME(),
    [updated_at] datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
    [__valid_until__] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME ([updated_at], [__valid_until__])
    ) ON [PRIMARY]
    GO
CREATE NONCLUSTERED INDEX IN_notes_active ON [dbo].[notes](active) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_notes_uuid ON [dbo].[notes](uuid) ON [PRIMARY]
CREATE NONCLUSTERED INDEX IN_notes_query_default ON [dbo].[notes](active, name) ON [PRIMARY]
GO

-- MARK: Relationships

CREATE TABLE [dbo].[users_groups] (
    [userId] int NOT NULL CONSTRAINT FK_users_groups_userId FOREIGN KEY REFERENCES [dbo].[users](id) ON DELETE CASCADE,
    [groupId] int NOT NULL CONSTRAINT FK_users_groups_groupId FOREIGN KEY REFERENCES [dbo].[groups](id) ON DELETE CASCADE,
    [custom_fields] varchar(2000) NOT NULL CONSTRAINT DF_users_groups_custom_fields DEFAULT '{}'
    ) ON [PRIMARY]
CREATE UNIQUE CLUSTERED INDEX IN_users_groups_compositeKey ON [dbo].[users_groups](userId, groupId) ON [PRIMARY];
CREATE NONCLUSTERED INDEX IN_users_groups_userId ON [dbo].[users_groups](userId) ON [PRIMARY];
CREATE NONCLUSTERED INDEX IN_users_groups_groupId ON [dbo].[users_groups](groupId) ON [PRIMARY];
GO

-- MARK: Procedures

CREATE OR ALTER PROC [dbo].[USP_user_create]
  @name varchar(128) = NULL,
  @email varchar(75) = NULL,
  @active bit = 1,
  @status tinyint = 1,
  @custom_fields varchar(2000) = '{}'
AS
-- validate
IF @name IS NULL
OR @email IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_user_create] (varchar[128])@name, (varchar[75])@email[, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
INSERT INTO [dbo].[users](name, email, active, status, custom_fields)
    OUTPUT Inserted.uuid
VALUES (@name, @email, @active, @status, @custom_fields)
    GO

CREATE OR ALTER PROC [dbo].[USP_user_retrieve]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_user_retrieve] (uniqueidentifier)@uuid', 1;
-- act
SELECT *
FROM [dbo].[users]
WHERE [uuid] = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_user_update]
  @uuid uniqueidentifier = NULL,
  @name varchar(128) = NULL,
  @email varchar(75) = NULL,
  @active bit = NULL,
  @status tinyint = NULL,
  @custom_fields varchar(2000) = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_user_update] (uniqueidentifier)@uuid[, (varchar[128])@name, (varchar[75])@email, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @custom_fields IS NOT NULL AND ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
UPDATE [dbo].[users] SET
    name = COALESCE(@name, name),
    email = COALESCE(@email, email),
    active = COALESCE(@active, active),
    status = COALESCE(@status, status),
    custom_fields = COALESCE(@custom_fields, custom_fields)
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_user_delete]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_user_delete] (uniqueidentifier)@uuid', 1;
-- act
DELETE FROM [dbo].[users]
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_users_cquery]
  @has_next bit OUTPUT,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @filter_email varchar(75) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_users_cquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[users]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';
IF @filter_email IS NOT NULL SET @predicate = @predicate + N' AND email LIKE @filter_email';

DECLARE @sql nvarchar(MAX) = FORMATMESSAGE(N'
SELECT *
INTO #__page_delta__
FROM [dbo].[users]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT (@page_size + 1) ROWS ONLY

SELECT TOP %s * FROM #__page_delta__;
SELECT @has_next = CASE WHEN COUNT(*) > @page_size THEN 1 ELSE 0 END FROM #__page_delta__;

DROP TABLE #__page_delta__;
', @predicate, @safe_orderBy, CAST(@page_size AS nvarchar(32)));
-- act
EXEC sp_executesql
  @sql,
  N'@has_next bit OUTPUT,
    @page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128),
    @filter_email varchar(75)
  ',
  @has_next = @has_next OUTPUT,
  @page = @page,
  @page_size = @page_size,
  @filter_active = @filter_active,
  @filter_name = @filter_name,
  @filter_email = @filter_email
;
GO

CREATE OR ALTER PROC [dbo].[USP_users_pquery]
  @do_count bit = 0,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @filter_email varchar(75) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_users_pquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[users]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';
IF @filter_email IS NOT NULL SET @predicate = @predicate + N' AND email LIKE @filter_email';

DECLARE @sql nvarchar(MAX);
-- act
SET @sql = FORMATMESSAGE(N'
SELECT *
FROM [dbo].[users]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT @page_size ROWS ONLY;
', @predicate, @safe_orderBy)

EXEC sp_executesql @sql,
  N'@page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128),
    @filter_email varchar(75)
  ', 
@page = @page, 
@page_size = @page_size, 
@filter_active = @filter_active,
@filter_name = @filter_name,
@filter_email = @filter_email;

IF @do_count = 1 BEGIN
  SET @sql = FORMATMESSAGE(N'SELECT total = COUNT(1) FROM [dbo].[users] WHERE %s', @predicate)
  EXEC sp_executesql
    @sql,
    N'@filter_active bit,
      @filter_name varchar(128),
      @filter_email varchar(75)
    ', 
    @filter_active = @filter_active,
    @filter_name = @filter_name,
    @filter_email = @filter_email;
END
GO

CREATE OR ALTER PROC [dbo].[USP_group_create]
  @name varchar(128) = NULL,
  @active bit = 1,
  @status tinyint = 1,
  @custom_fields varchar(2000) = '{}'
AS
-- validate
IF @name IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_group_create] (varchar[128])@name[, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
INSERT INTO [dbo].[groups](name, active, status, custom_fields)
    OUTPUT Inserted.uuid
VALUES (@name, @active, @status, @custom_fields)
    GO

CREATE OR ALTER PROC [dbo].[USP_group_retrieve]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_group_retrieve] (uniqueidentifier)@uuid', 1;
-- act
SELECT *
FROM [dbo].[groups]
WHERE [uuid] = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_group_update]
  @uuid uniqueidentifier = NULL,
  @name varchar(128) = NULL,
  @active bit = NULL,
  @status tinyint = NULL,
  @custom_fields varchar(2000) = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_group_update] (uniqueidentifier)@uuid[, (varchar[128])@name, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @custom_fields IS NOT NULL AND ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
UPDATE [dbo].[groups] SET
    name = COALESCE(@name, name),
    active = COALESCE(@active, active),
    status = COALESCE(@status, status),
    custom_fields = COALESCE(@custom_fields, custom_fields)
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_group_delete]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_group_delete] (uniqueidentifier)@uuid', 1;
-- act
DELETE FROM [dbo].[groups]
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_groups_cquery]
  @has_next bit OUTPUT,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_groups_cquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[groups]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX) = FORMATMESSAGE(N'
SELECT *
INTO #__page_delta__
FROM [dbo].[groups]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT (@page_size + 1) ROWS ONLY

SELECT TOP %s * FROM #__page_delta__;
SELECT @has_next = CASE WHEN COUNT(*) > @page_size THEN 1 ELSE 0 END FROM #__page_delta__;

DROP TABLE #__page_delta__;
', @predicate, @safe_orderBy, CAST(@page_size AS nvarchar(32)));
-- act
EXEC sp_executesql
  @sql,
  N'@has_next bit OUTPUT,
    @page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128)
  ',
  @has_next = @has_next OUTPUT,
  @page = @page,
  @page_size = @page_size,
  @filter_active = @filter_active,
  @filter_name = @filter_name
;
GO

CREATE OR ALTER PROC [dbo].[USP_groups_pquery]
  @do_count bit = 0,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_groups_pquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[groups]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX);
-- act
SET @sql = FORMATMESSAGE(N'
SELECT *
FROM [dbo].[groups]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT @page_size ROWS ONLY;
', @predicate, @safe_orderBy)

EXEC sp_executesql @sql,
  N'@page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128)
  ', 
@page = @page, 
@page_size = @page_size, 
@filter_active = @filter_active,
@filter_name = @filter_name;

IF @do_count = 1 BEGIN
  SET @sql = FORMATMESSAGE(N'SELECT total = COUNT(1) FROM [dbo].[groups] WHERE %s', @predicate)
  EXEC sp_executesql
    @sql,
    N'@filter_active bit,
      @filter_name varchar(128)
    ', 
    @filter_active = @filter_active,
    @filter_name = @filter_name;
END
GO

CREATE OR ALTER PROC [dbo].[USP_pad_create]
  @user_id int = NULL,
  @name varchar(128) = NULL,
  @active bit = 1,
  @status tinyint = 1,
  @custom_fields varchar(2000) = '{}'
AS
-- validate
IF @user_id IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_pad_create] (int)@user_id, (varchar[128])@name[, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @name IS NULL
  THROW 65600, N'@name cannot be NULL', 1;
IF ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
INSERT INTO [dbo].[pads](user_id, name, active, status, custom_fields)
    OUTPUT Inserted.uuid
VALUES (@user_id, @name, @active, @status, @custom_fields)
    GO

CREATE OR ALTER PROC [dbo].[USP_pad_retrieve]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_pad_retrieve] (uniqueidentifier)@uuid', 1;
-- act
SELECT *
FROM [dbo].[pads]
WHERE [uuid] = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_pad_update]
  @uuid uniqueidentifier = NULL,
  @user_id int = NULL,
  @name varchar(128) = NULL,
  @active bit = NULL,
  @status tinyint = NULL,
  @custom_fields varchar(2000) = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_pad_update] (uniqueidentifier)@uuid[, (int)@user_id, (varchar[128])@name, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @custom_fields IS NOT NULL AND ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
UPDATE [dbo].[pads] SET
    user_id = COALESCE(@user_id, user_id),
    name = COALESCE(@name, name),
    active = COALESCE(@active, active),
    status = COALESCE(@status, status),
    custom_fields = COALESCE(@custom_fields, custom_fields)
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_pad_delete]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_pad_delete] (uniqueidentifier)@uuid', 1;
-- act
DELETE FROM [dbo].[pads]
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_pads_cquery]
  @has_next bit OUTPUT,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_pads_cquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[pads]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX) = FORMATMESSAGE(N'
SELECT *
INTO #__page_delta__
FROM [dbo].[pads]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT (@page_size + 1) ROWS ONLY

SELECT TOP %s * FROM #__page_delta__;
SELECT @has_next = CASE WHEN COUNT(*) > @page_size THEN 1 ELSE 0 END FROM #__page_delta__;

DROP TABLE #__page_delta__;
', @predicate, @safe_orderBy, CAST(@page_size AS nvarchar(32)));
-- act
EXEC sp_executesql
  @sql,
  N'@has_next bit OUTPUT,
    @page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128)
  ',
  @has_next = @has_next OUTPUT,
  @page = @page,
  @page_size = @page_size,
  @filter_active = @filter_active,
  @filter_name = @filter_name
;
GO

CREATE OR ALTER PROC [dbo].[USP_pads_pquery]
  @do_count bit = 0,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(128) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_pads_pquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[pads]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX);
-- act
SET @sql = FORMATMESSAGE(N'
SELECT *
FROM [dbo].[pads]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT @page_size ROWS ONLY;
', @predicate, @safe_orderBy)

EXEC sp_executesql @sql,
  N'@page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(128)
  ', 
@page = @page, 
@page_size = @page_size, 
@filter_active = @filter_active,
@filter_name = @filter_name;

IF @do_count = 1 BEGIN
  SET @sql = FORMATMESSAGE(N'SELECT total = COUNT(1) FROM [dbo].[pads] WHERE %s', @predicate)
  EXEC sp_executesql
    @sql,
    N'@filter_active bit,
      @filter_name varchar(128)
    ', 
    @filter_active = @filter_active,
    @filter_name = @filter_name;
END
GO

CREATE OR ALTER PROC [dbo].[USP_note_create]
  @pad_id int = NULL,
  @name varchar(256) = NULL,
  @text varchar(MAX) = NULL,
  @active bit = 1,
  @status tinyint = 1,
  @custom_fields varchar(2000) = '{}'
AS
-- validate
IF @pad_id IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_note_create] (int)@pad_id, (varchar[256])@name, (varchar[MAX])@text[, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @name IS NULL
  THROW 65600, N'@name cannot be NULL', 1;
IF @text IS NULL
  THROW 65600, N'@text cannot be NULL', 1;
IF ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
INSERT INTO [dbo].[notes](pad_id, name, text, active, status, custom_fields)
    OUTPUT Inserted.uuid
VALUES (@pad_id, @name, @text, @active, @status, @custom_fields)
    GO

CREATE OR ALTER PROC [dbo].[USP_note_retrieve]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_note_retrieve] (uniqueidentifier)@uuid', 1;
-- act
SELECT *
FROM [dbo].[notes]
WHERE [uuid] = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_note_update]
  @uuid uniqueidentifier = NULL,
  @pad_id int = NULL,
  @name varchar(256) = NULL,
  @text varchar(MAX) = NULL,
  @active bit = NULL,
  @status tinyint = NULL,
  @custom_fields varchar(2000) = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_note_update] (uniqueidentifier)@uuid[, (int)@pad_id, (varchar[256])@name, (varchar[MAX])@text, (bit)@active, (tinyint)@status, (varchar[2000])@custom_fields]', 1;
IF @custom_fields IS NOT NULL AND ISJSON(@custom_fields) IS NULL
  THROW 65600, N'@custom_fields MUST conform to RFC4627', 1;
-- act
UPDATE [dbo].[notes] SET
    pad_id = COALESCE(@pad_id, pad_id),
    name = COALESCE(@name, name),
    text = COALESCE(@text, text),
    active = COALESCE(@active, active),
    status = COALESCE(@status, status),
    custom_fields = COALESCE(@custom_fields, custom_fields)
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_note_delete]
  @uuid uniqueidentifier = NULL
AS
-- validate
IF @uuid IS NULL
  THROW 65600, N'This stored procedure syntax is: EXEC [dbo].[USP_note_delete] (uniqueidentifier)@uuid', 1;
-- act
DELETE FROM [dbo].[notes]
WHERE uuid = @uuid
    GO

CREATE OR ALTER PROC [dbo].[USP_notes_cquery]
  @has_next bit OUTPUT,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(256) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_notes_cquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[notes]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX) = FORMATMESSAGE(N'
SELECT *
INTO #__page_delta__
FROM [dbo].[notes]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT (@page_size + 1) ROWS ONLY

SELECT TOP %s * FROM #__page_delta__;
SELECT @has_next = CASE WHEN COUNT(*) > @page_size THEN 1 ELSE 0 END FROM #__page_delta__;

DROP TABLE #__page_delta__;
', @predicate, @safe_orderBy, CAST(@page_size AS nvarchar(32)));
-- act
EXEC sp_executesql
  @sql,
  N'@has_next bit OUTPUT,
    @page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(256)
  ',
  @has_next = @has_next OUTPUT,
  @page = @page,
  @page_size = @page_size,
  @filter_active = @filter_active,
  @filter_name = @filter_name
;
GO

CREATE OR ALTER PROC [dbo].[USP_notes_pquery]
  @do_count bit = 0,
  @page int = 1,
  @page_size int = 12,
  @filter_active bit = 1,
  @filter_name varchar(256) = NULL,
  @order_by varchar(MAX) = NULL
AS
-- validate
IF @page_size > 120
  THROW 65600, N'Invalid optional parameter @page_size was specified for procedure [dbo].[USP_notes_pquery]. Upper boundary is 120.', 1
-- declare
DECLARE @safe_orderBy varchar(MAX) = dbo.fn_validateSortSpecificationList(@order_by, '[dbo].[notes]', 'name');

DECLARE @predicate varchar(MAX) = N'active = @filter_active';
IF @filter_name IS NOT NULL SET @predicate = @predicate + N' AND name LIKE @filter_name';

DECLARE @sql nvarchar(MAX);
-- act
SET @sql = FORMATMESSAGE(N'
SELECT *
FROM [dbo].[notes]
WHERE %s
ORDER BY %s
OFFSET (@page - 1) * @page_size ROWS
FETCH NEXT @page_size ROWS ONLY;
', @predicate, @safe_orderBy)

EXEC sp_executesql @sql,
  N'@page int,
    @page_size int,
    @filter_active bit,
    @filter_name varchar(256)
  ', 
@page = @page, 
@page_size = @page_size, 
@filter_active = @filter_active,
@filter_name = @filter_name;

IF @do_count = 1 BEGIN
  SET @sql = FORMATMESSAGE(N'SELECT total = COUNT(1) FROM [dbo].[notes] WHERE %s', @predicate)
  EXEC sp_executesql
    @sql,
    N'@filter_active bit,
      @filter_name varchar(256)
    ', 
    @filter_active = @filter_active,
    @filter_name = @filter_name;
END
GO