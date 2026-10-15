namespace attendence.Models
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public string InstitutionName { get; set; } = "SmartAttend";

        public int AttendanceToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int ExcusedToday { get; set; }

        public double AverageAttendance { get; set; }

        public int LowAttendance { get; set; }
        public double LowAttendanceThreshold { get; set; } = 75;
        public List<DailyAttendanceSummary> Weekly { get; set; } = new List<DailyAttendanceSummary>();
        public int MonthPresent { get; set; }
        public int MonthAbsent { get; set; }
        public int MonthLate { get; set; }
        public int MonthExcused { get; set; }
        public List<ClassAttendanceSummary> Classes { get; set; } = new List<ClassAttendanceSummary>();
        public List<RecentAttendanceActivity> Recent { get; set; } = new List<RecentAttendanceActivity>();
    }
    public sealed class DailyAttendanceSummary { public DateTime Date { get; set; } public int Present { get; set; } public int Absent { get; set; } public int Late { get; set; } }
    public sealed class ClassAttendanceSummary { public string ClassName { get; set; } = ""; public int Students { get; set; } public double Percentage { get; set; } }
    public sealed class RecentAttendanceActivity { public DateTime Date { get; set; } public string ClassName { get; set; } = ""; public string Subject { get; set; } = ""; public int Records { get; set; } }
}
