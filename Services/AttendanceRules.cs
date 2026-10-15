namespace attendence.Services;
public static class AttendanceRules
{
    public static double Percentage(int present,int absent,int late,int excused)
    {
        if(present<0||absent<0||late<0||excused<0)throw new ArgumentOutOfRangeException(nameof(present),"Attendance totals cannot be negative.");
        var eligible=present+absent+late;
        return eligible==0?0:100d*(present+late)/eligible;
    }
    public static bool IsBelowThreshold(double percentage,double threshold=75)=>percentage<threshold;
}
