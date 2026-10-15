/* Safe upgrade for an existing SmartAttend database. Run in SSMS/sqlcmd against SmartAttend. */
IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUsers (
        AppUserId int IDENTITY PRIMARY KEY,
        Email nvarchar(256) NOT NULL,
        NormalizedEmail nvarchar(256) NOT NULL UNIQUE,
        PasswordHash nvarchar(500) NOT NULL,
        Role nvarchar(20) NOT NULL CHECK(Role IN('Administrator','Teacher','Student')),
        StudentId int NULL UNIQUE REFERENCES dbo.Students(StudentId),
        TeacherId int NULL UNIQUE REFERENCES dbo.Teachers(TeacherId),
        IsActive bit NOT NULL DEFAULT 1,
        CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_AppUsers_Link CHECK((Role='Administrator' AND StudentId IS NULL AND TeacherId IS NULL) OR (Role='Teacher' AND TeacherId IS NOT NULL AND StudentId IS NULL) OR (Role='Student' AND StudentId IS NOT NULL AND TeacherId IS NULL))
    );
END;
