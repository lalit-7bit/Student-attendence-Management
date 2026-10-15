/* OPTIONAL: synthetic, clearly labelled demo entities only. Never run this against production student data. */
USE SmartAttend;
GO
DECLARE @DepartmentId int,@ClassId int,@TeacherId int,@SubjectId int;
IF NOT EXISTS(SELECT 1 FROM dbo.Departments WHERE Name=N'Demo Computer Science') INSERT dbo.Departments(Name) VALUES(N'Demo Computer Science');
SELECT @DepartmentId=DepartmentId FROM dbo.Departments WHERE Name=N'Demo Computer Science';
IF NOT EXISTS(SELECT 1 FROM dbo.Classes WHERE DepartmentId=@DepartmentId AND Name=N'Demo B.Tech CSE S5' AND AcademicYear=N'2026-27') INSERT dbo.Classes(DepartmentId,Name,Semester,AcademicYear) VALUES(@DepartmentId,N'Demo B.Tech CSE S5',5,N'2026-27');
SELECT @ClassId=ClassId FROM dbo.Classes WHERE DepartmentId=@DepartmentId AND Name=N'Demo B.Tech CSE S5' AND AcademicYear=N'2026-27';
IF NOT EXISTS(SELECT 1 FROM dbo.Teachers WHERE Email=N'demo.teacher@example.invalid') INSERT dbo.Teachers(FullName,Email) VALUES(N'Demo Faculty',N'demo.teacher@example.invalid');
SELECT @TeacherId=TeacherId FROM dbo.Teachers WHERE Email=N'demo.teacher@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.Subjects WHERE ClassId=@ClassId AND Code=N'DEMO-NET') INSERT dbo.Subjects(ClassId,Code,Name) VALUES(@ClassId,N'DEMO-NET',N'Demo .NET Technologies');
SELECT @SubjectId=SubjectId FROM dbo.Subjects WHERE ClassId=@ClassId AND Code=N'DEMO-NET';
IF NOT EXISTS(SELECT 1 FROM dbo.TeacherAssignments WHERE TeacherId=@TeacherId AND SubjectId=@SubjectId) INSERT dbo.TeacherAssignments(TeacherId,SubjectId) VALUES(@TeacherId,@SubjectId);
IF NOT EXISTS(SELECT 1 FROM dbo.Students WHERE EnrollmentNumber=N'DEMO-001') INSERT dbo.Students(EnrollmentNumber,FullName,Email,ClassId) VALUES(N'DEMO-001',N'Demo Student',N'demo.student@example.invalid',@ClassId);
GO
